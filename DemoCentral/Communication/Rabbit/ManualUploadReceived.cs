using DemoCentral.Communication.HTTP;
using RabbitMQ.Client;
using RabbitTransfer.Consumer;
using RabbitTransfer.Interfaces;
using RabbitTransfer.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.RabbitCommunication
{
    public class ManualUploadReceived : Consumer<GathererTransferModel>
    {
        private readonly IDemoFileWorker _demoFileWorker;
        private readonly IDemoCentralDBInterface _dBInterface;
        private readonly IUserInfoOperator _userInfoOperator;

        public ManualUploadReceived(IQueueConnection queueConnection, IDemoFileWorker demoFileWorker, IDemoCentralDBInterface dBInterface, IUserInfoOperator userInfoOperator) : base(queueConnection)
        {
            _demoFileWorker = demoFileWorker;
            _dBInterface = dBInterface;
            _userInfoOperator = userInfoOperator;
        }

        public async override void HandleMessage(IBasicProperties properties, GathererTransferModel model)
        {
            var requestedAnalyzerQuality = await _userInfoOperator.GetAnalyzerQualityAsync(model.UploaderId);
            if (_dBInterface.TryCreateNewDemoEntryFromGatherer(model, requestedAnalyzerQuality, out long matchId))
            {
                var dfwModel = _dBInterface.CreateDemoFileWorkerModel(matchId);
                dfwModel.BlobURI = dfwModel.DownloadUrl;

                _demoFileWorker.SendMessageAndUpdateQueueStatus(matchId.ToString(), dfwModel);
            }
        }
    }
}
