using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.RPC;
using RabbitCommunicationLib.TransferModels;
using System;
using Database.Enumerals;
using DataBase.Enumerals;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

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
        Task HandleMessageAsync(BasicDeliverEventArgs ea, DemoAnalyzeReport consumeModel);

        /// <summary>
        /// Send a downloaded demo to the demoFileWorker and update the queue status
        /// </summary>
        void SendMessageAndUpdateQueueStatus(string correlationId, DemoAnalyzeInstructions model);
    }

    public class DemoFileWorker : RPCClient<DemoAnalyzeInstructions, DemoAnalyzeReport>, IDemoFileWorker
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

        public void SendMessageAndUpdateQueueStatus(string correlationId, DemoAnalyzeInstructions model)
        {
            long matchId = long.Parse(correlationId);
            _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.DemoFileWorker, true);
            PublishMessage(correlationId, model);
        }

        private void UpdateDBEntryFromFileWorkerResponse(long matchId, DemoAnalyzeReport response)
        {
            var inQueueDemo = _inQueueDBInterface.GetDemoById(matchId);
            var dbDemo = _demoDBInterface.GetDemoById(matchId);

            if (!response.Unzipped)
            {
                //Remove demo from queue and set file status to unzip failed
                _demoDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.UnzipFailed);
                _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                _logger.LogWarning($"Demo#{matchId} could not be unzipped");
                return;
            }

            if (!response.DuplicateChecked)
            {
                //Keep track of demos for which the duplicate check itself failed,
                //they may or may not be duplicates, the check itself failed for any reason
                _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                _demoDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.DuplicateCheckFailed);
                _logger.LogWarning($"Demo#{matchId} was not duplicate checked");
                return;
            }

            if (response.IsDuplicate)
            {
                //Remove demo if duplicate
                //TODO OPTIONAL FEATURE handle duplicate entry 2
                //Currently a hash-checked demo, which is duplicated just gets removed
                //Maybe keep track of it or just report back ?
                _demoDBInterface.RemoveDemo(dbDemo);
                _logger.LogWarning($"Demo#{matchId} is duplicate via MD5Hash");
                return;
            }

            if (response.Success)
            {
                //Successfully handled in demo fileworker
                _demoDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.Finished);
                _demoDBInterface.SetFileStatus(dbDemo, FileStatus.InBlobStorage);
                _demoDBInterface.SetFrames(dbDemo, response.FramesPerSecond);

                _inQueueDBInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.DemoFileWorker, false);
                _inQueueDBInterface.RemoveDemoIfNotInAnyQueue(inQueueDemo);
                _logger.LogInformation($"Demo#{matchId} was successfully handled by DemoFileWorker");
                return;
            }

            //If you get here, the above if cases do not catch every statement
            //Therefore the response has more possible statusses than handled here
            //Probably a coding error if you update DemoFileWorker
            _logger.LogError("Could not handle response from DemoFileWorker");
        }

        public override Task HandleMessageAsync(BasicDeliverEventArgs ea, DemoAnalyzeReport consumeModel)
        {
            long matchId = long.Parse(ea.BasicProperties.CorrelationId);
            UpdateDBEntryFromFileWorkerResponse(matchId, consumeModel);
            return Task.CompletedTask;

        }
    }
}
