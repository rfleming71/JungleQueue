using System.Threading;
using Common.Logging;
using JungleQueue.Interfaces;
using TestApp.Messages;

namespace TestApp.Handlers
{
    public class Handler2 : IHandleMessage<TestMessage>
    {
        private readonly IQueue _queue;
        private readonly ILog _log;
        public Handler2(IQueue queue, ILog log)
        {
            _queue = queue;
            _log = log;
        }

        public Handler2()
        {
            _log = LogManager.GetCurrentClassLogger();
        }

        public void Handle(TestMessage message)
        {
            _log.Info("Starting message Handler 2");
            Thread.Sleep(10000);
            /*_queue.Send<TestMessage2>(x =>
            {
                x.ID = 1;
                x.Modified = 2;
            });

            _log.Info("Sent TestMessage2");*/
            _log.Info("Finished message Handler 2");
        }
    }
}
