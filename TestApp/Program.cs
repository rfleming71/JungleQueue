using System;
using Amazon;
using JungleQueue.Configuration;
using JungleQueue.Interfaces;
using TestApp.Messages;

namespace TestApp
{
    class Program
    {
        //private static IContainer _container;

        static void Main(string[] args)
        {
            //_container = new Container();
            //_container.Configure(x =>
            //{
            //    x.For<IWantMessageStatistics>().Use<StatsTracker>();
            //});

            IRunJungleQueue queue = CreateFullQueue();
            IQueue sendQueue = queue.CreateSendQueue();

            queue.StartReceiving();

            do
            {
                Console.WriteLine("Press any key to send a message");
                sendQueue.Send<TestMessage>(x =>
                {
                    x.ID = 123;
                    x.Modified = DateTime.Now;
                    x.Name = Guid.NewGuid().ToString();
                });
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            queue.StopReceiving();
        }

        static IRunJungleQueue CreateFullQueue()
        {
            return QueueBuilder.Create("JungleQueue_Testing", RegionEndpoint.USEast1)
                .WithSimpleObjectBuilder()
                .UsingJsonSerialization()
                .EnableMessageLogging()
                .SetSqsPollWaitTime(14)
                .UsingEventHandlersFromEntryAssembly()
                .WithMaxSimultaneousMessages(4)
                .CreateStartableQueue();
        }
    }
}
