using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitTransfer.Interfaces;
using RabbitTransfer.Producer;
using System;

namespace DemoCentral.RabbitCommunication
{
    public class DemoFileWorker : RPCProducer<DC2DFWModel, DFW2DCModel>
    {
        private readonly DemoCentralDBInterface _demoDBInterface;
        private readonly InQueueDBInterface _inQueueDBInterface;

        public DemoFileWorker(IQueueReplyQueueConnection queueConnection, IServiceProvider provider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoDBInterface = provider.GetService<DemoCentralDBInterface>();
            _inQueueDBInterface = provider.GetService<InQueueDBInterface>();
        }

        public new void PublishMessage(string correlationId, DC2DFWModel model)
        {
            _inQueueDBInterface.UpdateQueueStatus(long.Parse(correlationId), "DFW", true);
            base.PublishMessage(correlationId, model);
        }

        private void updateDBEntryFromFileWorkerResponse(long matchId, DFW2DCModel response)
        {

            if (!response.Unzipped)
            {
                _demoDBInterface.SetFileStatusZipped(matchId, false);
            }
            else if (response.DuplicateChecked && response.IsDuplicate)
            {
                //TODO Put in extra table if same match uploaded by different persons
                _demoDBInterface.RemoveDemo(matchId);
            }
            else if (response.UploadedToDb)
            {

                _demoDBInterface.AddFilePath(matchId, response.zippedFilePath);

                _demoDBInterface.SetFileStatusZipped(matchId, true);

                _inQueueDBInterface.UpdateQueueStatus(matchId, "DFW", false);
            }
        }

        public override void HandleReply(IBasicProperties properties, DFW2DCModel consumeModel)
        {
            long matchId = long.Parse(properties.CorrelationId);
            updateDBEntryFromFileWorkerResponse(matchId, consumeModel);
        }
    }
}
