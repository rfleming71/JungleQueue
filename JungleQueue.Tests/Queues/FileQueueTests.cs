using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JungleQueue.Queues.File;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace JungleQueue.Tests.Queues
{
    [TestClass]
    public class FileQueueTests
    {
        private Mock<IFileSystemFacade> _fileSystemFacade;
        private const string QueueFolder = "C:\\temp\\queue\\";
        private const int RetryCount = 5;
        private FileQueue _queue;
        private Dictionary<string, string> _fileSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            _fileSystem = new Dictionary<string, string>();
            _fileSystemFacade = new Mock<IFileSystemFacade>(MockBehavior.Strict);
            _fileSystemFacade.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Callback((string x, string y) => { _fileSystem[x] = y; });
            _queue = new FileQueue(QueueFolder, RetryCount, _fileSystemFacade.Object);
        }

        [TestMethod]
        public void TestSendMessage()
        {
            _queue.AddMessage("message body", new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("test1", "value1"),
                new KeyValuePair<string, string>("test2", "value2"),
                new KeyValuePair<string, string>("test3", "value3"),
            });

            _fileSystemFacade.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            Assert.AreEqual(1, _fileSystem.Count);
            Assert.IsTrue(_fileSystem.Keys.First().StartsWith(QueueFolder));
        }
    }
}
