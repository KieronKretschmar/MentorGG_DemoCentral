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
using RabbitCommunicationLib.Enums;

namespace DemoCentral.Communication.Rabbit
{
    //Implement IHostedService so the Interface can be added via AddHostedService()
    public interface IDemoFileWorker : IHostedService
    {
        /// <summary>
        /// Handle response fromm DemoFileWorker, update filepath,filestatus and queue status if success,
        /// remove entirely if duplicate, 
        /// remove from queue if unzip failed 
        /// </summary>
        Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoAnalyzeReport consumeModel);
        void PublishMessage(DemoAnalyzeInstruction model);
    }

    public class DemoFileWorker : RPCClient<DemoAnalyzeInstruction, DemoAnalyzeReport>, IDemoFileWorker
    {
        private readonly IDemoCentralDBInterface _demoDBInterface;
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly IProducer<RedisLocalizationInstruction> _fanoutSender;
        private readonly ILogger<DemoFileWorker> _logger;

        public DemoFileWorker(IRPCQueueConnections queueConnection, IServiceProvider provider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoDBInterface = provider.GetRequiredService<IDemoCentralDBInterface>();
            _inQueueDBInterface = provider.GetRequiredService<IInQueueDBInterface>();
            _fanoutSender = provider.GetRequiredService<IProducer<RedisLocalizationInstruction>>();
            _logger = provider.GetRequiredService<ILogger<DemoFileWorker>>();
        }

        private void UpdateDBEntryFromFileWorkerResponse(DemoAnalyzeReport response)
        {
            var matchId = response.MatchId;

            var inQueueDemo = _inQueueDBInterface.GetDemoById(matchId);
            var dbDemo = _demoDBInterface.GetDemoById(matchId);

            if (response.Success)
            {
                //Successfully handled in demo fileworker
                _demoDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.Finished);
                _demoDBInterface.SetFrames(dbDemo, response.FramesPerSecond);

                _inQueueDBInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.DemoFileWorker, false);

                var forwardModel = new RedisLocalizationInstruction
                {
                    MatchId = response.MatchId,
                    RedisKey = response.RedisKey,
                    ExpiryDate = response.ExpiryDate,
                };
                _fanoutSender.PublishMessage(forwardModel);

                //TODO IF SITUATIONOPERATOR IS OWN SERVICE
                //Set to in queue
                //_inQueueDBInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.SituationsOperator, true);

                _inQueueDBInterface.RemoveDemoIfNotInAnyQueue(inQueueDemo);
                _logger.LogInformation($"Demo [ {matchId} ] was sent to fanout");
                return;
            }
            else
            {
                // Handle failed demo according to the reason of its failure
                if (!response.Unzipped)
                {
                    //Remove demo from queue and set file status to unzip failed
                    _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.UnzipFailed);
                    _logger.LogWarning($"Demo [ {matchId} ] could not be unzipped");
                    return;
                }

                if (!response.DuplicateChecked)
                {
                    //Keep track of demos for which the duplicate check itself failed,
                    //they may or may not be duplicates, the check itself failed for any reason
                    _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.DuplicateCheckFailed);
                    _logger.LogWarning($"Demo [ {matchId} ] was not duplicate checked");
                    return;
                }

                if (response.IsDuplicate)
                {
                    //Remove demo if duplicate
                    //TODO OPTIONAL FEATURE handle duplicate entry 2
                    //Currently a hash-checked demo, which is duplicated just gets removed
                    //Maybe keep track of it or just report back ?
                    _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoDBInterface.RemoveDemo(dbDemo);

                    _logger.LogInformation($"Demo [ {matchId} ] is duplicate via MD5Hash");
                    return;
                }

                //If you get here, the above if cases do not catch every statement
                //Therefore the response has more possible statusses than handled here
                //Probably a coding error if you update DemoFileWorker
                _logger.LogError($"Could not handle response from DemoFileWorker. MatchId [ {matchId} ], Message [ {response.ToJson()} ]");
            }
        }

        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoAnalyzeReport consumeModel)
        {
            _logger.LogInformation($"Received demo [ {consumeModel.MatchId} ] from DemoAnalyzeReport queue");

            try
            {
                UpdateDBEntryFromFileWorkerResponse(consumeModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to update demo [ {consumeModel.MatchId} ] in database");
                return await Task.FromResult(ConsumedMessageHandling.ThrowAway);
            }
            return await Task.FromResult(ConsumedMessageHandling.Done);
        }
    }
}
