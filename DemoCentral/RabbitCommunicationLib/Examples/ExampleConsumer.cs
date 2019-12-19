using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

namespace RabbitTransfer
{
    /// <summary>
    /// This is an example of a consumer,
    /// Use this if you want to receive messages from a queue
    /// </summary>
    public class ExampleConsumer
    {
        public string QUEUE_NAME => "EXAMPLE_QUEUE";


        public ExampleConsumer()
        {
            var connection = RabbitInitializer.GetNewConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                long matchId = long.Parse(ea.BasicProperties.CorrelationId);
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                HandleReceive(matchId, message);
            };
            channel.BasicConsume(queue: QUEUE_NAME, autoAck: true, consumer: consumer);
        }



        public void HandleReceive(long matchId, string response)
        {
            //if response is JSON, deserialize into object like this
            var responseModel = JsonConvert.DeserializeObject<TransferModel>(response);

            Console.WriteLine("[.] received {0}", response);
        }
    }
}
