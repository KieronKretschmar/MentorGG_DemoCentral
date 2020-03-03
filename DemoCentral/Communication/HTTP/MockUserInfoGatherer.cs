using DemoCentral.Communication.HTTP;
using RabbitCommunicationLib.Enums;
using System.Threading.Tasks;

namespace DemoCentral
{
    internal class MockUserInfoGatherer : IUserInfoOperator
    {

        public Task<AnalyzerQuality> GetAnalyzerQualityAsync(long steamId)
        {
            return Task.FromResult(AnalyzerQuality.Medium);
        }
    }
}