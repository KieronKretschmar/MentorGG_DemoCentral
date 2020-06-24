using Database.DatabaseClasses;
using DemoCentral;
using DemoCentral.Communication.HTTP;
using DemoCentral.Enumerals;
using DemoCentral.Helpers.SubscriptionConfig;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static DemoCentral.Communication.HTTP.MatchInfoGetter;

namespace DemoCentralTests
{
    [TestClass]
    public class MatchInfoGetterTests
    {
        [TestMethod]
        public async Task CalculatesCorrectAsync()
        {
            var testMatchId = 123456789;
            var testDemo = new Demo { MatchId = testMatchId, MatchDate = DateTime.UtcNow };
            var testIds = new List<long> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var model = new PlayerInMatchModel { Players = testIds };
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(model));

            var messageHandler = new MockMessageHandler((req, token) => Task.FromResult(response));

            var mockedHttpClient = new HttpClient(messageHandler);
            mockedHttpClient.BaseAddress = new UriBuilder().Uri;


            var httpfactory = new MockHttpClientFactory();
            httpfactory.RegisterClient("match-retriever", mockedHttpClient);

            var mockSubscriptionConfigLoader = new MockedSubscriptionConfigLoader();

            var mockUserIdentityRetriever = new Mock<IUserIdentityRetriever>();
            mockUserIdentityRetriever.Setup(x => x.GetHighestUserSubscription(testIds)).Returns(Task.FromResult(SubscriptionType.Premium));

            var mockLogger = new Mock<ILogger<MatchInfoGetter>>();
            var mockDbInterface = new Mock<IDemoTableInterface>();
            mockDbInterface.Setup(x => x.GetDemoById(testMatchId)).Returns(testDemo);

            var test = new MatchInfoGetter(
                httpfactory,
                mockUserIdentityRetriever.Object,
                mockDbInterface.Object,
                mockSubscriptionConfigLoader,
                mockLogger.Object);

            var res = await test.CalculateDemoRemovalDateAsync(testDemo);

            Assert.IsInstanceOfType(res, typeof(DateTime));
            Assert.IsTrue(res > DateTime.UtcNow);
        }
    }
}