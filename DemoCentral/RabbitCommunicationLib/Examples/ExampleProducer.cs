using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitTransfer
{
    /// <summary>
    /// This is an example producer
    /// use this if you want to send messages
    /// </summary>
    public class ExampleProducer
    {
        public string QUEUE_NAME => "EXAMPLE_QUEUE";

        public ExampleProducer()
        {
            using (var connection = RabbitInitializer.GetNewConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false);
                IBasicProperties props = channel.CreateBasicProperties();

                long matchId = 1234;
                props.CorrelationId = matchId.ToString();

                TransferModel responseModel = new DC_DD_Model
                {
                    DownloadUrl = "https://api.faceit.com/thebestclutch.dem",
                };

                byte[] responseBytes = responseModel.ToBytes();

                channel.BasicPublish(exchange: "", routingKey: QUEUE_NAME, basicProperties: props, body: responseBytes);

                Console.WriteLine("[.] sent message");
            }
        }
    }
}
