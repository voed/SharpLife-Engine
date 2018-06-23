using System;

namespace SharpLife.Engine.CommandSystem.Commands
{
    internal abstract class BaseConsoleCommand : IBaseConsoleCommand
    {
        public string Name { get; }

        public CommandFlags Flags { get; }

        public string HelpInfo { get; }

        protected BaseConsoleCommand(string name, CommandFlags flags = CommandFlags.None, string helpInfo = "")
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            Name = name;
            Flags = flags;
            HelpInfo = helpInfo ?? throw new ArgumentNullException(nameof(helpInfo));
        }

        /// <summary>
        /// Handles a command invocation with the given arguments
        /// </summary>
        /// <param name="command"></param>
        /// <exception cref="InvalidCommandSyntaxException">When the command is invoked with the wrong syntax</exception>
        internal abstract void OnCommand(ICommand command);
    }
}
