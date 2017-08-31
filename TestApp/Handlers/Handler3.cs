using System.Threading.Tasks;
using Common.Logging;
using JungleQueue.Interfaces;
using TestApp.Messages;

namespace TestApp.Handlers
{
    public class Handler3 : IHandleMessage<TestMessage2>
    {
        private readonly ILog _log;
        public Handler3(ILog log)
        {
            _log = log;
        }

        public Handler3()
        {
            _log = LogManager.GetLogger<Handler3>();
        }

        public Task Handle(TestMessage2 message)
        {
            _log.Info("Handling message Handler 3");
            return Task.CompletedTask;
        }
    }
}
