using RabbitMQ.Client;
using System.Collections.Generic;
using System.Linq;

namespace RabbitTransfer
{
    /// <summary>
    /// Provides Utility for initializing the rabbitMQ transfers
    /// </summary>
    public static class RabbitInitializer
    {
        /// <summary>
        /// Sets up all the queues mentioning in the RPCExchange static property Exchanges
        /// </summary>
        /// <seealso cref="RPCExchange.EXCHANGES"/>
        public static void SetUpRPC()
        {
            var channel = GetNewConnection().CreateModel();
            List<string> allQueues = RPCExchange.EXCHANGES.SelectMany(x => x.Queues).ToList();
            foreach (string Queue in allQueues)
            {
                channel.QueueDeclare(queue: Queue, durable: false, exclusive: false, autoDelete: false);
            }
        }

        /// <summary>
        /// Declares a queue by creating a new channel and using this to create the queue
        /// </summary>
        /// <param name="name">name of the queue</param>
        public static void DeclareQueue(string name)
        {
            using (var channel = GetNewConnection().CreateModel())
            {
                channel.QueueDeclare(queue: name, durable: false, exclusive: false, autoDelete: false);
            }
        }

        /// <summary>
        /// Create a new connection to RabbitMQ
        /// DONT forget closing it.
        /// </summary>
        /// <returns>A new connection to our cloudAMQP Rabbit</returns>
        public static IConnection GetNewConnection()
        {
            //All access via the same user 
            var factory = new ConnectionFactory
            {
                HostName = "dove.rmq.cloudamqp.com",
                UserName = "zjwbyctn",
                VirtualHost = "zjwbyctn",
                Password = "9eKT0Ejw5uEJ3sDNxJYkJn_QehYl6UxH",
            };

            return factory.CreateConnection();
        }


    }

    /// <summary>
    /// Store information regarding the RPC pattern
    /// </summary>
    public class RPCExchange
    {
        public readonly string QUEUE;
        public readonly string REPLY_QUEUE;

        //ADD NEW RPC EXCHANGES HERE
        public static readonly RPCExchange DC_DFW = new RPCExchange("DC2DFW", "DFW2DC");
        public static readonly RPCExchange DC_DFW_HASH = new RPCExchange("DFW2DC_HASH", "DC2DFW_HASH");
        public static readonly RPCExchange DC_MP = new RPCExchange("DC2MP", "MP2DC");
        public static readonly RPCExchange DC_DD = new RPCExchange("DC2DD", "DD2DC");

        /// <summary>
        /// List of all the known exchanges
        /// MANUALLY CURATED
        /// </summary>
        public static List<RPCExchange> EXCHANGES
        {
            get
            {
                //ADD NEW RPC EXCHANGES HERE
                return new List<RPCExchange>
                {
                    DC_DFW,DC_DFW_HASH,DC_MP,DC_DD
                };
            }
        }

        /// <summary>
        /// List of Queues used in a RPCExchange
        /// </summary>
        public readonly List<string> Queues;

        /// <summary>
        /// Create a new RPCExchange with the specified queues
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="reply_queue"></param>
        public RPCExchange(string queue, string reply_queue)
        {
            QUEUE = queue;
            REPLY_QUEUE = reply_queue;
            Queues = new List<string> { QUEUE, REPLY_QUEUE };
        }
    }
}
