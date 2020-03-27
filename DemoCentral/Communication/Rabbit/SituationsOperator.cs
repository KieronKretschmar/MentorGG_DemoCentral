using Database.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RabbitCommunicationLib.Enums;
using System;

namespace DemoCentral.RabbitCommunication
{
    public class SituationsOperator : Consumer<TaskCompletedReport>
    {
        private readonly IInQueueDBInterface _inQueueDBInterface;
        private readonly ILogger<SituationsOperator> _logger;

        public SituationsOperator(IQueueConnection queueConnection, IInQueueDBInterface inQueueDBInterface, ILogger<SituationsOperator> logger) : base(queueConnection)
        {
            _inQueueDBInterface = inQueueDBInterface;
            _logger = logger;
        }

        /// <summary>
        /// Handle response from SituationsOperator, update queue status
        /// </summary>
        public override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport model)
        {
            try
            {
                UpdateDBFromSituationsOperator(model);
            }
            catch (Exception e)
            {

                _logger.LogError(e, $"Could not update demo #{model.MatchId} from situations operator response");
                return Task.FromResult(ConsumedMessageHandling.ThrowAway);
            }

            return Task.FromResult(ConsumedMessageHandling.Done);
        }

        private void UpdateDBFromSituationsOperator(TaskCompletedReport model)
        {
            long matchId = model.MatchId;
            _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.SituationsOperator, model.Success);
            _inQueueDBInterface.RemoveDemoIfNotInAnyQueue(matchId);

            string successString = model.Success ? "finished" : "failed";
            _logger.LogInformation($"Demo #{matchId} " + successString + "siutationsoperator");
        }
    }
}

