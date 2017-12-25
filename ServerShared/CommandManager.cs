using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pyratron.Frameworks.Commands.Parser;
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

        public CommandManager(GameServer server)
        {
            this.server = server;
            parser = CommandParser.CreateNew(prefix: "/");
            LoadCommands();
        }

        public bool HandleChatMessage(NetPlayer caller, string message)
        {
            return parser.Parse(message, caller);
        }

        private void LoadCommands()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes()).ToList();
            Type chatCommandBaseType = typeof(ChatCommand);

            foreach (Type type in types)
            {
                var attribute = type.GetCustomAttributes(typeof(ChatCommandAttribute), true).FirstOrDefault() as ChatCommandAttribute;

                if (attribute == null)
                    continue;

                if (!chatCommandBaseType.IsAssignableFrom(type))
                    continue;

                parser.AddCommand(CommandFromAttribute(type, attribute));
            }
        }

        private Command CommandFromAttribute(Type type, ChatCommandAttribute attribute)
        {
            var command = new Command(attribute.FriendlyName, attribute.CommandName, attribute.Description);

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var argumentAttribute = property.GetCustomAttributes(typeof(CommandArgumentAttribute), true).FirstOrDefault() as CommandArgumentAttribute;

                if (argumentAttribute == null)
                    continue;

                if (!property.CanWrite)
                {
                    Console.WriteLine($"Argument property not writable: {type.Name}.{property.Name}");
                    continue;
                }

                command.AddArgument(ArgumentFromAttribute(type, property, argumentAttribute));
            }

            command.SetAction((arguments, data) =>
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

                instance.Handle(netPlayer);
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
                        object convertedValue = Convert.ChangeType(argument.Value, targetType);
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
