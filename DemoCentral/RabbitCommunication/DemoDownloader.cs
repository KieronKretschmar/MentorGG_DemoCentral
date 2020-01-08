using System;
using RabbitMQ.Client;
using RabbitTransfer.RPC;
using RabbitTransfer.TransferModels;
using RabbitTransfer.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database.Enumerals;

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

        public DemoDownloader(IRPCQueueConnections queueConnection, IServiceProvider serviceProvider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoCentralDBInterface = serviceProvider.GetService<IDemoCentralDBInterface>();
            _inQueueDBInterface = serviceProvider.GetService<IInQueueDBInterface>();
            _demoFileWorker = serviceProvider.GetRequiredService<IDemoFileWorker>();
        }



        public void SendMessageAndUpdateStatus(string correlationId, DC_DD_Model produceModel)
        {
            long matchId = long.Parse(correlationId);
            _demoCentralDBInterface.SetFileStatusDownloading(matchId);
            _inQueueDBInterface.UpdateQueueStatus(matchId,QueueName.DemoDownloader, true);

            PublishMessage(correlationId, produceModel);
        }

        public override void HandleMessage(IBasicProperties properties, DD_DC_Model consumeModel)
        {
            long matchId = long.Parse(properties.CorrelationId);

            _demoCentralDBInterface.SetFileStatusDownloaded(matchId, consumeModel.Success);

            if (consumeModel.Success)
            {
                _demoCentralDBInterface.AddFilePath(matchId, consumeModel.DemoUrl);

                _inQueueDBInterface.UpdateQueueStatus(matchId,QueueName.DemoDownloader, false);

                var model = _demoCentralDBInterface.CreateDemoFileWorkerModel(matchId);

                _demoFileWorker.PublishMessage(properties.CorrelationId, model);
            }
            else
            {
                int attempts = _inQueueDBInterface.IncrementRetry(matchId);

                if (attempts >= 3)
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(matchId);
                }
                else
                {
                    var downloadUrl = _demoCentralDBInterface.SetDownloadRetryingAndGetDownloadPath(matchId);

                    var resendModel = new DC_DD_Model
                    {
                        DownloadUrl = downloadUrl,
                    };
                    SendMessageAndUpdateStatus(properties.CorrelationId, resendModel);
                }
            }
        }
    }
}

