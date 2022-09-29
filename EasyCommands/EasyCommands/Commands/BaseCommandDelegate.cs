using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EasyCommands.Commands
{
    /// <summary>
    /// Command delegate for command without sub-commands
    /// </summary>
    /// <typeparam name="TSender">Object containing the context of the user sending the command</typeparam>
    public class BaseCommandDelegate<TSender> : CommandDelegate<TSender>
    {
        private MethodInfo callback;
        private ParameterInfo[] callbackParams;
        /// <summary>
        /// Holds the number of parameters that this instance will need to manage
        /// </summary>
        private int callbackParamsLength;
        private string[] paramNames;
        private int phraseIndex;
        private int flagsIndex;
        private int minLength;
        private int maxLength;
        private string syntaxDocumentation;

        public string ShortName { get; private set; }

        public BaseCommandDelegate(Context<TSender> context, string mainName, string[] allNames, string shortName, MethodInfo callback)
            : base(context, mainName, allNames)
        {
            ShortName = shortName;
            this.callback = callback;
            callbackParams = callback.GetParameters();
            callbackParamsLength = callbackParams
                .Where(p => !Context.CommandRepository.CanResolveType(p.ParameterType)) // external types are not handled here
                .Count();
            paramNames = callbackParams.Select(p => {
                ParamName nameOverride = p.GetCustomAttribute<ParamName>();
                return nameOverride == null ? p.Name : nameOverride.Name;
            }).ToArray();
            maxLength = callbackParamsLength;
            minLength = Array.FindIndex(callbackParams, p => p.HasDefaultValue);
            if(minLength == -1)
            {
                minLength = maxLength;
            }
            phraseIndex = Array.FindIndex(callbackParams, p => p.GetCustomAttribute<AllowSpaces>() != null);
            flagsIndex = Array.FindIndex(callbackParams, p => Context.ArgumentParser.ParseRuleIsFlags(p.ParameterType));
            SyntaxOverride syntaxOverride = callback.GetCustomAttribute<SyntaxOverride>();
            if (syntaxOverride == null)
            {
                syntaxDocumentation =
                    Context.TextOptions.CommandPrefix + Name + " "
                    + string.Join(" ", paramNames.Select(
                        (nm, i) =>
                            i >= minLength || Context.ArgumentParser.ParseRuleIsFlags(callbackParams[i].ParameterType)
                            ? $"[{nm}]" : $"<{nm}>"));
            }
            else
            {
                syntaxDocumentation = $"{Context.TextOptions.CommandPrefix}{Name} {syntaxOverride.Syntax}";
            }
            foreach(CustomAttribute attribute in callback.GetCustomAttributes<CustomAttribute>(true))
            {
                customAttributes[attribute.GetType()] = attribute;
            }

            if(minLength != maxLength && phraseIndex >= 0 && phraseIndex < maxLength - 1)
            {
                throw new CommandRegistrationException(
                    $"{callback.DeclaringType.Name}.{callback.Name} cannot contain a parameter with the AllowSpaces attribute along with any optional parameters.");
            }
            if(minLength != maxLength && flagsIndex >= 0)
            {
                throw new CommandRegistrationException(
                    $"{callback.DeclaringType.Name}.{callback.Name} cannot contain a flags parameter along with any optional parameters.");
            }
            if(callback.ReturnType != typeof(void) && callback.ReturnType != typeof(Task))
            {
                throw new CommandRegistrationException($"{callback.DeclaringType.Name}.{callback.Name} must return void or Task.");
            }
            if(callbackParams.Count(p => p.GetCustomAttribute<AllowSpaces>() != null || Context.ArgumentParser.ParseRuleIsFlags(p.ParameterType)) > 1)
            {
                throw new CommandRegistrationException(
                    $"{callback.DeclaringType.Name}.{callback.Name} cannot contain more than one flags parameter or parameter with the AllowSpaces attribute.");
            }
            ParameterInfo undefinedParam = callbackParams.FirstOrDefault(p => 
                !Context.ArgumentParser.ParseRuleExists(p.ParameterType)
                && !Context.CommandRepository.CanResolveType(p.ParameterType)
            );
            if(undefinedParam != null)
            {
                throw new CommandRegistrationException(
                    $"The parameter {undefinedParam.Name} in the command {callback.DeclaringType.Name}.{callback.Name} does " +
                    $"not contain a corresponding parse rule for type {undefinedParam.ParameterType.Name}.");
            }
            // Flag parameters can have length zero, so don't count it towards the min length
            if(flagsIndex >= 0)
            {
                minLength--;
                maxLength = int.MaxValue;
            }
            if(phraseIndex >= 0)
            {
                maxLength = int.MaxValue;
            }
        }

        public override string SyntaxDocumentation(TSender sender)
        {
            return syntaxDocumentation;
        }

        public override async Task Invoke(TSender sender, string args)
        {
            var argList = new List<string>();
            bool inQuotes = false;
            // Build up space-separated list with quotes accounted for
            for(int i = 0; i < args.Length; i++)
            {
                if(argList.Count == 0)
                {
                    argList.Add("");
                }
                char c = args[i];
                if(i < args.Length - 1 && c == '\\' && args[i + 1] == '"')
                {
                    // Add escaped quotes
                    argList[argList.Count - 1] += '"';
                    i++;
                }
                else if(c == '"')
                {
                    // Enter or exit a quote
                    inQuotes = !inQuotes;
                }
                else if(c == ' ' && !inQuotes)
                {
                    argList.Add("");
                }
                else
                {
                    argList[argList.Count - 1] += c;
                }
            }
            await Invoke(sender, args, argList);
        }

        private async Task Invoke(TSender sender, string argText, IEnumerable<string> args)
        {
            Context.CommandHandler.PreCheck(sender, this);

            if (args.Count() < minLength || args.Count() > maxLength)
            {
                throw new CommandParsingException(
                    string.Format(Context.TextOptions.WrongNumberOfArguments, SyntaxDocumentation(sender)));
            }

            var invocationParams = new object[callbackParams.Length];
            int j = 0;
            for(int i = 0; i < callbackParams.Length; i++)
            {
                if (ResolveType(callbackParams[i].ParameterType) is object obj)
                    invocationParams[i] = obj;
                else if(i == flagsIndex || i == phraseIndex) // Handle multi-word arguments
                {
                    int multiWordLength = args.Count() - callbackParamsLength + 1;
                    if(i >= args.Count() && i == phraseIndex)
                    {
                        invocationParams[i] = callbackParams[i].DefaultValue;
                    }
                    else
                    {
                        invocationParams[i] = Context.ArgumentParser.ParseArgument(
                            callbackParams[i].ParameterType,
                            callbackParams[i].GetCustomAttributes(),
                            paramNames[i],
                            SyntaxDocumentation(sender),
                            args.ToList().GetRange(j, multiWordLength).ToArray());
                    }
                    j += multiWordLength;
                }
                else
                {
                    if(i >= args.Count())
                    {
                        invocationParams[i] = callbackParams[i].DefaultValue;
                    }
                    else
                    {
                        invocationParams[i] = Context.ArgumentParser.ParseArgument(
                            callbackParams[i].ParameterType, callbackParams[i].GetCustomAttributes(),
                            paramNames[i], SyntaxDocumentation(sender), args.ElementAt(j));
                    }
                    j++;
                }
            }
            CommandCallbacks<TSender> instance = (CommandCallbacks<TSender>)Activator.CreateInstance(callback.DeclaringType);
            instance.Sender = sender;
            instance.RawCommandText = argText;
            instance.CommandRepository = Context.CommandRepository;
            instance.TextOptions = Context.TextOptions;
            try
            {
                object task = callback.Invoke(instance, invocationParams);
                if (task is Task t)
                {
                    await t;
                }
            }
            catch(TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }
    }
}