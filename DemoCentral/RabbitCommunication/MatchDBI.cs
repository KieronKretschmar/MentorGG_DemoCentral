using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitTransfer;
using System.Text;

namespace DemoCentral.RabbitCommunication
{
    public class MatchDBI
    {
        public string QUEUE_NAME => "MatchDBI_DC";

        public MatchDBI()
        {
            using (var connection = RabbitInitializer.GetNewConnection())
            using (var channel = connection.CreateModel())
            {
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
        }

        public void HandleReceive(long matchId, string response)
        {
            AnalyzerTransferModel responseModel = JsonConvert.DeserializeObject<AnalyzerTransferModel>(response);
            DemoCentralDBInterface.UpdateUploadStatus(matchId, responseModel.Success);
        }
    }
}
