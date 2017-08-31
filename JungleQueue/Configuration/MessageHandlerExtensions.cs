using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JungleQueue.Interfaces;
using JungleQueue.Interfaces.Exceptions;
using JungleQueue.Interfaces.IoC;

namespace JungleQueue.Configuration
{
    /// <summary>
    /// Extension Methods for setting up the handler configuration
    /// </summary>
    public static class MessageHandlerExtensions
    {
        /// <summary>
        /// Load the event handlers from the entry assembly
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <returns>Modified configuration</returns>
        public static QueueConfiguration UsingEventHandlersFromEntryAssembly(this QueueConfiguration configuration)
        {
            IEnumerable<Type> types = Assembly.GetEntryAssembly().ExportedTypes;
            return configuration
                .UsingEventHandlers(types)
                .UsingEventFaultHandlers(types);
        }

        /// <summary>
        /// Load the event handlers from the given types
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <param name="eventHandlers">Event handlers to register</param>
        /// <returns>Modified configuration</returns>
        public static QueueConfiguration UsingEventHandlers(this QueueConfiguration configuration, IEnumerable<Type> eventHandlers)
        {
            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            if (configuration.ObjectBuilder == null)
            {
                throw new JungleConfigurationException("ObjectBuilder", "Object builder must be set");
            }

            configuration.Handlers = ScanForTypes(eventHandlers, typeof(IHandleMessage<>), configuration.ObjectBuilder);
            configuration.FaultHandlers = new Dictionary<Type, HashSet<Type>>();
            return configuration;
        }

        /// <summary>
        /// Load the event handlers from the given types
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <param name="eventFaultHandlers">Event fault handlers to register</param>
        /// <returns>Modified configuration</returns>
        public static QueueConfiguration UsingEventFaultHandlers(this QueueConfiguration configuration, IEnumerable<Type> eventFaultHandlers)
        {
            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            if (configuration.ObjectBuilder == null)
            {
                throw new JungleConfigurationException("ObjectBuilder", "Object builder must be set");
            }

            configuration.FaultHandlers = ScanForTypes(eventFaultHandlers, typeof(IHandleMessageFaults<>), configuration.ObjectBuilder);
            return configuration;
        }

        /// <summary>
        /// Scans the given types for instances of the requested interface
        /// </summary>
        /// <param name="typesToScan">Types to scan</param>
        /// <param name="handlerTypeToFind">Handler type to find</param>
        /// <param name="objectBuilder">Object builder to register the types with</param>
        /// <returns>Types found</returns>
        private static Dictionary<Type, HashSet<Type>> ScanForTypes(IEnumerable<Type> typesToScan, Type handlerTypeToFind, IObjectBuilder objectBuilder)
        {
            Dictionary<Type, HashSet<Type>> results = new Dictionary<Type, HashSet<Type>>();
            var handlerTypes = typesToScan.Where(x => x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerTypeToFind));
            foreach (var handlerType in handlerTypes)
            {
                foreach (var handlerTypeType in handlerType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerTypeToFind))
                {
                    Type messageType = handlerTypeType.GenericTypeArguments[0];
                    if (!results.ContainsKey(messageType))
                    {
                        results[messageType] = new HashSet<Type>();
                    }

                    results[messageType].Add(handlerType);
                }

                objectBuilder.RegisterType(handlerType, handlerType);
            }

            return results;
        }
    }
}
