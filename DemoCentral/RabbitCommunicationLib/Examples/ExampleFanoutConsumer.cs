using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitTransfer;

namespace RabbitTransfer.Examples
{
    public class ExampleFanoutConsumer
    {
        private const string QUEUE_NAME = "FANOUT_QUEUE_1";

        public ExampleFanoutConsumer()
        {
            using (var connection = RabbitInitializer.GetNewConnection())
            using (var channel = connection.CreateModel())
            {
                //SPECIFY YOUR EXCHANGE TYPE
                channel.ExchangeDeclare(exchange: "EXCHANGE NAME", ExchangeType.Fanout);

                channel.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false);
                var consumer = new EventingBasicConsumer(channel);

                channel.QueueBind(queue:QUEUE_NAME, exchange:"EXCHANGE NAME", routingKey: "GETS IGNORED");


                consumer.Received += (model, ea) =>
                {
                    long matchId = long.Parse(ea.BasicProperties.CorrelationId);
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    HandleReceive(matchId, message);
                };


                channel.BasicConsume(QUEUE_NAME, false, consumer);
            }


        }

        private void HandleReceive(long matchId, string message)
        {
            throw new NotImplementedException();
        }
    }
}
