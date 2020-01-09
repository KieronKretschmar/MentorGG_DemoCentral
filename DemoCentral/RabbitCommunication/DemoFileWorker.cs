using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitTransfer.Interfaces;
using RabbitTransfer.RPC;
using RabbitTransfer.TransferModels;
using System;
using Database.Enumerals;
using DataBase.Enumerals;
using Microsoft.Extensions.Logging;

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
        void SendMessageAndUpdateQueueStatus(string correlationId, DC2DFWModel model);
    }

    public class DemoFileWorker : RPCClient<DC2DFWModel, DFW2DCModel>, IDemoFileWorker
    {
        private readonly IDemoCentralDBInterface _demoDBInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly ILogger<DemoFileWorker> _logger;

        public DemoFileWorker(IRPCQueueConnections queueConnection, IServiceProvider provider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoDBInterface = provider.GetRequiredService<IDemoCentralDBInterface>();
            _inQueueDBInterface = provider.GetRequiredService<IInQueueDBInterface>();
            _logger = provider.GetRequiredService<ILogger<DemoFileWorker>>();
        }

        public void SendMessageAndUpdateQueueStatus(string correlationId, DC2DFWModel model)
        {
            long matchId = long.Parse(correlationId);
            _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoFileWorker, true);
            PublishMessage(correlationId, model);
        }

        private void UpdateDBEntryFromFileWorkerResponse(long matchId, DFW2DCModel response)
        {
            if (!response.Unzipped)
            {
                //Remove demo from queue and set file status to unzip failed
                _demoDBInterface.SetFileStatus(matchId, FileStatus.UNZIPFAILED);
                _inQueueDBInterface.RemoveDemoFromQueue(matchId);
                _logger.LogWarning($"Demo#{matchId} could not be unzipped");
                return;
            }

            if (response.DuplicateChecked && response.IsDuplicate)
            {
                //Remove demo if duplicate
                //TODO OPTIONAL FEATURE handle duplicate entry 2
                //Currently a hash-checked demo, which is duplicated just gets removed
                //Maybe keep track of it or just report back ?
                _demoDBInterface.RemoveDemo(matchId);
                _logger.LogWarning($"Demo#{matchId} is duplicate via MD5Hash");
                return;
            }

            if (response.Success)
            {
                //Successful handled in demo fileworker
                //store filepath, set status to unzipped, remove from queue
                _demoDBInterface.SetFilePath(matchId, response.zippedFilePath);

                _demoDBInterface.SetFileStatus(matchId, FileStatus.UNZIPPED);

                _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoFileWorker, false);
                _logger.LogInformation($"Demo#{matchId} was successfully handled by DemoFileWorker");
                return;
            }
        }

        public override void HandleMessage(IBasicProperties properties, DFW2DCModel consumeModel)
        {
            long matchId = long.Parse(properties.CorrelationId);
            UpdateDBEntryFromFileWorkerResponse(matchId, consumeModel);
        }
    }
}
