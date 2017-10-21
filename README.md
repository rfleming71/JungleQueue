# JungleBus
Transactional queue built on top of Amazon Web Services.
[![Build status](https://ci.appveyor.com/api/projects/status/3dxjsva48y40rp2y/branch/development?svg=true)](https://ci.appveyor.com/project/rfleming71/junglequeue/branch/development)

# Creating the Queue
Creates a queue that can send and recieve messages
```C#
var queue = QueueBuilder.Create("JungleQueue_Testing", RegionEndpoint.USEast1)
	.WithSimpleObjectBuilder()
	.UsingJsonSerialization()
	.EnableMessageLogging()
	.SetSqsPollWaitTime(14)
	.UsingEventHandlersFromEntryAssembly()
	.WithMaxSimultaneousMessages(1)
	.CreateStartableQueue();

queue.StartReceiving();
queue.CreateSendQueue().Publish(new TestMessage());
```

# Example message handler
```C#
public class Handler2 : IHandleMessage<TestMessage>
{
	private readonly IQueue _queue;
	private readonly ILog _log;
	public Handler2(IQueue queue, ILog log)
	{
		_queue = queue;
		_log = log;
	}

	public void Handle(TestMessage message)
	{
		_log.Info("Starting message Handler 2");
		_queue.Send(new TestMessage2());
		_log.Info("Finished message Handler 2");
	}
}
```

## Send Only Queue
Creates a queue that can only send messages
```C#
var queueBuilder = QueueBuilder.Create("JungleQueue_Testing", RegionEndpoint.USEast1)
	.WithSimpleObjectBuilder()
	.UsingJsonSerialization()
	.EnableMessageLogging()
	.CreateSendOnlyQueueFactory()
queueBuilder().Send(new TestMessage());
```
