using System;
using RabbitMQ.Client;
using RabbitTransfer.RPC;
using RabbitTransfer.TransferModels;
using RabbitTransfer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database.Enumerals;
using Microsoft.Extensions.Logging;

namespace DemoCentral.RabbitCommunication
{
    //Implement IHostedService so the Interface can be added via AddHostedService()
    public interface IDemoDownloader : IHostedService
    {
        /// <summary>
        /// Handle the response from DemoDownloader, set the corresponding FileStatus, update the QueueStatus and check the retries, eventually remove the demo
        /// </summary>
        void HandleMessage(IBasicProperties properties, DD_DC_Model consumeModel);

        /// <summary>
        /// Send a downloadUrl to the DemoDownloader, set the FileStatus to Downloading, and update the DemoDownloaderQueue Status
        /// </summary>
        void SendMessageAndUpdateStatus(string correlationId, DC_DD_Model produceModel);
    }

    public class DemoDownloader : RPCClient<DC_DD_Model, DD_DC_Model>, IDemoDownloader
    {
        private readonly IDemoCentralDBInterface _demoCentralDBInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly IDemoFileWorker _demoFileWorker;
        private readonly ILogger<DemoDownloader> _logger;
        private const int RETRY_LIMIT = 3;

        public DemoDownloader(IRPCQueueConnections queueConnection, IServiceProvider serviceProvider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoCentralDBInterface = serviceProvider.GetService<IDemoCentralDBInterface>();
            _inQueueDBInterface = serviceProvider.GetService<IInQueueDBInterface>();
            _demoFileWorker = serviceProvider.GetRequiredService<IDemoFileWorker>();
            _logger = serviceProvider.GetRequiredService<ILogger<DemoDownloader>>();
        }



        public void SendMessageAndUpdateStatus(string correlationId, DC_DD_Model produceModel)
        {
            long matchId = long.Parse(correlationId);
            _demoCentralDBInterface.SetFileStatus(matchId,DataBase.Enumerals.FileStatus.DOWNLOADING);
            _inQueueDBInterface.UpdateProcessStatus(matchId,ProcessedBy.DemoDownloader, true);

            PublishMessage(correlationId, produceModel);
        }

        public override void HandleMessage(IBasicProperties properties, DD_DC_Model consumeModel)
        {
            long matchId = long.Parse(properties.CorrelationId);


            if (consumeModel.Success)
            {
                _demoCentralDBInterface.SetFilePath(matchId, consumeModel.DemoUrl);

                _demoCentralDBInterface.SetFileStatus(matchId, DataBase.Enumerals.FileStatus.DOWNLOADED);

                _inQueueDBInterface.UpdateProcessStatus(matchId,ProcessedBy.DemoDownloader, false);

                var model = _demoCentralDBInterface.CreateDemoFileWorkerModel(matchId);

                _demoFileWorker.SendMessageAndUpdateQueueStatus(properties.CorrelationId, model);

                _logger.LogInformation($"Demo#{matchId} successfully downloaded");
            }
            else
            {
                int attempts = _inQueueDBInterface.IncrementRetry(matchId);

                if (attempts >= RETRY_LIMIT)
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(matchId);
                    _logger.LogError($"Demo#{matchId} failed download more than {RETRY_LIMIT}, deleted");
                }
                else
                {
                    var downloadUrl = _demoCentralDBInterface.SetDownloadRetryingAndGetDownloadPath(matchId);

                    var resendModel = new DC_DD_Model
                    {
                        DownloadUrl = downloadUrl,
                    };
                    SendMessageAndUpdateStatus(properties.CorrelationId, resendModel);

                    _logger.LogWarning($"Demo#{matchId} failed download, retrying");
                }
            }
        }
    }
}

