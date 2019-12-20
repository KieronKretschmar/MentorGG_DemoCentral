using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitTransfer.RPC;
using RabbitTransfer.TransferModels;
using RabbitTransfer.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DemoCentral.RabbitCommunication
{
    public class DemoDownloader : RPCClient<DC_DD_Model, DD_DC_Model>
    {
        private readonly DemoCentralDBInterface _demoCentralDBInterface;
        private readonly InQueueDBInterface _inQueueDBInterface;
        private readonly DemoFileWorker _demoFileWorker;

        public DemoDownloader(IRPCQueueConnections queueConnection, IServiceProvider serviceProvider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoCentralDBInterface = serviceProvider.GetService<DemoCentralDBInterface>();
            _inQueueDBInterface = serviceProvider.GetService<InQueueDBInterface>();
            _demoFileWorker = serviceProvider.GetRequiredService<DemoFileWorker>();
        }

        public new void PublishMessage(string correlationId, DC_DD_Model produceModel)
        {
            long matchId = long.Parse(correlationId);
            _demoCentralDBInterface.SetFileStatusDownloading(matchId);
            _inQueueDBInterface.UpdateQueueStatus(matchId, "DD", true);
        
            base.PublishMessage(correlationId, produceModel);
        }

        public override void HandleMessage(IBasicProperties properties, DD_DC_Model consumeModel)
        {
            long matchId = long.Parse(properties.CorrelationId);

            _demoCentralDBInterface.SetFileStatusDownloaded(matchId, consumeModel.Success);

            if (consumeModel.Success)
            {
                _demoCentralDBInterface.AddFilePath(matchId, consumeModel.DemoUrl);

                _inQueueDBInterface.UpdateQueueStatus(matchId, "DD", false);

                var model = _demoCentralDBInterface.CreateDemoFileWorkerModel(matchId);

                _demoFileWorker.PublishMessage(properties.CorrelationId, model);
            }
            else
            {
                var downloadUrl = _demoCentralDBInterface.SetDownloadRetryingAndGetDownloadPath(matchId);
                int attempts = _inQueueDBInterface.IncrementRetry(matchId);

                if (attempts >= 3)
                {
                    _inQueueDBInterface.RemoveDemoFromQueue(matchId);
                }
                else
                {
                    var resendModel = new DC_DD_Model
                    {
                        DownloadUrl = downloadUrl,
                    };
                    PublishMessage(properties.CorrelationId, resendModel);
                }
            }
        }
    }
}

