using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

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
        string QueueName { get; set; }
    }
}
