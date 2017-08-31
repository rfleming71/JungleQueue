using System;
using Common.Logging;
using JungleQueue.Interfaces;
using JungleQueue.Messaging;
using TestApp.Messages;

namespace ConsoleTestApp.FaultHandlers
{
    public class FaultHandler3 : IHandleMessageFaults<TransportMessage>
    {
        private readonly IQueue _queue;
        private readonly ILog _log;
        public FaultHandler3(IQueue queue, ILog log)
        {
            _queue = queue;
            _log = log;
        }

        public FaultHandler3()
        {
            _log = LogManager.GetCurrentClassLogger();
        }

        public void Handle(TransportMessage message, Exception ex)
        {
            _log.Info("Starting message fault Handler 3");
        }
    }
}
