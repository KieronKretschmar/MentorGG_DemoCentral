using Database.Enumerals;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitCommunicationLib.Consumer;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using System.Threading.Tasks;
using RabbitCommunicationLib.Enums;
using RabbitMQ.Client.Events;
using DataBase.Enumerals;
using DemoCentral.Communication.HTTP;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace DemoCentral.Communication.Rabbit
{
    /// <summary>
    /// Consumer for the Gatherer queue
    /// If a message is received , <see cref="HandleMessage(IBasicProperties, GathererTransferModel)"/> is called
    /// and the message is forwarded to the demodownloader
    /// </summary>
    public class Gatherer : Consumer<DemoInsertInstruction>
    {
        private readonly IServiceProvider _serviceProvider;

        public Gatherer(
            IQueueConnection queueConnection,
            IServiceProvider serviceProvider) : base(queueConnection)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Handle downloadUrl from GathererQueue, create new entry and send to downloader if unique, else delete and forget
        /// </summary>
        public async override Task<ConsumedMessageHandling> HandleMessageAsync(BasicDeliverEventArgs ea, DemoInsertInstruction model)
        {
            // Require the `GathererWorker` service upon receiving a message, ensuring a new instance and disposal.
            return await _serviceProvider.GetRequiredService<GathererWorker>().WorkAsync(model);
        }
    }
}
