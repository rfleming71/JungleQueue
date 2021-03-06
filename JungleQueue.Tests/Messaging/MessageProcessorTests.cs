﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logging;
using JungleQueue.Interfaces;
using JungleQueue.Interfaces.IoC;
using JungleQueue.Interfaces.Statistics;
using JungleQueue.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace JungleQueue.Tests.Messaging
{
    [TestClass]
    public class MessageProcessorTests
    {
        private MessageProcessor _processor;
        private Mock<IObjectBuilder> _objectBuilder;
        private Dictionary<Type, HashSet<Type>> _typeMapping;
        private Dictionary<Type, HashSet<Type>> _faultHandlers;
        private static int _testHandler1Called;
        private static int _testHandler2Called;
        private static int _testHandler3Called;
        private static int _testFaultHandler1Called;
        private static int _testFaultHandler2Called;
        private static int _testFaultHandler3Called;
        private static int _testStatHandler1Called;
        private static int _testStatHandler2Called;
        private static int _transportMessageFaultHandler;
        private TransportMessage _message;

        [TestInitialize]
        public void TestInitialize()
        {
            _testHandler1Called = 0;
            _testHandler2Called = 0;
            _testHandler3Called = 0;
            _testFaultHandler1Called = 0;
            _testFaultHandler2Called = 0;
            _testFaultHandler3Called = 0;
            _transportMessageFaultHandler = 0;
            _testStatHandler1Called = 0;
            _testStatHandler2Called = 0;
            _typeMapping = new Dictionary<Type, HashSet<Type>>()
            {
                { typeof(TestMessage), new HashSet<Type>() { typeof(TestHandler), typeof(TestHandler2) } },
                { typeof(TestHandler), new HashSet<Type>() { typeof(TestHandler), typeof(TestHandler2) } },
            };

            _faultHandlers = new Dictionary<Type, HashSet<Type>>()
            {
                { typeof(TestMessage), new HashSet<Type>() { typeof(TestFaultHandler1), typeof(TestFaultHandler2) } },
                { typeof(TestHandler), new HashSet<Type>() { typeof(TestFaultHandler1), typeof(TestFaultHandler2) } },
            };

            _objectBuilder = new Mock<IObjectBuilder>(MockBehavior.Strict);
            _objectBuilder.Setup(x => x.GetNestedBuilder()).Returns(_objectBuilder.Object);
            _objectBuilder.Setup(x => x.Dispose());
            _objectBuilder.Setup(x => x.RegisterInstance<ILog>(It.IsAny<ILog>()));
            _objectBuilder.Setup(x => x.GetValue(It.IsAny<Type>())).Returns<Type>(x => Activator.CreateInstance(x));
            _objectBuilder.Setup(x => x.GetValues<IWantMessageStatistics>()).Returns(new IWantMessageStatistics[] { new TestStatHandler1(), new TestStatHandler2() });

            _processor = new MessageProcessor(_typeMapping, _faultHandlers, _objectBuilder.Object);
            _message = new TransportMessage() { MessageType = typeof(TestMessage), Message = new TestMessage(), MessageParsingSucceeded = true, };
        }

        [TestMethod]
        public async Task MessageProcessorTests_process()
        {
            MessageProcessingResult result = await _processor.ProcessMessage(_message);
            Assert.IsTrue(result.WasSuccessful);
            Assert.AreEqual(1, _testHandler1Called);
            Assert.AreEqual(1, _testHandler2Called);
            Assert.AreEqual(0, _testHandler3Called);
        }

        [TestMethod]
        public async Task MessageProcessorTests_process_with_preHandler()
        {
            int preHandlerCallCount = 0;
            _processor = new MessageProcessor(_typeMapping, _faultHandlers, _objectBuilder.Object, x => preHandlerCallCount++);
            MessageProcessingResult result = await _processor.ProcessMessage(_message);
            Assert.IsTrue(result.WasSuccessful);
            Assert.AreEqual(1, _testHandler1Called);
            Assert.AreEqual(1, _testHandler2Called);
            Assert.AreEqual(0, _testHandler3Called);
            Assert.AreEqual(2, preHandlerCallCount);
        }

        [TestMethod]
        public async Task MessageProcessorTests_Exception_Thrown_in_Handler()
        {
            _typeMapping[typeof(TestMessage)].Add(typeof(TestHandler3));
            MessageProcessingResult result = await _processor.ProcessMessage(_message);
            Assert.IsFalse(result.WasSuccessful);
            Assert.AreEqual(1, _testHandler1Called);
            Assert.AreEqual(1, _testHandler2Called);
            Assert.AreEqual(1, _testHandler3Called);
        }

        [TestMethod]
        public async Task MessageProcessorTests_Unknown_message()
        {
            _typeMapping.Remove(typeof(TestMessage));
            MessageProcessingResult result = await _processor.ProcessMessage(_message);
            Assert.IsFalse(result.WasSuccessful);
            Assert.AreEqual(0, _testHandler1Called);
            Assert.AreEqual(0, _testHandler2Called);
            Assert.AreEqual(0, _testHandler3Called);
        }

        [TestMethod]
        public async Task MessageProcessorTests_process_fault()
        {
            await _processor.ProcessFaultedMessage(_message, new Exception("Foo!"));
            Assert.AreEqual(1, _testFaultHandler1Called);
            Assert.AreEqual(1, _testFaultHandler2Called);
            Assert.AreEqual(0, _testFaultHandler3Called);
        }

        [TestMethod]
        public async Task MessageProcessorTests_process_fault_with_preHandler()
        {
            int preHandlerCallCount = 0;
            _processor = new MessageProcessor(_typeMapping, _faultHandlers, _objectBuilder.Object, x => preHandlerCallCount++);
            await _processor.ProcessFaultedMessage(_message, new Exception("Foo!"));
            Assert.AreEqual(1, _testFaultHandler1Called);
            Assert.AreEqual(1, _testFaultHandler2Called);
            Assert.AreEqual(0, _testFaultHandler3Called);
            Assert.AreEqual(2, preHandlerCallCount);
        }

        [TestMethod]
        public async Task MessageProcessorTests_process_general_and_message_handlers()
        {
            _faultHandlers[typeof(TransportMessage)] = new HashSet<Type>() { typeof(TestFaultHandler) };
            await _processor.ProcessFaultedMessage(_message, new Exception("Foo!"));
            Assert.AreEqual(1, _testFaultHandler1Called);
            Assert.AreEqual(1, _testFaultHandler2Called);
            Assert.AreEqual(0, _testFaultHandler3Called);
            Assert.AreEqual(1, _transportMessageFaultHandler);
        }

        [TestMethod]
        public async Task MessageProcessorTests_process_general_handlers()
        {
            _faultHandlers.Remove(typeof(TestMessage));
            _faultHandlers[typeof(TransportMessage)] = new HashSet<Type>() { typeof(TestFaultHandler) };
            await _processor.ProcessFaultedMessage(_message, new Exception("Foo!"));
            Assert.AreEqual(0, _testFaultHandler1Called);
            Assert.AreEqual(0, _testFaultHandler2Called);
            Assert.AreEqual(0, _testFaultHandler3Called);
            Assert.AreEqual(1, _transportMessageFaultHandler);
        }

        [TestMethod]
        public async Task MessageProcessorTests_process_general_handlers_on_parse_failure()
        {
            _faultHandlers[typeof(TransportMessage)] = new HashSet<Type>() { typeof(TestFaultHandler) };
            _message.MessageParsingSucceeded = false;
            await _processor.ProcessFaultedMessage(_message, new Exception("Foo!"));
            Assert.AreEqual(0, _testFaultHandler1Called);
            Assert.AreEqual(0, _testFaultHandler2Called);
            Assert.AreEqual(0, _testFaultHandler3Called);
            Assert.AreEqual(1, _transportMessageFaultHandler);
        }

        [TestMethod]
        public async Task MessageProcessorTests_Exception_Thrown_Fault_Handler()
        {
            _faultHandlers[typeof(TestMessage)].Add(typeof(TestFaultHandler3));
            await _processor.ProcessFaultedMessage(_message, new Exception("Foo!"));
            Assert.AreEqual(1, _testFaultHandler1Called);
            Assert.AreEqual(1, _testFaultHandler2Called);
            Assert.AreEqual(1, _testFaultHandler3Called);
        }

        [TestMethod]
        public async Task MessageProcessorTests_Exception_Thrown_IfNull()
        {
            try
            {
                await _processor.ProcessMessage(null);
                Assert.Fail();
            }
            catch
            {

            }
        }

        [TestMethod]
        public async Task MessageProcessorTests_Process_Message_Stats()
        {
            await _processor.ProcessMessageStatistics(new MessageStatistics());
            Assert.AreEqual(1, _testStatHandler1Called);
            Assert.AreEqual(1, _testStatHandler2Called);
        }

        [TestMethod]
        public async Task MessageProcessorTests_Process_Message_Stats_With_No_Handlers()
        {
            _objectBuilder.Setup(x => x.GetValues<IWantMessageStatistics>()).Returns<IEnumerable<IWantMessageStatistics>>(null);
            await _processor.ProcessMessageStatistics(new MessageStatistics());
            Assert.AreEqual(0, _testStatHandler1Called);
            Assert.AreEqual(0, _testStatHandler2Called);
        }

        #region TEST HANDLERS

        class TestHandler : IHandleMessage<TestMessage>
        {
            public Task Handle(TestMessage message)
            {
                ++_testHandler1Called;
                return Task.CompletedTask;
            }
        }

        class TestHandler2 : IHandleMessage<TestMessage>
        {
            public Task Handle(TestMessage message)
            {
                ++_testHandler2Called;
                return Task.CompletedTask;
            }
        }

        class TestHandler3 : IHandleMessage<TestMessage>
        {
            public Task Handle(TestMessage message)
            {
                ++_testHandler3Called;
                throw new Exception("Exception!");
            }
        }

        class TestFaultHandler1 : IHandleMessageFaults<TestMessage>
        {
            public Task Handle(TestMessage message, Exception ex)
            {
                ++_testFaultHandler1Called;
                throw new Exception("Exception!");
            }
        }

        class TestFaultHandler2 : IHandleMessageFaults<TestMessage>
        {
            public Task Handle(TestMessage message, Exception ex)
            {
                ++_testFaultHandler2Called;
                return Task.CompletedTask;
            }
        }

        class TestFaultHandler3 : IHandleMessageFaults<TestMessage>
        {
            public Task Handle(TestMessage message, Exception ex)
            {
                ++_testFaultHandler3Called;
                throw new Exception("Exception!");
            }
        }

        class TestFaultHandler : IHandleMessageFaults<TransportMessage>
        {
            public Task Handle(TransportMessage message, Exception ex)
            {
                ++_transportMessageFaultHandler;
                return Task.CompletedTask;
            }
        }

        class TestStatHandler1 : IWantMessageStatistics
        {
            public Task ReceiveStatisitics(IMessageStatistics statistics)
            {
                ++_testStatHandler1Called;
                return Task.CompletedTask;
            }
        }

        class TestStatHandler2 : IWantMessageStatistics
        {
            public Task ReceiveStatisitics(IMessageStatistics statistics)
            {
                ++_testStatHandler2Called;
                return Task.CompletedTask;
            }
        }

        #endregion TEST HANDLERS
    }
}
