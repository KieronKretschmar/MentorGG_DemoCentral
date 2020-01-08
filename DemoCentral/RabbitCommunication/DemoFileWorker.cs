using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitTransfer.Interfaces;
using RabbitTransfer.RPC;
using RabbitTransfer.TransferModels;
using System;
using Database.Enumerals;

namespace DemoCentral.RabbitCommunication
{
    //Implement IHostedService so the Interface can be added via AddHostedService()
    public interface IDemoFileWorker : IHostedService
    {
        /// <summary>
        /// Handle response fromm DemoFileWorker, update filepath,filestatus and queue status if success,
        /// remove entirely if duplicate, 
        /// remove from queue if unzip failed 
        /// </summary>
        void HandleMessage(IBasicProperties properties, DFW2DCModel consumeModel);

        /// <summary>
        /// Send a downloaded demo to the demoFileWorker and update the queue status
        /// </summary>
        void PublishMessage(string correlationId, DC2DFWModel model);
    }

    public class DemoFileWorker : RPCClient<DC2DFWModel, DFW2DCModel>, IDemoFileWorker
    {
        private readonly IDemoCentralDBInterface _demoDBInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;

        public DemoFileWorker(IRPCQueueConnections queueConnection, IServiceProvider provider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoDBInterface = provider.GetRequiredService<IDemoCentralDBInterface>();
            _inQueueDBInterface = provider.GetRequiredService<IInQueueDBInterface>();
        }

        public new void PublishMessage(string correlationId, DC2DFWModel model)
        {
            long matchId = long.Parse(correlationId);
            _inQueueDBInterface.UpdateQueueStatus(matchId, QueueName.DemoFileWorker, true);
            base.PublishMessage(correlationId, model);
        }

        private void updateDBEntryFromFileWorkerResponse(long matchId, DFW2DCModel response)
        {
            if (!response.Unzipped)
            {
                _demoDBInterface.SetFileStatusZipped(matchId, false);
                _inQueueDBInterface.RemoveDemoFromQueue(matchId);
            }
            else if (response.DuplicateChecked && response.IsDuplicate)
            {
                //TODO Put in extra table if same match uploaded by different persons
                _demoDBInterface.RemoveDemo(matchId);
            }
            else if (response.Success)
            {
                _demoDBInterface.SetFilePath(matchId, response.zippedFilePath);

                _demoDBInterface.SetFileStatusZipped(matchId, true);

                _inQueueDBInterface.UpdateQueueStatus(matchId, QueueName.DemoFileWorker, false);
            }
        }

        public override void HandleMessage(IBasicProperties properties, DFW2DCModel consumeModel)
        {
            long matchId = long.Parse(properties.CorrelationId);
            updateDBEntryFromFileWorkerResponse(matchId, consumeModel);
        }
    }
}
