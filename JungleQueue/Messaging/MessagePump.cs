// <copyright file="MessagePump.cs">
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
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JungleQueue.Aws.Sqs;

namespace JungleQueue.Messaging
{
    /// <summary>
    /// Responsible for polling SQS queue and dispatching the events
    /// </summary>
    internal sealed class MessagePump : IDisposable
    {
        /// <summary>
        /// Instance of the logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(MessagePump));

        /// <summary>
        /// Message processor, handles calling the event handlers
        /// </summary>
        private readonly IMessageProcessor _messageProcessor;

        /// <summary>
        /// Queue to read messages from
        /// </summary>
        private readonly ISqsQueue _queue;

        /// <summary>
        /// Number of times to retry a message
        /// </summary>
        private readonly int _messageRetryCount;

        /// <summary>
        /// Message logger
        /// </summary>
        private readonly IMessageLogger _messageLogger;

        /// <summary>
        /// Object to parse inbound messages
        /// </summary>
        private readonly IMessageParser _messageParser;

        /// <summary>
        /// Token used to control when to stop the pump
        /// </summary>
        private CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePump" /> class.
        /// </summary>
        /// <param name="queue">Queue to read messages from</param>
        /// <param name="messageRetryCount">Number of times to retry a message</param>
        /// <param name="messageProcessor">Class for calling out to event handlers</param>
        /// <param name="messageLogger">Instance of the message logger</param>
        /// <param name="messageParser">Parses inbound messages</param>
        public MessagePump(ISqsQueue queue, int messageRetryCount, IMessageProcessor messageProcessor, IMessageLogger messageLogger, IMessageParser messageParser)
        {
            _queue = queue;
            _messageRetryCount = messageRetryCount;
            _messageProcessor = messageProcessor;
            _cancellationToken = new CancellationTokenSource();
            _messageLogger = messageLogger;
            _messageParser = messageParser;
        }

        /// <summary>
        /// Gets or sets the maximum number messages to process
        /// simultaneously
        /// </summary>
        public int MaxSimultaneousMessages { get; set; }

        /// <summary>
        /// Starts the loop for polling the SQS queue
        /// </summary>
        public async Task Run()
        {
            Log.InfoFormat("Starting message pump with max sim messages of {0}", MaxSimultaneousMessages);
            SemaphoreSlim semaphore = new SemaphoreSlim(MaxSimultaneousMessages);
            Delayer messageCheckDelay = new Delayer(); 
            while (!_cancellationToken.IsCancellationRequested)
            {
                Log.Trace("Starting receiving call");
                try
                {
                    await messageCheckDelay.Delay(_cancellationToken.Token);
                    await semaphore.WaitAsync(_cancellationToken.Token); // Wait for a worker slot to become available
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    IEnumerable<TransportMessage> ReceivedMessages = await _queue.GetMessages(_messageParser, _cancellationToken.Token);
                    Log.TraceFormat("Received {0} messages", ReceivedMessages.Count());
                    if (!ReceivedMessages.Any())
                    {
                        semaphore.Release(); // Mark the slot as free again if we aren't going to use it
                    }
                    else
                    {
                        messageCheckDelay.Reset();
                    }

                    foreach (TransportMessage message in ReceivedMessages)
                    {
                        ThreadPool.QueueUserWorkItem(async _ =>
                        {
                            await ProcessMessage(message);
                            semaphore.Release(); // Mark the slot as free again to resume message polling
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Log.Error("Error occurred in message pump run", ex);
                }
            }
        }

        private async Task ProcessMessage(TransportMessage message)
        {
            Log.InfoFormat("Received message of type '{0}'", message.MessageTypeName);
            _messageLogger.InboundLogMessage(message.Body, message.MessageTypeName, message.Id, message.AttemptNumber);
            MessageProcessingResult result;
            if (message.MessageParsingSucceeded)
            {
                Log.Trace("Processing message");
                result = await _messageProcessor.ProcessMessage(message);
                Log.TraceFormat("Processed message - Error: {0}", !result.WasSuccessful);
            }
            else
            {
                Log.ErrorFormat("Failed to parse message of type {0}", message.Exception, message.MessageTypeName);
                result = new MessageProcessingResult() { WasSuccessful = false, Exception = new Exception("Message parse failure") };
            }

            if (result.WasSuccessful)
            {
                Log.Info("Removing message from the queue");
                await _queue.RemoveMessage(message);
            }
            else if (message.AttemptNumber == _messageRetryCount)
            {
                Log.Info("Message faulted ");
                await _messageProcessor.ProcessFaultedMessage(message, result.Exception);
            }

            MessageStatistics stats = new MessageStatistics()
            {
                FinalAttempt = message.AttemptNumber == _messageRetryCount,
                HandlerRunTime = result.Runtime,
                MessageLength = message.Body.Length,
                MessageType = message.MessageTypeName,
                Success = result.WasSuccessful,
                PreviousRetryCount = message.AttemptNumber,
            };
            await _messageProcessor.ProcessMessageStatistics(stats);
        }

        /// <summary>
        /// Stops the loop for polling the SQS queue
        /// </summary>
        public void Stop()
        {
            Log.Info("Stop requested");
            _cancellationToken.Cancel();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            if (_cancellationToken != null)
            {
                _cancellationToken.Dispose();
                _cancellationToken = null;
            }
        }
    }
}
