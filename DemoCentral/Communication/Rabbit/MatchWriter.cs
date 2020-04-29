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

    /// <summary>
    /// Send demo removal instructions to matchwriter
    /// </summary>
    public interface IMatchWriter : IHostedService
    {
        Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport consumeModel);
        void PublishMessage(DemoRemovalInstruction instruction);
    }

    public class MatchWriter : RPCClient<DemoRemovalInstruction, TaskCompletedReport>, IMatchWriter
    {
        private readonly IDemoDBInterface _dbInterface;
        private readonly ILogger<MatchWriter> _logger;
        private readonly IBlobStorage _blobStorage;

        public MatchWriter(IRPCQueueConnections queueConnection, 
            IDemoDBInterface dbInterface,
            IBlobStorage blobStorage, 
            ILogger<MatchWriter> logger) : base(queueConnection)
        {
            _dbInterface = dbInterface;
            _blobStorage = blobStorage;
            _logger = logger;
        }

        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, TaskCompletedReport consumeModel)
        {
            var matchId = consumeModel.MatchId;
            _logger.LogInformation($"Received report for demo [ {matchId} ] storage removal - success : [ {consumeModel.Success} ] ");
            var demo = _dbInterface.GetDemoById(matchId);
            
            if (consumeModel.Success)
            {
                _dbInterface.SetFileStatus(demo, DataBase.Enumerals.FileStatus.Removed);
                await _blobStorage.DeleteBlobAsync(demo.BlobUrl);
                return ConsumedMessageHandling.Done;
            }
            else
            {
                _logger.LogWarning($"Match [ {matchId} ] failed to be removed. Check the correctness of the remaining data.");
                return await Task.FromResult(ConsumedMessageHandling.ThrowAway);
            }
        }
    }
}
