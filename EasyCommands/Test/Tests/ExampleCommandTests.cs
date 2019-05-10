﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Example;
using System.Collections.Generic;

namespace EasyCommands.Test.Tests
{
    [TestClass]
    public class ExampleCommandTests
    {
        User CurrentUser;
        ExampleCommandHandler CommandHandler;
        ConsoleReader ConsoleReader;
        
        public ExampleCommandTests()
        {
            CommandHandler = new ExampleCommandHandler();
            CommandHandler.RegisterCommands("Example.Commands");
        }

        [TestInitialize]
        public void TestInitialize()
        {
            UserDatabase.Reset();
            CurrentUser = UserDatabase.GetUserByName("Admin");
            ConsoleReader = new ConsoleReader();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ConsoleReader.Close();
        }

        [TestMethod]
        [Description("Empty commands throw an error.")]
        public void TestEmptyCommand()
        {
            CommandHandler.RunCommand(CurrentUser, "");
            Assert.AreEqual("Please enter a command.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Nonexistent commands throw an error.")]
        public void TestInvalidCommand()
        {
            CommandHandler.RunCommand(CurrentUser, "asdf 1 2 3");
            Assert.AreEqual("Command \"asdf\" does not exist.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Nonexistent subcommands throw an error.")]
        public void TestInvalidSubcommand()
        {
            CommandHandler.RunCommand(CurrentUser, "window asdf 1 2 3");
            Assert.AreEqual("Command \"window asdf\" does not exist.", ConsoleReader.ReadLine());
            Assert.AreEqual("window contains these subcommands:", ConsoleReader.ReadLine());
            ConsoleReader.AssertOutputContains(
                "window resize <width> <height>",
                "window move <left> <top>");
        }

        [TestMethod]
        [Description("The add command works.")]
        public void TestAdd()
        {
            CommandHandler.RunCommand(CurrentUser, "add 1 2");
            Assert.AreEqual("1 + 2 = 3", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The subtract command works, inferring the command name from the method name.")]
        public void TestSubtract()
        {
            CommandHandler.RunCommand(CurrentUser, "subtract 10 5");
            Assert.AreEqual("10 - 5 = 5", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The divide command works.")]
        public void TestDivide()
        {
            CommandHandler.RunCommand(CurrentUser, "divide 1 2");
            Assert.AreEqual("1 / 2 = 0.5", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The divide command works with its alias.")]
        public void TestDiv()
        {
            CommandHandler.RunCommand(CurrentUser, "div 1 2");
            Assert.AreEqual("1 / 2 = 0.5", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands throw an error when you input the wrong number of arguments.")]
        public void TestTooManyArguments()
        {
            CommandHandler.RunCommand(CurrentUser, "add 1 2 3");
            Assert.AreEqual("Incorrect number of arguments! Proper syntax: add <num1> <num2>", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands throw an error when you input an invalid argument type.")]
        public void TestInvalidArgument()
        {
            CommandHandler.RunCommand(CurrentUser, "add 1 bleh");
            Assert.AreEqual("Invalid syntax! num2 must be a whole number!", ConsoleReader.ReadLine());
            Assert.AreEqual("Proper syntax: add <num1> <num2>", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands throw an error when you input an invalid argument type and uses the overridden name.")]
        public void TestInvalidArgument2()
        {
            CommandHandler.RunCommand(CurrentUser, "subtract 1 bleh");
            Assert.AreEqual("Invalid syntax! num2 must be a whole number!", ConsoleReader.ReadLine());
            Assert.AreEqual("Proper syntax: subtract <num1> <num2>", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands show the correct parameter name when overridden.")]
        public void TestOverriddenParamName()
        {
            CommandHandler.RunCommand(CurrentUser, "subtract");
            Assert.AreEqual("Incorrect number of arguments! Proper syntax: subtract <num1> <num2>", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands with optional arguments work with the optional command omitted.")]
        public void TestOptionalArguments()
        {
            CommandHandler.RunCommand(CurrentUser, "add3or4 1 2 3");
            Assert.AreEqual("sum = 6", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands with optional arguments work with the optional command included.")]
        public void TestOptionalArguments2()
        {
            CommandHandler.RunCommand(CurrentUser, "add3or4 1 2 3 4");
            Assert.AreEqual("sum = 10", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands with optional arguments throw an error when given the wrong number of arguments and show the correct proper syntax.")]
        public void TestOptionalArgumentsProperSyntax()
        {
            CommandHandler.RunCommand(CurrentUser, "add3or4");
            Assert.AreEqual("Incorrect number of arguments! Proper syntax: add3or4 <num1> <num2> <num3> [num4]", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The myname command works.")]
        public void TestMyName()
        {
            CommandHandler.RunCommand(CurrentUser, "myname");
            Assert.AreEqual("Your name is Admin.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The favorite-food command to get a user's favorite food.")]
        public void TestFavoriteFoodGet()
        {
            CommandHandler.RunCommand(CurrentUser, "favorite-food Jeff");
            Assert.AreEqual("Jeff's favorite food is steak.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The favorite-food command to set a user's favorite food.")]
        public void TestFavoriteFoodSet()
        {
            CommandHandler.RunCommand(CurrentUser, "favorite-food Blake bananas");
            User Blake = UserDatabase.GetUserByName("Blake");
            Assert.AreEqual("bananas", Blake.FavoriteFood);
        }

        [TestMethod]
        [Description("Parameters are properly set when in quotes.")]
        public void TestParamsInQuotes()
        {
            CommandHandler.RunCommand(CurrentUser, "favorite-food Henry \"creamed corn\"");
            User Henry = UserDatabase.GetUserByName("Henry");
            Assert.AreEqual("creamed corn", Henry.FavoriteFood);
        }

        [TestMethod]
        [Description("Parameters are properly set with escaped quotes.")]
        public void TestParamsInEscapedQuotes()
        {
            CommandHandler.RunCommand(CurrentUser, "favorite-food Jessica \\\"tacos\\\"");
            User Jessica = UserDatabase.GetUserByName("Jessica");
            Assert.AreEqual("\"tacos\"", Jessica.FavoriteFood);
        }

        [TestMethod]
        [Description("When a user isn't found, the correct error message is shown.")]
        public void TestUserNotFound()
        {
            CommandHandler.RunCommand(CurrentUser, "favorite-food Carl");
            Assert.AreEqual("User Carl not found.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands with the AllowSpaces attribute can include spaces without quotation marks.")]
        public void TestMultiWordArgument()
        {
            CommandHandler.RunCommand(CurrentUser, "add-user Jimmy DefaultUser baked potatoes");
            User Jimmy = UserDatabase.GetUserByName("Jimmy");
            Assert.IsNotNull(Jimmy);
            Assert.AreEqual("baked potatoes", Jimmy.FavoriteFood);
        }

        [TestMethod]
        [Description("The fail condition works on commands with a PermissionLevel parameter.")]
        public void TestPermissionLevel()
        {
            CommandHandler.RunCommand(CurrentUser, "add-user Jimmy BadBoy baked potatoes");
            Assert.AreEqual("BadBoy is not a permission level. Valid values: Guest, DefaultUser, Admin, Superadmin", ConsoleReader.ReadLine());
            
        }

        [TestMethod]
        [Description("Blank commands with subcommands show the available subcommands.")]
        public void TestSubCommand()
        {
            CommandHandler.RunCommand(CurrentUser, "window");
            Assert.AreEqual("window contains these subcommands:", ConsoleReader.ReadLine());
            ConsoleReader.AssertOutputContains(
                "window resize <width> <height>",
                "window move <left> <top>");
        }

        [TestMethod]
        [Description("The window resize subcommand works.")]
        public void TestWindowResize()
        {
            CommandHandler.RunCommand(CurrentUser, "window resize 800 600");
            Assert.AreEqual("Window dimensions set to 800 x 600.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The window move subcommand works, inferring the command name from the method name.")]
        public void TestWindowMove()
        {
            CommandHandler.RunCommand(CurrentUser, "window move 100 100");
            Assert.AreEqual("Window position set to (100, 100).", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows all available commands.")]
        public void TestHelp()
        {
            CommandHandler.RunCommand(CurrentUser, "help");
            Assert.AreEqual("Available commands:", ConsoleReader.ReadLine());
            ConsoleReader.AssertOutputContains(
                "add <num1> <num2>",
                "subtract <num1> <num2>",
                "divide <num1> <num2>",
                "add3or4 <num1> <num2> <num3> [num4]",
                "myname ",
                "favorite-food <querying> [food]",
                "add-user <name> <permissionLevel> <favoriteFood>",
                "window <resize|move>",
                "help [command] [subcommand]",
                "permission-level <user>",
                "superadmin-me <superSecretPassword>");
        }

        [TestMethod]
        [Description("The help command shows more commands after getting access to those commands.")]
        public void TestHelp2()
        {
            CommandHandler.RunCommand(CurrentUser, "superadmin-me hunter2");
            CommandHandler.RunCommand(CurrentUser, "help");
            ConsoleReader.ReadLine();
            ConsoleReader.ReadLine();
            ConsoleReader.AssertOutputContains(
                "add <num1> <num2>",
                "subtract <num1> <num2>",
                "divide <num1> <num2>",
                "add3or4 <num1> <num2> <num3> [num4]",
                "myname ",
                "favorite-food <querying> [food]",
                "add-user <name> <permissionLevel> <favoriteFood>",
                "window <resize|move>",
                "help [command] [subcommand]",
                "permission-level <user>",
                "superadmin-me <superSecretPassword>",
                "delete-production ",
                "supersecret <a|b>");
        }

        [TestMethod]
        [Description("The help command shows the description for a command.")]
        public void TestHelpWithCommand()
        {
            CommandHandler.RunCommand(CurrentUser, "help add");
            Assert.AreEqual("Adds two integers together. Syntax: add <num1> <num2>", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help does not show help for a command if the user doesn't have permission to use a it.")]
        public void TestHelpWithInaccessibleCommand()
        {
            CommandHandler.RunCommand(CurrentUser, "help delete-production");
            Assert.AreEqual("Command \"delete-production\" does not exist.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows the description for a command if you have access to it.")]
        public void TestHelpWithInaccessibleCommand2()
        {
            CommandHandler.RunCommand(CurrentUser, "superadmin-me hunter2");
            CommandHandler.RunCommand(CurrentUser, "help delete-production");
            ConsoleReader.ReadLine();
            Assert.AreEqual("Why would you want to do this!? Syntax: delete-production ", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help does not show help for a command with subcommands if the user doesn't have permission to use a it.")]
        public void TestHelpWithInaccessibleCommand3()
        {
            CommandHandler.RunCommand(CurrentUser, "help supersecret");
            Assert.AreEqual("Command \"supersecret\" does not exist.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows the description for a command with subcommands if you have access to it.")]
        public void TestHelpWithInaccessibleCommand4()
        {
            CommandHandler.RunCommand(CurrentUser, "superadmin-me hunter2");
            CommandHandler.RunCommand(CurrentUser, "help supersecret");
            ConsoleReader.ReadLine();
            Assert.AreEqual("A or B. Subcommands:", ConsoleReader.ReadLine());
            ConsoleReader.AssertOutputContains(
                "supersecret a ",
                "supersecret b ");
        }

        [TestMethod]
        [Description("The help does not show help for a subcommand if the user doesn't have permission to use a it.")]
        public void TestHelpWithInaccessibleCommand5()
        {
            CommandHandler.RunCommand(CurrentUser, "help supersecret a");
            Assert.AreEqual("Command \"supersecret a\" does not exist.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows the description for a subcommand if you have access to it.")]
        public void TestHelpWithInaccessibleCommand6()
        {
            CommandHandler.RunCommand(CurrentUser, "superadmin-me hunter2");
            CommandHandler.RunCommand(CurrentUser, "help supersecret a");
            ConsoleReader.ReadLine();
            Assert.AreEqual("A. Syntax: supersecret a ", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows the aliases for a command.")]
        public void TestHelpWithCommandWithAliases()
        {
            CommandHandler.RunCommand(CurrentUser, "help divide");
            Assert.AreEqual("Divides num1 by num2. Syntax: divide <num1> <num2>. Aliases: div", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows an error if a command doesn't exist.")]
        public void TestHelpWithNonexistentCommand()
        {
            CommandHandler.RunCommand(CurrentUser, "help asdf");
            Assert.AreEqual("Command \"asdf\" does not exist.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows the description for a subcommand.")]
        public void TestHelpWithSubcommand()
        {
            CommandHandler.RunCommand(CurrentUser, "help window move");
            Assert.AreEqual("Moves the window to a certain position. Syntax: window move <left> <top>", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("The help command shows the description for a command with subcommands.")]
        public void TestHelpWithCommandWithSubcommands()
        {
            CommandHandler.RunCommand(CurrentUser, "help window");
            Assert.AreEqual("Manipulates the console window. Subcommands:", ConsoleReader.ReadLine());
            ConsoleReader.AssertOutputContains(
                "window resize <width> <height>",
                "window move <left> <top>");
        }

        [TestMethod]
        [Description("Commands cannot be run if the user doesn't have permission to use them.")]
        public void TestInaccessibleCommand()
        {
            CommandHandler.RunCommand(CurrentUser, "delete-production");
            Assert.AreEqual("Command \"delete-production\" does not exist.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands can be run if the user has access to use them.")]
        public void TestInaccessibleCommand2()
        {
            CommandHandler.RunCommand(CurrentUser, "superadmin-me hunter2");
            CommandHandler.RunCommand(CurrentUser, "delete-production");
            ConsoleReader.ReadLine();
            Assert.AreEqual("Congratulations! You deleted production!", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands with subcommands cannot be run if the user doesn't have permission to use them.")]
        public void TestInaccessibleCommandWithSubcommands()
        {
            CommandHandler.RunCommand(CurrentUser, "supersecret a");
            Assert.AreEqual("Command \"supersecret\" does not exist.", ConsoleReader.ReadLine());
        }

        [TestMethod]
        [Description("Commands with subcommands can be run if the user has access to use them.")]
        public void TestInaccessibleCommandWithSubcommands2()
        {
            CommandHandler.RunCommand(CurrentUser, "superadmin-me hunter2");
            CommandHandler.RunCommand(CurrentUser, "supersecret a");
            ConsoleReader.ReadLine();
            Assert.AreEqual("A", ConsoleReader.ReadLine());
        }
    }
}