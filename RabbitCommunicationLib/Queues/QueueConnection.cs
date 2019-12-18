using RabbitMQ.Client;
using RabbitTransfer.Interfaces;
using System;

namespace RabbitTransfer.Queues
{
    internal static class ConnectionFactoryHelper
    {
        /// <summary>
        /// Create a connection from a URI
        /// </summary>
        /// <param name="uriString">Rabbit URI</param>
        /// <returns>AMQP Conneciton</returns>
        public static IConnection FromUriString(string uriString)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(uriString)
            };
            return factory.CreateConnection();
        }
    }

    public class QueueConnection : IQueueConnection
    {
        /// <summary>
        /// Assert a value is not Null or Empty.
        /// </summary>
        /// <param name="key">Key to attach to Exception</param>
        /// <param name="value">Value to assert</param>
        internal static void AssertValidValue(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(key);
            }
        }

        /// <summary>
        /// Create a QueueConnection.
        /// </summary>
        /// <param name="rabbitUri">Rabbit URI</param>
        /// <param name="queueName">Queue Name</param>
        public QueueConnection(string rabbitUri, string queueName)
        {
            AssertValidValue("rabbitUrl", rabbitUri);
            AssertValidValue("queueName", queueName);

            Connection = ConnectionFactoryHelper.FromUriString(rabbitUri);
            Queue = queueName;
        }

        public IConnection Connection { get; set; }
        public string Queue { get; set; }
    }
}
