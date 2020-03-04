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

namespace DemoCentral.RabbitCommunication
{
    public class ManualUploadReceiver : Consumer<DemoInsertInstruction>
    {
        private readonly IDemoFileWorker _demoFileWorker;
        private readonly IDemoCentralDBInterface _dBInterface;
        private readonly IUserInfoOperator _userInfoOperator;
        private readonly IInQueueDBInterface _inQueueDBInterface;

        public ManualUploadReceiver(IQueueConnection queueConnection, IDemoFileWorker demoFileWorker, IDemoCentralDBInterface dBInterface, IUserInfoOperator userInfoOperator, IInQueueDBInterface inQueueDBInterface) : base(queueConnection)
        {
            _demoFileWorker = demoFileWorker;
            _dBInterface = dBInterface;
            _userInfoOperator = userInfoOperator;
            _inQueueDBInterface = inQueueDBInterface;
        }

        public async override Task HandleMessageAsync(BasicDeliverEventArgs ea, DemoInsertInstruction model)
        {
            var requestedAnalyzerQuality = await _userInfoOperator.GetAnalyzerQualityAsync(model.UploaderId);
            if (_dBInterface.TryCreateNewDemoEntryFromGatherer(model, requestedAnalyzerQuality, out long matchId))
            {
                _inQueueDBInterface.Add(matchId, model.MatchDate, model.Source, model.UploaderId);
                var analyzeInstructions = _dBInterface.CreateAnalyzeInstructions(matchId);
                analyzeInstructions.BlobUrl = model.DownloadUrl;

                _demoFileWorker.SendMessageAndUpdateQueueStatus(analyzeInstructions);
            }
        }
    }
}
