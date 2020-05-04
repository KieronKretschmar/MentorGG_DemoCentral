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
    public class DemoFileWorkerReportProcessor
    {
        private readonly ILogger<DemoFileWorkerReportProcessor> _logger;
        private readonly IDemoDBInterface _demoCentralDBInterface;
        private readonly IProducer<DemoAnalyzeInstruction> _demoFileWorkerProducer;
        private readonly IProducer<RedisLocalizationInstruction> _fanoutProducer;
        private IInQueueDBInterface _inQueueDBInterface;

        public DemoFileWorkerReportProcessor(
            ILogger<DemoFileWorkerReportProcessor> logger,
            IDemoDBInterface dbInterface,
            IProducer<DemoAnalyzeInstruction> demoFileWorkerProducer,
            IProducer<RedisLocalizationInstruction> fanoutProducer,
            IInQueueDBInterface inQueueDBInterface)
        {
            _logger = logger;
            _demoCentralDBInterface = dbInterface;
            _fanoutProducer = fanoutProducer;
            _demoFileWorkerProducer = demoFileWorkerProducer;
            _inQueueDBInterface = inQueueDBInterface;
        }


        /// <summary>
        /// Determine Analyze Quality, Update Queue Status and Send message to DemoDownloader for Demo Retrieval.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task WorkAsync(DemoAnalyzeReport model)
        {
            try
            {
                UpdateDBEntryFromFileWorkerResponse(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to update demo [ {model.MatchId} ] in database");
            }
        }



        private void UpdateDBEntryFromFileWorkerResponse(DemoAnalyzeReport response)
        {
            var matchId = response.MatchId;

            var inQueueDemo = _inQueueDBInterface.GetDemoById(matchId);
            var dbDemo = _demoCentralDBInterface.GetDemoById(matchId);

            if (response.Success)
            {
                //Successfully handled in demo fileworker
                _demoCentralDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.Finished);
                _demoCentralDBInterface.SetFrames(dbDemo, response.FramesPerSecond);

                _inQueueDBInterface.UpdateProcessStatus(inQueueDemo, ProcessedBy.DemoFileWorker, false);

                var forwardModel = new RedisLocalizationInstruction
                {
                    MatchId = response.MatchId,
                    RedisKey = response.RedisKey,
                    ExpiryDate = response.ExpiryDate,
                };
                _fanoutProducer.PublishMessage(forwardModel);

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
                    _demoCentralDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.UnzipFailed);
                    _logger.LogWarning($"Demo [ {matchId} ] could not be unzipped");
                    return;
                }

                if (!response.DuplicateChecked)
                {
                    //Keep track of demos for which the duplicate check itself failed,
                    //they may or may not be duplicates, the check itself failed for any reason
                    _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoCentralDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.DuplicateCheckFailed);
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
                    _demoCentralDBInterface.RemoveDemo(dbDemo);

                    _logger.LogInformation($"Demo [ {matchId} ] is duplicate via MD5Hash");
                    return;
                }

                if (!response.DemoAnalyzerSucceeded)
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                    _demoCentralDBInterface.SetFileWorkerStatus(dbDemo, DemoFileWorkerStatus.AnalyzerFailed);

                    _logger.LogWarning($"Demo [ {matchId} ] failed at DemoAnalyzer.");
                    return;
                }

                //If you get here, the above if cases do not catch every statement
                //Therefore the response has more possible statusses than handled here
                //Probably a coding error if you update DemoFileWorker
                _inQueueDBInterface.RemoveDemoFromQueue(inQueueDemo);
                _demoCentralDBInterface.RemoveDemo(dbDemo);
                _logger.LogError($"Could not handle response from DemoFileWorker. Removing match from database. MatchId [ {matchId} ], Message [ {response.ToJson()} ]");
            }
        }
    }
}