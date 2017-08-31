// <copyright file="QueueBuilder.cs">
//     The MIT License (MIT)
//
// Copyright(c) 2016 Ryan Fleming
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using JungleQueue.Interfaces;
using JungleQueue.Interfaces.Configuration;
using JungleQueue.Interfaces.Exceptions;
using JungleQueue.Interfaces.IoC;
using JungleQueue.Messaging;

namespace JungleQueue.Configuration
{
    /// <summary>
    /// Helper class for configuring the queue
    /// </summary>
    public static class QueueBuilder
    {
        /// <summary>
        /// Starting method for the configuration
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="region">AWS Region the queue lives in</param>
        /// <returns>Queue configuration</returns>
        public static IConfigureObjectBuilder Create(string queueName, RegionEndpoint region)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new JungleConfigurationException("queueName", "Cannot have a blank input queue name");
            }

            if (region == null)
            {
                throw new JungleConfigurationException("region", "Region cannot be null");
            }

            return new QueueConfiguration()
            {
                MessageLogger = new NoOpMessageLogger(),
                QueueName = queueName,
                Region = region,
                FaultHandlers = new Dictionary<Type, HashSet<Type>>(),
                Handlers = new Dictionary<Type, HashSet<Type>>(),
                MaxSimultaneousMessages = 0,
                RetryCount = 5,
                SqsPollWaitTime = 14,
            };
        }

        /// <summary>
        /// Configure the the queue to use a given object builder to build the handlers
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <param name="objectBuilder">Object Builder to use</param>
        /// <returns>Modified configuration</returns>
        public static IConfigureMessageSerializer WithObjectBuilder(this IConfigureObjectBuilder configuration, IObjectBuilder objectBuilder)
        {
            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            configuration.ObjectBuilder = objectBuilder;
            return configuration as IConfigureMessageSerializer;
        }

        /// <summary>
        /// Configure the the queue to use the simple object builder to build the handlers
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <returns>Modified configuration</returns>
        public static IConfigureMessageSerializer WithSimpleObjectBuilder(this IConfigureObjectBuilder configuration)
        {
            return configuration.WithObjectBuilder(new IoC.SimpleObjectBuilder());
        }

        /// <summary>
        /// Configure the the queue to use JSON serialization of the messages
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <returns>Modified configuration</returns>
        public static QueueConfiguration UsingJsonSerialization(this IConfigureMessageSerializer configuration)
        {
            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            if (configuration.ObjectBuilder == null)
            {
                throw new JungleConfigurationException("ObjectBuilder", "Object builder must be set");
            }

            // ToDo: Currently not being used, should reintroduce the message serializer in the next version
            // configuration.ObjectBuilder.RegisterInstance<IMessageSerializer>(new JsonNetSerializer());
            return configuration as QueueConfiguration;
        }

        /// <summary>
        /// Configure the the queue to use log inbound and outbound messages
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <returns>Modified configuration</returns>
        public static QueueConfiguration EnableMessageLogging(this QueueConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            configuration.MessageLogger = new MessageLogger();
            return configuration;
        }

        /// <summary>
        /// Configure the polling wait time for receive queue
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <param name="timeInSeconds">Number of seconds to the long polling to wait</param>
        /// <returns>Modified configuration</returns>
        public static QueueConfiguration SetSqsPollWaitTime(this QueueConfiguration configuration, int timeInSeconds)
        {
            if (timeInSeconds < 0 || timeInSeconds > 14)
            {
                throw new JungleConfigurationException("timeInSeconds", "Time in seconds must be between 0 and 14");
            }

            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            configuration.SqsPollWaitTime = timeInSeconds;
            return configuration;
        }

        /// <summary>
        /// Configure the messages that can be processed simultaneously
        /// </summary>
        /// <param name="configuration">Configuration to modify</param>
        /// <param name="instances">Number of polling instances to run</param>
        /// <returns>Modified configuration</returns>
        public static QueueConfiguration WithMaxSimultaneousMessages(this QueueConfiguration configuration, int instances)
        {
            if (instances < 0)
            {
                throw new JungleConfigurationException("instances", "Number of simultaneous messages must be greater than 0");
            }

            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            configuration.MaxSimultaneousMessages = instances;
            return configuration;
        }

        /// <summary>
        /// Construct the queue
        /// </summary>
        /// <param name="configuration">Configuration to build from</param>
        /// <returns>Created queue</returns>
        public static IRunJungleQueue CreateStartableQueue(this QueueConfiguration configuration)
        {
            configuration.RunGeneralConfigurationValidation();

            if (configuration.Handlers == null || !configuration.Handlers.Any())
            {
                throw new JungleConfigurationException("Handlers", "No messages handlers configured");
            }

            if (configuration.MaxSimultaneousMessages <= 0)
            {
                throw new JungleConfigurationException("instances", "Number of simultaneous messages must be greater than 0");
            }

            JungleQueue jungleQueue = new JungleQueue(configuration);
            return jungleQueue;
        }

        /// <summary>
        /// Creates a send only queue factory from the configuration
        /// </summary>
        /// <param name="configuration">Configuration to build from</param>
        /// <returns>Factory for building send only queues</returns>
        public static Func<IQueue> CreateSendOnlyQueueFactory(this QueueConfiguration configuration)
        {
            configuration.RunGeneralConfigurationValidation();

            JungleQueue jungleQueue = new JungleQueue(configuration);
            return () => jungleQueue.CreateSendQueue();
        }

        /// <summary>
        /// Runs validation general to building a queue
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        private static void RunGeneralConfigurationValidation(this QueueConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new JungleConfigurationException("configuration", "Configuration cannot be null");
            }

            if (configuration.ObjectBuilder == null)
            {
                throw new JungleConfigurationException("ObjectBuilder", "Object builder has not been configured");
            }

            if (configuration.MessageLogger == null)
            {
                throw new JungleConfigurationException("MessageLogger", "Message logger cannot be null");
            }
        }
    }
}
