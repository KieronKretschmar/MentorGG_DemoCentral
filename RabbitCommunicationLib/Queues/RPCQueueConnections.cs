using RabbitTransfer.Interfaces;

namespace RabbitTransfer.Queues
{
    public class RPCQueueConnections : IRPCQueueConnections
    {
        /// <summary>
        /// Create a set of QueueConnections.
        /// </summary>
        /// <param name="rabbitUri">Rabbit URI</param>
        /// <param name="consumeQueueName">Consume Queue Name</param>
        /// <param name="produceQueueName">Produce Queue Name</param>
        public RPCQueueConnections(string rabbitUri, string consumeQueueName, string produceQueueName)
        {
            ConsumeConnection = new QueueConnection(rabbitUri, consumeQueueName);
            ProduceConnection = new QueueConnection(rabbitUri, produceQueueName);
        }
        public IQueueConnection ConsumeConnection { get ; set ; }
        public IQueueConnection ProduceConnection { get ; set ; }
    }
}
