using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;

namespace RabbitTransfer.Interfaces
{
    public interface IQueueConnection
    {
        /// <summary>
        /// AMQP Connection
        /// </summary>
        IConnection Connection { get; set; }

        /// <summary>
        /// AMQP Queue to consume.
        /// </summary>
        string Queue { get; set; }
    }
}
