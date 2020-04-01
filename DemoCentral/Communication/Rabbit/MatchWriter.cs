using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.RPC;
using RabbitCommunicationLib.TransferModels;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.Communication.Rabbit
{
    public interface IMatchWriter : IHostedService
    {
        Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport consumeModel);
        void PublishMessage(DemoRemovalInstruction instruction);
    }

    public class MatchWriter : RPCClient<DemoRemovalInstruction, TaskCompletedReport>, IMatchWriter
    {
        private readonly IDemoCentralDBInterface _dbInterface;
        private readonly ILogger<MatchWriter> _logger;

        public MatchWriter(IRPCQueueConnections queueConnection, IDemoCentralDBInterface dbInterface, ILogger<MatchWriter> logger) : base(queueConnection)
        {
            _dbInterface = dbInterface;
            _logger = logger;
        }

        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport consumeModel)
        {
            _logger.LogInformation($"Received report for demo [ {consumeModel.MatchId} ] storage removal - success : [ {consumeModel.Success} ] ");
            if (consumeModel.Success)
            {
                _dbInterface.SetFileStatus(consumeModel.MatchId, DataBase.Enumerals.FileStatus.Removed);
                return await Task.FromResult(ConsumedMessageHandling.Done);
            }
            else
            {
                _logger.LogWarning($"Match [ {consumeModel.MatchId} ] failed to be removed. Check the correctness of the remaining data.");
                return await Task.FromResult(ConsumedMessageHandling.ThrowAway);
            }
        }
    }
}
