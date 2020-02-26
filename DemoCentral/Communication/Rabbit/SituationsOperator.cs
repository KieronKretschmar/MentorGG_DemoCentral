﻿using Database.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

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
        public override Task HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport model)
        {
            long matchId = long.Parse(ea.BasicProperties.CorrelationId);
            _inQueueDBInterface.UpdateProcessStatus(matchId, ProcessedBy.SituationsOperator, model.Success);

            string successString = model.Success ? "finished" : "failed";
            _logger.LogInformation($"Demo#{matchId} " + successString + "siutationsoperator");

            return Task.CompletedTask;
        }

    }
}

