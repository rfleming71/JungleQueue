using System;
using System.Threading.Tasks;
using Common.Logging;
using JungleQueue.Interfaces;
using TestApp.Messages;

namespace TestApp.FaultHandlers
{
    public class FaultHandler1 : IHandleMessageFaults<TestMessage>
    {
        private readonly IQueue _queue;
        private readonly ILog _log;
        public FaultHandler1(IQueue queue, ILog log)
        {
            _queue = queue;
            _log = log;
        }

        public FaultHandler1()
        {
            _log = LogManager.GetLogger<FaultHandler1>();
        }

        public Task Handle(TestMessage message, Exception ex)
        {
            _log.Info("Starting message fault Handler 1");
            return Task.CompletedTask;
        }
    }
}
