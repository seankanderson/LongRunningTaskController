.NET Standard / Core 2.0 project to manage groups of long running tasks. Includes a concrete implementation for RabbitMq consumers and a web API to control the pool.  

This can be used or adapted to conduct load testing on a RabbitMQ broker or cluster.  


```

ILongRunningTaskController Controller = new LongRunningTaskController();

var connection = new RabbitMqConnection();
connection.Host = "192.168.0.101";
            
var queue = new RabbitMqQueue();
queue.Name = "TestQueue01";
queue.AutoAck = false;

var queue2 = new RabbitMqQueue();
queue2.Name = "TestQueue02";
queue2.AutoAck = false;

var defaultExchange = new RabbitMqExchange();

var fanoutExchange = new RabbitMqExchange();
fanoutExchange.Type = "fanout";
fanoutExchange.Name = "amq.fanout";

queue.BindingExchange = fanoutExchange;
queue2.BindingExchange = fanoutExchange;
	
//Publish 
var publisher = new BasicPublisherTask();
publisher.Connection = connection;
publisher.Queues.Add(queue);
publisher.Queues.Add(queue2);
publisher.ConfirmPublishedMessages = true;
Controller.AddLongRunningTask(publisher);

// OR ...

//Consume
var consumer = new BasicConsumerTask();
consumer.Connection = connection;
consumer.Queue = queue;

//Add as many consumer threads as needed based on workload
Controller.AddLongRunningTask(consumer);

// AND ...

Controller.StartAll();
Controller.StopAll();					

```

The ConsumerDaemon project is a self-hosted web app demonstrating how you can remotely start, stop, and monitor consumers.

`http://localhost:56013/api/consumer/add/10/TestQueue01`

`http://localhost:56013/api/consumer/start`

`http://localhost:56013/api/consumer/running`

`http://localhost:56013/api/consumer/logs`

`http://localhost:56013/api/consumer/exceptions`

