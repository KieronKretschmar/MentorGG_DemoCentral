using DemoCentral.Communication.HTTP;
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

namespace DemoCentral.RabbitCommunication
{
    public class ManualUploadReceiver : Consumer<DemoInsertInstruction>
    {
        private readonly IDemoFileWorker _demoFileWorker;
        private readonly IDemoCentralDBInterface _dBInterface;
        private readonly IUserInfoOperator _userInfoOperator;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly ILogger<ManualUploadReceiver> _logger;

        public ManualUploadReceiver(IQueueConnection queueConnection, IDemoFileWorker demoFileWorker, IDemoCentralDBInterface dBInterface, IUserInfoOperator userInfoOperator, IInQueueDBInterface inQueueDBInterface, ILogger<ManualUploadReceiver> logger) : base(queueConnection)
        {
            _demoFileWorker = demoFileWorker;
            _dBInterface = dBInterface;
            _userInfoOperator = userInfoOperator;
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }

        public async override Task HandleMessageAsync(BasicDeliverEventArgs ea, DemoInsertInstruction model)
        {
            var requestedAnalyzerQuality = await _userInfoOperator.GetAnalyzerQualityAsync(model.UploaderId);
            _logger.LogInformation($"Received manual upload from uploader#{model.UploaderId}, \n\t stored at {model.DownloadUrl}");
            if (_dBInterface.TryCreateNewDemoEntryFromGatherer(model, requestedAnalyzerQuality, out long matchId))
            {
                _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);
                var analyzeInstructions = _dBInterface.CreateAnalyzeInstructions(matchId);
                analyzeInstructions.BlobUrl = model.DownloadUrl;
                _logger.LogInformation($"Upload from uploader#{model.UploaderId} was unique, stored at match id #{matchId} now");

                _demoFileWorker.SendMessageAndUpdateQueueStatus(analyzeInstructions);
            }
            else
                _logger.LogInformation($"Received manual upload request from uploader#{model.UploaderId} was duplicate of match#{matchId}");

        }
    }
}
