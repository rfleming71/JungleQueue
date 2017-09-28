// <copyright file="SqsQueue.cs">
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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JungleQueue.Interfaces.Exceptions;
using JungleQueue.Messaging;

namespace JungleQueue.Aws.Sqs
{
    /// <summary>
    /// Represents the SQS queue in Amazon AWS
    /// </summary>
    public sealed class SqsQueue : ISqsQueue, IDisposable
    {
        /// <summary>
        /// Underlying queue name
        /// </summary>
        private readonly string _queueName;

        /// <summary>
        /// Number of times to retry a message before moving it to the dead letter queue
        /// </summary>
        private readonly int _retryCount;

        /// <summary>
        /// URL for the underlying queue
        /// </summary>
        private string _queueUrl;

        /// <summary>
        /// ARN for the underlying queue
        /// </summary>
        private string _queueArn;

        /// <summary>
        /// Instance of the SQS service
        /// </summary>
        private IAmazonSQS _simpleQueueService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqsQueue" /> class.
        /// </summary>
        /// <param name="endpoint">Region the queue is in</param>
        /// <param name="queueName">Name of the queue</param>
        /// <param name="retryCount">Number of times to retry a message before moving it to the dead letter queue</param>
        public SqsQueue(RegionEndpoint endpoint, string queueName, int retryCount)
        {
            _simpleQueueService = new AmazonSQSClient(endpoint);
            _queueName = queueName;
            _retryCount = retryCount;
            MaxNumberOfMessages = 1;
        }

        /// <summary>
        /// Gets the URL for the queue
        /// </summary>
        public string Url { get { return _queueUrl; } }

        /// <summary>
        /// Gets or sets the number of seconds to long poll for
        /// </summary>
        public int WaitTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of messages the request can retrieve
        /// </summary>
        public int MaxNumberOfMessages { get; set; }

        /// <summary>
        /// Initializes the queue
        /// </summary>
        /// <returns>Task</returns>
        public async Task Init()
        {
            if (!string.IsNullOrWhiteSpace(_queueUrl))
            {
                return;
            }

            CreateQueueResponse createResponse = await _simpleQueueService.CreateQueueAsync(_queueName);
            _queueUrl = createResponse.QueueUrl;
            var attributes = await _simpleQueueService.GetAttributesAsync(_queueUrl);
            _queueArn = attributes["QueueArn"];
            if (!attributes.ContainsKey("RedrivePolicy"))
            {
                createResponse = await _simpleQueueService.CreateQueueAsync(_queueName + "_Dead_Letter");
                string deadLetterQueue = createResponse.QueueUrl;
                var deadLetterAttributes = await _simpleQueueService.GetAttributesAsync(deadLetterQueue);
                string redrivePolicy = string.Format(CultureInfo.InvariantCulture, "{{\"maxReceiveCount\":\"{0}\", \"deadLetterTargetArn\":\"{1}\" }}", _retryCount, deadLetterAttributes["QueueArn"]);
                await _simpleQueueService.SetQueueAttributesAsync(_queueUrl, new Dictionary<string, string>() { { "RedrivePolicy", redrivePolicy }, { "MessageRetentionPeriod", "1209600" } });
                await _simpleQueueService.SetQueueAttributesAsync(createResponse.QueueUrl, new Dictionary<string, string>() { { "MessageRetentionPeriod", "1209600" } });
            }
        }

        /// <summary>
        /// Retrieve messages from the underlying queue
        /// </summary>
        /// <param name="messageParser">Parses the inbound messages</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Messages or empty</returns>
        public async Task<IEnumerable<TransportMessage>> GetMessages(IMessageParser messageParser, CancellationToken cancellationToken)
        {
            ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
            receiveMessageRequest.WaitTimeSeconds = WaitTimeSeconds;
            receiveMessageRequest.MaxNumberOfMessages = MaxNumberOfMessages;
            receiveMessageRequest.AttributeNames = new List<string>() { "ApproximateReceiveCount" };
            receiveMessageRequest.MessageAttributeNames = new List<string>() { "messageType", "fromSns" };
            receiveMessageRequest.QueueUrl = _queueUrl;

            ReceiveMessageResponse receiveMessageResponse = await _simpleQueueService.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);
            return receiveMessageResponse.Messages.Select(x => messageParser.ParseMessage(x)).ToList();
        }

        /// <summary>
        /// Removes a message from the queue
        /// </summary>
        /// <param name="message">Message to remove</param>
        public async Task RemoveMessage(TransportMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (string.IsNullOrWhiteSpace(message.ReceiptHandle))
            {
                throw new JungleException("Invalid receipt handle");
            }

            await _simpleQueueService.DeleteMessageAsync(_queueUrl, message.ReceiptHandle);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, 
        /// or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_simpleQueueService != null)
            {
                _simpleQueueService.Dispose();
                _simpleQueueService = null;
            }
        }

        /// <summary>
        /// Adds the message to the queue
        /// </summary>
        /// <param name="message">Message to add to the queue</param>
        /// <param name="metadata">Message metadata</param>
        public async Task AddMessage(string message, IEnumerable<KeyValuePair<string, string>> metadata)
        {
            SendMessageRequest request = new SendMessageRequest(_queueUrl, message);
            foreach (KeyValuePair<string, string> kvp in metadata)
            {
                request.MessageAttributes[kvp.Key] = new MessageAttributeValue() { StringValue = kvp.Value, DataType = "String" };
            }

            await _simpleQueueService.SendMessageAsync(request);
        }
    }
}
