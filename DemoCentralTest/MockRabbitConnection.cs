using Moq;
using RabbitCommunicationLib.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

namespace DemoCentralTests
{
    public class MockRabbitConnection : IQueueConnection
    {
        public IConnection Connection { get; set; }
        public string Queue { get; set; } = "mockedQueue";

        public MockRabbitConnection()
        {
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<IModel>();


            mockConnection.Setup(x => x.CreateModel()).Returns(mockChannel.Object);

            Connection = mockConnection.Object;
        }
    }


    public class MockRPCQueueConnection : IRPCQueueConnections
    {
        public IQueueConnection ConsumeConnection { get; set; } = new MockRabbitConnection();
        public IQueueConnection ProduceConnection { get; set; } = new MockRabbitConnection();
    }
}
