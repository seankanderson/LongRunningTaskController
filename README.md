.NET Standard / Core 2.0 project to manage groups of long running tasks. Includes a concrete implementation for RabbitMq consumers and a web API to control the pool.  

This can be used to conduct load testing on a RabbitMQ broker cluster.  


```
Run the RabbitMqTaskDemo console app to create a parameters.json file.  Alter the properties of the various objects to create a connection and publish/subscribe to messages on a RabbitMQ broker.  An alternate payload file can be used; with the default message payload being the object-serialized version of the data in parameters.json.

Custom parameters and payload files can be specified at the command line, making it easy to script out sophisticated message toplogies for testing.

dotnet RabbitMqTaskDemo.dll parameters.json messageBody.json

Features decent logging.

```

The ConsumerDaemon project is a self-hosted web app demonstrating how you can remotely start, stop, and monitor consumers via a REST API. Uses the RabbitMQTaskDemo project.

`http://localhost:56013/api/consumer/add/10/TestQueue01`

`http://localhost:56013/api/consumer/start`

`http://localhost:56013/api/consumer/running`

`http://localhost:56013/api/consumer/logs`

`http://localhost:56013/api/consumer/exceptions`

