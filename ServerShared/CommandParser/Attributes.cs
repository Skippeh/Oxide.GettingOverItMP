using System;

namespace Pyratron.Frameworks.Commands.Parser
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ChatCommandAttribute : Attribute
    {
        public readonly string FriendlyName;
        public readonly string CommandName;
        public readonly string Description;

        public ChatCommandAttribute(string friendlyName, string commandName, string description)
        {
            FriendlyName = friendlyName;
            CommandName = commandName;
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CommandArgumentAttribute : Attribute
    {
        public readonly string Name;
        public readonly bool Optional;
        public readonly string DefaultValue;

        public CommandArgumentAttribute(string name, bool optional = false, string defaultValue = null)
        {
            Name = name;
            Optional = optional;
            DefaultValue = defaultValue;
        }
    }
}
