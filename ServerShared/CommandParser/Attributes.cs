using System;
using ServerShared.Player;

namespace Pyratron.Frameworks.Commands.Parser
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public readonly string FriendlyName;
        public readonly string[] CommandNames;
        public readonly string Description;

        public CommandAttribute(string friendlyName, string commandName, string description) : this(friendlyName, new[] {commandName}, description)
        {
        }

        public CommandAttribute(string friendlyName, string[] commandNames, string description)
        {
            FriendlyName = friendlyName;
            CommandNames = commandNames;
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

    /// <summary>
    /// Specifies that the command requires the specified access level or higher to use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireAuthAttribute : Attribute
    {
        public readonly AccessLevel AccessLevel;

        public RequireAuthAttribute(AccessLevel accessLevel)
        {
            AccessLevel = accessLevel;
        }
    }

    /// <summary>
    /// Specifies that the command requires that a player called the command. This would not be the case if the command was called from the server console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireCallerAttribute : Attribute
    {
    }
}
