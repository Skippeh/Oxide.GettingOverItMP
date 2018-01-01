using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared
{
    public class CommandManager
    {
        class PropertyAttribute
        {
            public readonly PropertyInfo PropertyInfo;
            public readonly CommandArgumentAttribute Attribute;

            public PropertyAttribute(PropertyInfo propertyInfo, CommandArgumentAttribute attribute)
            {
                PropertyInfo = propertyInfo;
                Attribute = attribute;
            }
        }

        private GameServer server;
        private CommandParser parser;
        private NetPlayer currentCaller;

        public CommandManager(GameServer server)
        {
            this.server = server;
            parser = CommandParser.CreateNew(prefix: "/");
            parser.OnError(OnParseError);
            LoadCommands();
        }

        private void OnParseError(object sender, string error)
        {
            currentCaller?.SendChatMessage(error, SharedConstants.ColorRed);
        }

        public bool HandleChatMessage(NetPlayer caller, string message)
        {
            currentCaller = caller;
            bool result = parser.Parse(message, caller, (int) caller.AccessLevel);
            currentCaller = null;
            return result;
        }

        private void LoadCommands()
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes()).ToList();
            Type chatCommandBaseType = typeof(ChatCommand);

            foreach (Type type in types)
            {
                var attribute = type.GetCustomAttributes(typeof(ChatCommandAttribute), true).FirstOrDefault() as ChatCommandAttribute;
                var authAttribute = type.GetCustomAttributes(typeof(RequireAuthAttribute), true).FirstOrDefault() as RequireAuthAttribute;

                if (attribute == null)
                    continue;

                if (!chatCommandBaseType.IsAssignableFrom(type))
                    continue;

                parser.AddCommand(CommandFromAttribute(type, attribute, authAttribute));
            }
        }

        private Command CommandFromAttribute(Type type, ChatCommandAttribute attribute, RequireAuthAttribute authAttribute)
        {
            var command = new Command(attribute.FriendlyName, attribute.CommandName, attribute.Description);

            if (authAttribute != null)
                command.AccessLevel = (int) authAttribute.AccessLevel;

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var argumentAttribute = property.GetCustomAttributes(typeof(CommandArgumentAttribute), true).FirstOrDefault() as CommandArgumentAttribute;

                if (argumentAttribute == null)
                    continue;

                if (!property.CanWrite)
                {
                    Logger.LogError($"Argument property not writable: {type.Name}.{property.Name}");
                    continue;
                }

                command.AddArgument(ArgumentFromAttribute(type, property, argumentAttribute));
            }

            command.SetAction((arguments, data, rawArgs) =>
            {
                var netPlayer = (NetPlayer)data;
                var instance = (ChatCommand) Activator.CreateInstance(type, true);
                instance.Server = server;
                instance.Parser = parser;

                if (!PrepareInstance(instance, arguments, out string error))
                {
                    netPlayer?.SendChatMessage(error, SharedConstants.ColorRed);
                    return;
                }

                instance.Handle(netPlayer, rawArgs);
            });

            return command;
        }

        private Argument ArgumentFromAttribute(Type commandType, PropertyInfo argumentProperty, CommandArgumentAttribute attribute)
        {
            var argument = new Argument(attribute.Name, attribute.Optional);

            if (attribute.Optional)
                argument.SetDefault(attribute.DefaultValue);
            
            return argument;
        }

        private bool PrepareInstance(ChatCommand instance, Argument[] arguments, out string error)
        {
            List<PropertyAttribute> propertyAttributes = new List<PropertyAttribute>();

            foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attribute = property.GetCustomAttributes(typeof(CommandArgumentAttribute), true).FirstOrDefault() as CommandArgumentAttribute;

                if (attribute == null)
                    continue;

                propertyAttributes.Add(new PropertyAttribute(property, attribute));
            }

            foreach (Argument argument in arguments)
            {
                foreach (var propertyAttribute in propertyAttributes)
                {
                    if (propertyAttribute.Attribute.Name.ToLower() != argument.Name)
                    {
                        continue;
                    }

                    Type targetType = propertyAttribute.PropertyInfo.PropertyType;

                    try
                    {
                        object convertedValue = Convert.ChangeType(argument.Value, targetType, CultureInfo.InvariantCulture);
                        propertyAttribute.PropertyInfo.SetValue(instance, convertedValue, null);
                    }
                    catch
                    {
                        error = $"Argument is not valid: {argument.Name}.";
                        return false;
                    }

                    break;
                }
            }

            error = null;
            return true;
        }
    }
}
