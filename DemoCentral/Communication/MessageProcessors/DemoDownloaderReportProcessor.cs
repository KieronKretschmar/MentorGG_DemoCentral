using System;
using System.Threading.Tasks;
using Database.Enumerals;
using DataBase.Enumerals;
using DemoCentral.Communication.HTTP;
using DemoCentral.Communication.Rabbit;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace DemoCentral.Communication.MessageProcessors
{
    public class DemoDownloaderReportProcessor
    {
        private readonly IDemoDBInterface _demoCentralDBInterface;
        private readonly IProducer<DemoDownloadInstruction> _demoDownloaderProducer;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly ILogger<DemoDownloaderReportProcessor> _logger;
        private IInQueueDBInterface _inQueueDBInterface;

        private const int MAX_RETRIES = 2;

        public DemoDownloaderReportProcessor(
            IDemoDBInterface dbInterface,
            IProducer<DemoDownloadInstruction> demoDownloaderProducer,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            ILogger<DemoDownloaderReportProcessor> logger,
            IInQueueDBInterface inQueueDBInterface)
        {

            _demoCentralDBInterface = dbInterface;
            _demoDownloaderProducer = demoDownloaderProducer;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }


        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoObtainReport model)
        {
            try
            {
                UpdateDemoStatusFromObtainReport(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not update demo [ {model.MatchId} ] from DemoObtainReport.");
            }
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
                _demoFileWorkerProducer.PublishMessage(model);
            }
            else
            {
                int attempts = _inQueueDBInterface.IncrementRetry(inQueueDemo);

                if (attempts > MAX_RETRIES)
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoCentralDBInterface.SetFileStatus(dbDemo, FileStatus.DownloadFailed);
                    _logger.LogError($"Demo [ {matchId} ] failed download more than {MAX_RETRIES} times, no further analyzing");
                }
                else
                {
                    _demoCentralDBInterface.SetFileStatus(dbDemo, FileStatus.DownloadRetrying);

                    var resendModel = _demoCentralDBInterface.CreateDownloadInstructions(dbDemo);

                    _demoCentralDBInterface.SetFileStatus(matchId, FileStatus.DownloadRetrying);
                    _logger.LogInformation($"Sent demo [ {matchId} ] to DemoDownloadInstruction queue");

                    _demoDownloaderProducer.PublishMessage(resendModel);

                    _logger.LogWarning($"Demo [ {matchId} ] failed download, retrying");
                }
            }

            _inQueueDBInterface.RemoveDemoIfNotInAnyQueue(inQueueDemo);
        }
    }
}