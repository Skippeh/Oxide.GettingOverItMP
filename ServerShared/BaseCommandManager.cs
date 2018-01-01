using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared
{
    public abstract class BaseCommandManager<T> where T : BaseCommand
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

        protected readonly GameServer Server;
        protected readonly CommandParser Parser;
        protected NetPlayer CurrentCaller { get; private set; }
        
        public BaseCommandManager(GameServer server, string prefix, params Assembly[] searchAssemblies)
        {
            Server = server;
            Parser = CommandParser.CreateNew(prefix);
            Parser.OnError(OnParseError);
            LoadCommands(searchAssemblies.Concat(new[] {Assembly.GetExecutingAssembly()}));
        }
        
        public bool HandleMessage(NetPlayer caller, string message)
        {
            CurrentCaller = caller;
            bool result = Parser.Parse(message, caller, (int) (caller?.AccessLevel ?? AccessLevel.Console));
            CurrentCaller = null;
            return result;
        }

        private void LoadCommands(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes()).ToList();
            Type commandBaseType = typeof(T);

            foreach (Type type in types)
            {
                var attribute = type.GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault() as CommandAttribute;
                var authAttribute = type.GetCustomAttributes(typeof(RequireAuthAttribute), true).FirstOrDefault() as RequireAuthAttribute;
                var requireCallerAttribute = type.GetCustomAttributes(typeof(RequireCallerAttribute), true).FirstOrDefault() as RequireCallerAttribute;

                if (attribute == null)
                    continue;

                if (!commandBaseType.IsAssignableFrom(type))
                    continue;

                Parser.AddCommand(CommandFromAttribute(type, attribute, authAttribute, requireCallerAttribute != null));
            }
        }

        private Command CommandFromAttribute(Type type, CommandAttribute attribute, RequireAuthAttribute authAttribute, bool requireCaller)
        {
            var command = new Command(Parser, attribute.FriendlyName, attribute.CommandNames.First(), attribute.Description);
            command.RequireCaller = requireCaller;

            if (attribute.CommandNames.Length > 1)
                command.AddAlias(attribute.CommandNames.Skip(1).ToArray());

            if (authAttribute != null)
                command.AccessLevel = (int)authAttribute.AccessLevel;

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
                var netPlayer = (NetPlayer) data;
                var instance = (BaseCommand) Activator.CreateInstance(type, true);
                instance.Server = Server;
                instance.Parser = Parser;
                instance.Context = (this is ChatCommandManager) ? CommandContext.Chat : CommandContext.Console;
                instance.Caller = netPlayer;
                
                if (!PrepareInstance(instance, arguments, out string error))
                {
                    OnParseError(this, error);
                    return;
                }

                instance.Handle(rawArgs);
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

        private bool PrepareInstance(BaseCommand instance, Argument[] arguments, out string error)
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
                        object convertedValue;

                        if (targetType.IsEnum)
                        {
                            convertedValue = Enum.Parse(targetType, argument.Value, true);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(argument.Value, targetType, CultureInfo.InvariantCulture);
                        }

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

        protected abstract void OnParseError(object sender, string error);
    }
}
