using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EasyCommands.Commands
{
    /// <summary>
    /// Stores and indexes all commands
    /// </summary>
    /// <typeparam name="TSender">Object containing the context of the user sending the command</typeparam>
    public abstract class CommandRepository<TSender>
    {
        protected List<CommandDelegate<TSender>> commandList = new List<CommandDelegate<TSender>>();
        protected Context<TSender> Context;

        public ReadOnlyCollection<CommandDelegate<TSender>> CommandList
        {
            get => commandList.AsReadOnly();
        }

        public CommandRepository(Context<TSender> context)
        {
            Context = context;
        }

        public virtual void RegisterCommand(string[] names, MethodInfo command)
        {
            var newCommand = new BaseCommandDelegate<TSender>(Context, names[0], names, names[0], command);
            AddCommand(newCommand, names);
        }

        public virtual void RegisterCommandWithSubcommands(string[] names, Type command)
        {
            if(command.BaseType != typeof(CommandCallbacks<TSender>))
            {
                throw new CommandRegistrationException($"{command.Name} must have the base class CommandCallbacks.");
            }
            if(command.GetNestedTypes().Any(t => command.GetCommandNames<Command>() != null))
            {
                throw new CommandRegistrationException($"Unexpected nested subcommand in {command.Name}.");
            }

            CommandGroupDelegate<TSender> newCommand = new CommandGroupDelegate<TSender>(Context, names[0], names, command);
            AddCommand(newCommand, names);
        }

        public abstract Task Invoke(TSender sender, string command);

        protected abstract void AddCommand(CommandDelegate<TSender> command, string[] names);

        /// <summary>
        /// Allows external type resolutions
        /// </summary>
        /// <param name="type">Type requested</param>
        /// <returns>True when the type is supported by the external system, false otherwise</returns>
        public virtual bool CanResolveType(Type type) => false;
    }
}
