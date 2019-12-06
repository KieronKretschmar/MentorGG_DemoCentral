using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitTransfer;
using System.Text;

namespace DemoCentral.RabbitCommunication
{
    public class GathererConsumer
    {
        public string QUEUE_NAME => "Gatherer_DC";


        public GathererConsumer()
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
            var responseModel = JsonConvert.DeserializeObject<GathererTransferModel>(response);
            DC_DD_Model forwardModel = new DC_DD_Model { matchId = matchId, DownloadPath = responseModel.DownloadUrl };

            //TODO handle duplicate entry, currently not inserted into db and forgotten afterwards
            if (DemoCentralDBInterface.CreateNewDemoEntryFromGatherer(responseModel))
            {
                new DD().SendNewDemo(matchId, forwardModel.ToJSON());
            }
        }
    }
}
