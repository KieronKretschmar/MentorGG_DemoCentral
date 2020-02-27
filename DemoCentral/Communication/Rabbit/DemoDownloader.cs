using System;
using RabbitMQ.Client;
using RabbitCommunicationLib.RPC;
using RabbitCommunicationLib.TransferModels;
using RabbitCommunicationLib.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database.Enumerals;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using DataBase.Enumerals;
using RabbitMQ.Client.Events;

namespace DemoCentral.RabbitCommunication
{
    //Implement IHostedService so the Interface can be added via AddHostedService()
    public interface IDemoDownloader : IHostedService
    {
        /// <summary>
        /// Handle the response from DemoDownloader, set the corresponding FileStatus, update the QueueStatus and check the retries, eventually remove the demo
        /// </summary>
        Task HandleMessageAsync(BasicDeliverEventArgs ea, DownloadReport consumeModel);

        /// <summary>
        /// Send a downloadUrl to the DemoDownloader, set the FileStatus to Downloading, and update the DemoDownloaderQueue Status
        /// </summary>
        void SendMessageAndUpdateStatus(string correlationId, DemoDownloadInstructions produceModel);
    }

    public class DemoDownloader : RPCClient<DemoDownloadInstructions, DownloadReport>, IDemoDownloader
    {
        private readonly IDemoCentralDBInterface _demoCentralDBInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly IDemoFileWorker _demoFileWorker;
        private readonly ILogger<DemoDownloader> _logger;
        private const int MAX_RETRIES = 2;

        public DemoDownloader(IRPCQueueConnections queueConnection, IServiceProvider serviceProvider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoCentralDBInterface = serviceProvider.GetService<IDemoCentralDBInterface>();
            _inQueueDBInterface = serviceProvider.GetService<IInQueueDBInterface>();
            _demoFileWorker = serviceProvider.GetRequiredService<IDemoFileWorker>();
            _logger = serviceProvider.GetRequiredService<ILogger<DemoDownloader>>();
        }



        public void SendMessageAndUpdateStatus(string correlationId, DemoDownloadInstructions produceModel)
        {
            long matchId = long.Parse(correlationId);
            _demoCentralDBInterface.SetFileStatus(matchId, FileStatus.Downloading);
            _inQueueDBInterface.UpdateProcessStatus(matchId,ProcessedBy.DemoDownloader, true);

            PublishMessage(correlationId, produceModel);
        }

        public override Task HandleMessageAsync(BasicDeliverEventArgs ea, DownloadReport consumeModel)
        {
            var properties = ea.BasicProperties;
            long matchId = long.Parse(properties.CorrelationId);


            if (consumeModel.Success)
            {
                _demoCentralDBInterface.SetFilePath(matchId, consumeModel.DemoUrl);

                _demoCentralDBInterface.SetFileStatus(matchId, FileStatus.InBlobStorage);

                _inQueueDBInterface.UpdateProcessStatus(matchId,ProcessedBy.DemoDownloader, false);

                var model = _demoCentralDBInterface.CreateAnalyzeInstructions(matchId);

                _demoFileWorker.SendMessageAndUpdateQueueStatus(properties.CorrelationId, model);

                _logger.LogInformation($"Demo#{matchId} successfully downloaded");
            }
            else
            {
                int attempts = _inQueueDBInterface.IncrementRetry(matchId);

                if (attempts > MAX_RETRIES)
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(matchId);
                    _logger.LogError($"Demo#{matchId} failed download more than {MAX_RETRIES}, deleted");
                }
                else
                {
                    var downloadUrl = _demoCentralDBInterface.SetDownloadRetryingAndGetDownloadPath(matchId);

                    var resendModel = new DemoDownloadInstructions
                    {
                        DownloadUrl = downloadUrl,
                    };

                    SendMessageAndUpdateStatus(properties.CorrelationId, resendModel);

                    _logger.LogWarning($"Demo#{matchId} failed download, retrying");
                }
            }

            return Task.CompletedTask;
        }
    }
}

