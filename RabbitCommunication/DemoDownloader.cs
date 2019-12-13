﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitTransfer.Producer;
using RabbitTransfer.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DemoCentral.RabbitCommunication
{
    public class DemoDownloader : RPCProducer<DC_DD_Model, DD_DC_Model>
    {
        private readonly DemoCentralDBInterface _demoCentralDBInterface;
        private readonly InQueueDBInterface _inQueueDBInterface;

        public DemoDownloader(IQueueReplyQueueConnection queueConnection, ServiceProvider serviceProvider, bool persistantMessageSending = true) : base(queueConnection, persistantMessageSending)
        {
            _demoCentralDBInterface = serviceProvider.GetService<DemoCentralDBInterface>();
            _inQueueDBInterface = serviceProvider.GetService<InQueueDBInterface>();
        }

        public override void HandleReply(IBasicProperties properties, DD_DC_Model consumeModel)
        {
            long matchId = long.Parse(properties.CorrelationId);

            _demoCentralDBInterface.UpdateDownloadStatus(matchId, consumeModel.Success);
            if (consumeModel.Success)
            {
                _demoCentralDBInterface.AddFilePath(matchId, consumeModel.DemoUrl);

                _inQueueDBInterface.UpdateQueueStatus(matchId, "DD", false);

                //TODO send to DFW
            }
            else
            {
                var downloadUrl = _demoCentralDBInterface.SetDownloadRetryingAndGetDownloadPath(matchId);
                int attempts = _inQueueDBInterface.IncrementRetry(matchId);

                if (attempts >= 3)
                {
                    _demoCentralDBInterface.RemoveDemo(matchId);
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

