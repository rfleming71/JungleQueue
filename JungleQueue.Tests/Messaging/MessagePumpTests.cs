﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JungleQueue.Aws.Sqs;
using JungleQueue.Interfaces;
using JungleQueue.Interfaces.Statistics;
using JungleQueue.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace JungleQueue.Tests.Messaging
{
    [TestClass]
    public class MessagePumpTests
    {
        private MessagePump _messagePump;
        private Mock<ISqsQueue> _queue;
        private Mock<IMessageProcessor> _messageProcessor;
        private Mock<IMessageLogger> _messageLogger;
        private TransportMessage _message;
        private const int MaxTryCount = 5;

        [TestInitialize]
        public void TestInitialized()
        {
            _messageLogger = new Mock<IMessageLogger>();
            _message = new TransportMessage()
            {
                ReceiptHandle = "123",
                MessageParsingSucceeded = true,
                Body = "{}",
                Message = "new message",
            };
            _queue = new Mock<ISqsQueue>(MockBehavior.Strict);
            _messageProcessor = new Mock<IMessageProcessor>(MockBehavior.Strict);
            _messageProcessor.Setup(x => x.ProcessMessage(It.IsAny<TransportMessage>())).Returns(Task.FromResult(new MessageProcessingResult() { WasSuccessful = true }));
            _messageProcessor.Setup(x => x.ProcessFaultedMessage(It.IsAny<TransportMessage>(), It.IsAny<Exception>())).Returns(Task.CompletedTask);
            _messageProcessor.Setup(x => x.ProcessMessageStatistics(It.IsAny<IMessageStatistics>())).Returns(Task.CompletedTask);

            _messagePump = new MessagePump(_queue.Object, MaxTryCount, _messageProcessor.Object, _messageLogger.Object, null);
            _messagePump.MaxSimultaneousMessages = 1;

            _queue.Setup(x => x.GetMessages(It.IsAny<IMessageParser>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult((IEnumerable<TransportMessage>)new[] { _message }))
                .Callback(() => _messagePump.Stop());
            _queue.Setup(x => x.RemoveMessage(It.IsAny<TransportMessage>())).Returns(Task.CompletedTask);
        }

        [TestMethod]
        public async Task MessagePumpTests_SinglePass_success()
        {
            await _messagePump.Run();
            await Task.Delay(500);
            _queue.Verify(x => x.GetMessages(It.IsAny<IMessageParser>(), It.IsAny<CancellationToken>()), Times.Once());
            _queue.Verify(x => x.RemoveMessage(It.Is<TransportMessage>(t => t.ReceiptHandle == "123")), Times.Once());
            _messageProcessor.Verify(x => x.ProcessMessage(It.IsAny<TransportMessage>()), Times.Once());
        }

        [TestMethod]
        public async Task MessagePumpTests_SinglePass_message_parse_failure()
        {
            _message.MessageParsingSucceeded = false;
            await _messagePump.Run();
            await Task.Delay(500);
            _queue.Verify(x => x.GetMessages(It.IsAny<IMessageParser>(), It.IsAny<CancellationToken>()), Times.Once());
            _queue.Verify(x => x.RemoveMessage(It.IsAny<TransportMessage>()), Times.Never());
            _messageProcessor.Verify(x => x.ProcessMessage(It.IsAny<TransportMessage>()), Times.Never());
        }

        [TestMethod]
        public async Task MessagePumpTests_SinglePass_processing_failure()
        {
            _message.AttemptNumber = 1;
            _messageProcessor.Setup(x => x.ProcessMessage(It.IsAny<TransportMessage>())).Returns(Task.FromResult(new MessageProcessingResult() { WasSuccessful = false }));
            await _messagePump.Run();
            await Task.Delay(500);
            _queue.Verify(x => x.GetMessages(It.IsAny<IMessageParser>(), It.IsAny<CancellationToken>()), Times.Once());
            _queue.Verify(x => x.RemoveMessage(It.IsAny<TransportMessage>()), Times.Never());
            _messageProcessor.Verify(x => x.ProcessMessage(It.IsAny<TransportMessage>()), Times.Once());
            _messageProcessor.Verify(x => x.ProcessFaultedMessage(It.IsAny<TransportMessage>(), It.IsAny<Exception>()), Times.Never());
        }

        [TestMethod]
        public async Task MessagePumpTests_SinglePass_FinalPass_processing_failure()
        {
            _message.AttemptNumber = MaxTryCount;
            _messageProcessor.Setup(x => x.ProcessMessage(It.IsAny<TransportMessage>())).Returns(Task.FromResult(new MessageProcessingResult() { WasSuccessful = false, Exception = new Exception()}));
            await _messagePump.Run();
            await Task.Delay(500);
            _queue.Verify(x => x.GetMessages(It.IsAny<IMessageParser>(), It.IsAny<CancellationToken>()), Times.Once());
            _queue.Verify(x => x.RemoveMessage(It.IsAny<TransportMessage>()), Times.Never());
            _messageProcessor.Verify(x => x.ProcessMessage(It.IsAny<TransportMessage>()), Times.Once());
            _messageProcessor.Verify(x => x.ProcessFaultedMessage(It.IsAny<TransportMessage>(), It.IsAny<Exception>()), Times.Once());
        }
    }
}
