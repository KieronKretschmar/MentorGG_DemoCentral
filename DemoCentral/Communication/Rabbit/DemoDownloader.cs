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
using RabbitCommunicationLib.Enums;

namespace DemoCentral.RabbitCommunication
{
    //Implement IHostedService so the Interface can be added via AddHostedService()
    public interface IDemoDownloader : IHostedService
    {
        /// <summary>
        /// Handle the response from DemoDownloader, set the corresponding FileStatus, update the QueueStatus and check the retries, eventually remove the demo
        /// </summary>
        Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoObtainReport consumeModel);

        void PublishMessage(DemoDownloadInstruction produceModel);
    }

    public class DemoDownloader : RPCClient<DemoDownloadInstruction, DemoObtainReport>, IDemoDownloader
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

        public override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoObtainReport consumeModel)
        {
            _logger.LogInformation($"Received {consumeModel.GetType()} for match#{consumeModel.MatchId}");

            try
            {
                UpdateDemoStatusFromObtainReport(consumeModel);
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not update demo#{consumeModel.MatchId} from DemoObtainReport due to {e}");
                return Task.FromResult(ConsumedMessageHandling.ThrowAway);
            }
            return Task.FromResult(ConsumedMessageHandling.Done);
        }

        private void UpdateDemoStatusFromObtainReport(DemoObtainReport consumeModel)
        {
            long matchId = consumeModel.MatchId;
            var inQueueDemo = _inQueueDBInterface.GetDemoById(matchId);
            var dbDemo = _demoCentralDBInterface.GetDemoById(matchId);

            if (consumeModel.Success)
            {
                _demoCentralDBInterface.SetBlobUrl(dbDemo, consumeModel.BlobUrl);

                _demoCentralDBInterface.SetFileStatus(dbDemo, FileStatus.InBlobStorage);

                _inQueueDBInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.DemoDownloader, false);

                var model = _demoCentralDBInterface.CreateAnalyzeInstructions(dbDemo);

                _inQueueDBInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.DemoFileWorker, true);
                _demoFileWorker.PublishMessage(model);
            }
            else
            {
                int attempts = _inQueueDBInterface.IncrementRetry(inQueueDemo);

                if (attempts > MAX_RETRIES)
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                    _logger.LogError($"Demo#{matchId} failed download more than {MAX_RETRIES}, deleted");
                }
                else
                {
                    _demoCentralDBInterface.SetFileStatus(dbDemo, FileStatus.DownloadRetrying);
                    var downloadUrl = dbDemo.DownloadUrl;

                    var resendModel = new DemoDownloadInstruction
                    {
                        DownloadUrl = downloadUrl,
                    };

                    _demoCentralDBInterface.SetFileStatus(matchId, FileStatus.DownloadRetrying);
                    _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoDownloader, true);
                    _logger.LogInformation($"Sent demo#{matchId} to DemoDownloadInstruction queue");

                    PublishMessage(resendModel);


                    _logger.LogWarning($"Demo#{matchId} failed download, retrying");
                }
            }

            _inQueueDBInterface.RemoveDemoIfNotInAnyQueue(inQueueDemo);
        }
    }
}

