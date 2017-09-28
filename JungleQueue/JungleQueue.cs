// <copyright file="JungleQueue.cs">
//     The MIT License (MIT)
//
// Copyright(c) 2017 Ryan Fleming
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
using System.Threading.Tasks;
using Common.Logging;
using JungleQueue.Aws.Sqs;
using JungleQueue.Configuration;
using JungleQueue.Interfaces;
using JungleQueue.Interfaces.IoC;
using JungleQueue.Interfaces.Serialization;
using JungleQueue.Messaging;

namespace JungleQueue
{
    /// <summary>
    /// Main application queue for sending and receiving messages from AWS
    /// </summary>
    public class JungleQueue : IRunJungleQueue
    {
        /// <summary>
        /// Logger instance
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(JungleQueue));

        /// <summary>
        /// SQS queue
        /// </summary>
        private readonly ISqsQueue _queue;

        /// <summary>
        /// Receive event message pump
        /// </summary>
        private readonly MessagePump _messagePump;

        /// <summary>
        /// Receive event message pump
        /// </summary>
        private Task _messagePumpTask;

        /// <summary>
        /// Message Logger
        /// </summary>
        private readonly IMessageLogger _messageLogger;

        /// <summary>
        /// Message serializer
        /// </summary>
        private readonly IMessageSerializer _messageSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JungleQueue" /> class.
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        public JungleQueue(QueueConfiguration configuration)
        {
            _messageLogger = configuration.MessageLogger;
            _messageSerializer = configuration.MessageSerializer;
            Action<IObjectBuilder> queuePreHandler = x =>
            {
                x.RegisterInstance(CreateSendQueue());
                configuration.Prehandler?.Invoke(x);
            };
            _queue = new SqsQueue(configuration.Region, configuration.QueueName, configuration.RetryCount);
            _queue.WaitTimeSeconds = configuration.SqsPollWaitTime;
            if (configuration.MaxSimultaneousMessages > 0)
            {
                MessageParser parser = new MessageParser(configuration.MessageSerializer);
                MessageProcessor messageProcessor = new MessageProcessor(configuration.Handlers, configuration.FaultHandlers, configuration.ObjectBuilder, queuePreHandler);
                _messagePump = new MessagePump(_queue, configuration.RetryCount, messageProcessor, _messageLogger, parser);
                _messagePump.MaxSimultaneousMessages = configuration.MaxSimultaneousMessages;
            }
        }

        /// <summary>
        /// Gets an instance of the queue that can send messages
        /// </summary>
        /// <returns>Instance of the queue</returns>
        public IQueue CreateSendQueue()
        {
            _queue.Init().Wait();
            return new TransactionalQueue(_queue, _messageLogger, _messageSerializer);
        }

        /// <summary>
        /// Starts the queue receiving and processing messages
        /// </summary>
        public void StartReceiving()
        {
            if (_messagePump == null)
            {
                throw new InvalidOperationException("Queue is not configured for receive operations");
            }

            _queue.Init().Wait();
            Log.Info("Starting message pumps");
            _messagePumpTask = _messagePump.Run();
        }

        /// <summary>
        /// Triggers the queue to stop processing new messages
        /// </summary>
        public void StopReceiving()
        {
            Log.Info("Stopping the queue");
            _messagePump.Stop();
            Task.WaitAll(_messagePumpTask);
            _messagePump.Dispose();
            _messagePumpTask = null;
        }
    }
}
