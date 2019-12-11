using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitTransfer;

namespace DemoCentral.RabbitCommunication
{
    /// <summary>
    /// SituationsOperator Consumer
    /// This receives all the messages from the SO_DC Queue and updates their queue status
    /// </summary>
    public class SO
    {
        public string QUEUE_NAME => "SO_DC";

        public SO()
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
            var responseModel = JsonConvert.DeserializeObject<AnalyzerTransferModel>(response);

            InQueueDBInterface.UpdateQueueStatus(matchId, "SO", responseModel.Success);
        }
    }
}

