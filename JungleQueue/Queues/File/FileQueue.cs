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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JungleQueue.Messaging;
using Newtonsoft.Json;

namespace JungleQueue.Queues.File
{
    /// <summary>
    /// Queue based on the file system
    /// </summary>
    public class FileQueue : IProviderQueue
    {
        private readonly string _queueFolder;
        private readonly string _deadLetterQueueFolder;
        private readonly int _retries;
        private readonly IFileSystemFacade _fileSystemFacade;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="folder">Folder to use as the queue</param>
        /// <param name="retries">Number of times to attempt each message</param>
        public FileQueue(string folder, int retries, IFileSystemFacade fileSystemFacade = null)
        {
            _queueFolder = folder;
            _deadLetterQueueFolder = Path.Combine(_queueFolder, "DeadLetter");
            _retries = retries;
            _fileSystemFacade = fileSystemFacade ?? new FileSystemFacade();
        }

        /// <summary>
        /// Gets the queue's url
        /// </summary>
        public string Url => throw new NotImplementedException();

        /// <summary>
        /// Polling wait time
        /// </summary>
        public int WaitTimeSeconds { get; set; }

        /// <summary>
        /// Retrieve messages from the underlying queue
        /// </summary>
        /// <param name="messageParser">Parses the inbound messages</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Messages or empty</returns>
        public Task AddMessage(string message, IEnumerable<KeyValuePair<string, string>> metadata)
        {
            Guid messageId = Guid.NewGuid();
            MessageWrapper wrapper = new MessageWrapper()
            {
                Id = messageId,
                Attempts = 0,
                Body = message,
                Metadata = metadata,
                NextAttemptTime = DateTime.Now,
                SentTime = DateTime.Now,
            };

            WriteMessage(wrapper, false);
            return Task.FromResult(true);
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Adds the message to the queue
        /// </summary>
        /// <param name="message">Message to add to the queue</param>
        /// <param name="metadata">Message metadata</param>
        public Task<IEnumerable<TransportMessage>> GetMessages(IMessageParser messageParser, CancellationToken cancellationToken)
        {
            IEnumerable<string> files = _fileSystemFacade.GetFilesInDirectory(_queueFolder);
            foreach (string file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string messageJson = _fileSystemFacade.ReadAllText(file);
                MessageWrapper wrapper = JsonConvert.DeserializeObject<MessageWrapper>(messageJson);
                if (!wrapper.NextAttemptTime.HasValue || wrapper.NextAttemptTime < DateTime.Now)
                {
                    if (wrapper.Attempts >= _retries)
                    {
                        WriteMessage(wrapper, true);
                    }
                    else
                    {
                        Amazon.SQS.Model.Message awsMessage = new Amazon.SQS.Model.Message()
                        {
                            Body = wrapper.Body,
                            MessageId = wrapper.Id.ToString(),
                            ReceiptHandle = wrapper.Id.ToString(),
                            MessageAttributes = new Dictionary<string, Amazon.SQS.Model.MessageAttributeValue>(),
                        };
                        foreach (var thing in wrapper.Metadata)
                        {
                            awsMessage.MessageAttributes[thing.Key] = new Amazon.SQS.Model.MessageAttributeValue() { StringValue = thing.Value };
                        }
                        wrapper.NextAttemptTime = DateTime.Now.AddMinutes(1);
                        wrapper.Attempts++;
                        WriteMessage(wrapper, false);
                        return Task.FromResult<IEnumerable<TransportMessage>>(new[] { messageParser.ParseMessage(awsMessage) });
                    }
                }
            }

            return Task.FromResult(Enumerable.Empty<TransportMessage>());
        }

        /// <summary>
        /// Initializes the queue
        /// </summary>
        /// <returns>Task</returns>
        public Task Init()
        {
            _fileSystemFacade.CreateDiretory(_queueFolder);
            _fileSystemFacade.CreateDiretory(_deadLetterQueueFolder);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Removes a message from the queue
        /// </summary>
        /// <param name="message">Message to remove</param>
        public Task RemoveMessage(TransportMessage message)
        {
            string messagePath = Path.Combine(_queueFolder, message.ReceiptHandle);
            _fileSystemFacade.DeleteFile(messagePath);
            return Task.FromResult(true);
        }

        private void WriteMessage(MessageWrapper wrapper, bool toDeadLetter)
        {
            string folder = toDeadLetter ? _deadLetterQueueFolder : _queueFolder;
            string messageJson = JsonConvert.SerializeObject(wrapper);
            _fileSystemFacade.WriteAllText(Path.Combine(folder, wrapper.Id.ToString()), messageJson);
            if (toDeadLetter)
            {
                string messagePath = Path.Combine(_queueFolder, wrapper.Id.ToString());
                _fileSystemFacade.DeleteFile(messagePath);
            }
        }
    }
}
