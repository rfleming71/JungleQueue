using System.Threading;
using Common.Logging;
using JungleQueue.Interfaces;
using TestApp.Messages;

namespace TestApp.Handlers
{
    public class Handler1 : IHandleMessage<TestMessage>
    {
        private readonly IQueue _queue;
        private readonly ILog _log;
        public Handler1(IQueue queue, ILog log)
        {
            _queue = queue;
            _log = log;
        }

        public Handler1()
        {
            _log = LogManager.GetLogger<Handler1>();
        }

        public void Handle(TestMessage message)
        {
            _log.Info("Starting message Handler 1");
            Thread.Sleep(5000);
            _log.Info("Finished message Handler 1");
        }
    }
}
