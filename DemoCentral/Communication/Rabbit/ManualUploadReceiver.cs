using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using Database.Enumerals;
using DemoCentral.Communication.HTTP;

namespace DemoCentral.Communication.Rabbit
{
    public class ManualUploadReceiver : Consumer<ManualDownloadReport>
    {
        private readonly IDemoFileWorker _demoFileWorker;
        private readonly IDemoCentralDBInterface _dBInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly IUserInfoGetter _userInfoGetter;
        private readonly ILogger<ManualUploadReceiver> _logger;

        public ManualUploadReceiver(IQueueConnection queueConnection, IDemoFileWorker demoFileWorker, IDemoCentralDBInterface dBInterface, IInQueueDBInterface inQueueDBInterface,IUserInfoGetter userInfoGetter , ILogger<ManualUploadReceiver> logger) : base(queueConnection)
        {
            _demoFileWorker = demoFileWorker;
            _dBInterface = dBInterface;
            _inQueueDBInterface = inQueueDBInterface;
            _userInfoGetter = userInfoGetter;
            _logger = logger;
        }

        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, ManualDownloadReport model)
        {
            try
            {
                _logger.LogInformation($"Received manual upload from uploader [ {model.UploaderId} ], \n\t stored at [ {model.BlobUrl} ]");

                if (model.BlobUrl is null)
                    throw new ArgumentNullException("BlobUrl can not be null");

                await InsertNewDemo(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not insert download from {model.BlobUrl}");
                return ConsumedMessageHandling.ThrowAway;
            }
            return ConsumedMessageHandling.Done;
        }

        private async Task InsertNewDemo(ManualDownloadReport model)
        {
            var requestedAnalyzerQuality = await _userInfoGetter.GetAnalyzerQualityAsync(model.UploaderId);
            var matchId = _dBInterface.CreateNewDemoEntryFromManualUpload(model, requestedAnalyzerQuality);

            _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);
            var analyzeInstructions = _dBInterface.CreateAnalyzeInstructions(matchId);

            _demoFileWorker.PublishMessage(analyzeInstructions);
            _logger.LogInformation($"Sent demo [ {matchId} ] to DemoAnalyzeInstruction queue");
            _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoFileWorker, true);
        }
    }
}
