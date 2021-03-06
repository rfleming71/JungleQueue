﻿using System;
using System.Threading.Tasks;
using Common.Logging;
using JungleQueue.Interfaces;
using TestApp.Messages;

namespace ConsoleTestApp.FaultHandlers
{
    public class FaultHandler2 : IHandleMessageFaults<TestMessage>
    {
        private readonly IQueue _queue;
        private readonly ILog _log;
        public FaultHandler2(IQueue queue, ILog log)
        {
            _queue = queue;
            _log = log;
        }

        public FaultHandler2()
        {
            _log = LogManager.GetLogger<FaultHandler2>();
        }

        public Task Handle(TestMessage message, Exception ex)
        {
            _log.Info("Starting message fault Handler 2");
            return Task.FromResult(true);
        }
    }
}
