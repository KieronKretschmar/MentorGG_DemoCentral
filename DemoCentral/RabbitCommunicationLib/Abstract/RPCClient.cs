using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitTransfer.Abstract
{
    public abstract class RPCClient<T> : IDisposable
    {
        public abstract string QUEUE_NAME { get; }
        public abstract string REPLY_QUEUE { get; }

        private readonly IConnection _connection;
        private readonly IModel _channel;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<T>> _callbackMapper =
                       new ConcurrentDictionary<string, TaskCompletionSource<T>>();

        /// <summary>
        /// Initialize rpc pattern
        /// </summary>
        protected RPCClient()
        {
            _connection = RabbitInitializer.GetNewConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(queue: REPLY_QUEUE, durable: false, exclusive: false, autoDelete: false);

            var consumer = new EventingBasicConsumer(_channel);

            _channel.BasicConsume(
        consumer: consumer,
        queue: REPLY_QUEUE,
        autoAck: true);

            consumer.Received += (model, ea) =>
            {
                long matchId = long.Parse(ea.BasicProperties.CorrelationId);
                if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out TaskCompletionSource<T> tcs))
                {
                    Log.WriteLine(string.Format("Got demo with unknown correlationId {0}", matchId.ToString()));
                    return;
                }

                var body = Encoding.UTF8.GetString(ea.Body);
                var response = JsonConvert.DeserializeObject<T>(body);

                HandleReplyQueue(matchId, response);

                tcs.TrySetResult(response);
            };
        }

        /// <summary>
        /// Send a demo to the queue, demo is the first parameter
        /// </summary>
        /// <param name="demoModel">the transfer model</param>
        /// <param name="matchId">id of the match </param>
        /// <returns>async task to await, result is return model</returns>
        public Task<T> SendNewDemo(long matchId, string demoModel, CancellationToken cancellationToken = default(CancellationToken))
        {

            IBasicProperties props = _channel.CreateBasicProperties();
            var correlationId = matchId.ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = REPLY_QUEUE;
            var messageBytes = Encoding.UTF8.GetBytes(demoModel);
            var tcs = new TaskCompletionSource<T>();

            _callbackMapper.TryAdd(correlationId, tcs);


            _channel.BasicPublish(
                exchange: "",
                routingKey: QUEUE_NAME,
                basicProperties: props,
                body: messageBytes);

            cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out var tmp));

            //RETURN TASK OBJECT
            return tcs.Task;
        }

        /// <summary>
        /// Handle responses on the replyQueue
        /// </summary>
        /// <param name="matchId">id of the demo correlated to the response</param>
        /// <param name="response">model of the response</param>
        public abstract void HandleReplyQueue(long matchId, T response);

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Dispose()
        {
            _connection.Close();
        }
    }
}