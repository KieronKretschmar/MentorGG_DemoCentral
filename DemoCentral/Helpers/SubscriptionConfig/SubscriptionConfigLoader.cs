using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DemoCentral.Helpers.SubscriptionConfig
{
    public interface ISubscriptionConfigProvider
    {
        SubscriptionConfig Config { get; }
    }

    /// <summary>
    /// Responsible for loading the User Subscription Config.
    /// </summary>
    public class SubscriptionConfigLoader : ISubscriptionConfigProvider
    {
        /// <summary>
        /// Location of th configuration file
        /// </summary>
        private const string CONFIG_PATH = "/app/config/subscriptions.json";

        /// <summary>
        /// The SubscriptionConfig instance.
        /// </summary>
        public SubscriptionConfig Config { get; private set; }

        /// <summary>
        /// Logger for internal errors.
        /// </summary>
        public ILogger<SubscriptionConfigLoader> _logger { get; }

        /// <summary>
        /// Instantiate the SubscriptionConfig.
        /// </summary>
        public SubscriptionConfigLoader(
            ILogger<SubscriptionConfigLoader> logger)
        {
            this._logger = logger;
            Config = LoadConfig();

            _logger.LogInformation(
                $"Loaded SubscriptionConfig [ { JsonConvert.SerializeObject(Config) }]");
        }

        /// <summary>
        /// Load the configuration from disk.
        /// </summary>
        /// <returns></returns>
        private SubscriptionConfig LoadConfig()
        {
            using (StreamReader file = File.OpenText(CONFIG_PATH))
            {
                try
                {
                    var fileContent = file.ReadToEnd();
                    return JsonConvert.DeserializeObject<SubscriptionConfig>(fileContent);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to deserialize SubscriptionConfig");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Mock Subscription Config Loader, For testing.
    /// </summary>
    public class MockedSubscriptionConfigLoader : ISubscriptionConfigProvider
    {
        private SubscriptionConfig defaultConfig => new SubscriptionConfig
        {
            Free = new SubscriptionSettings
            {
                MatchAccessDurationInDays = 14,
                DailyMatchesLimit = 3,
                FirstAndLastRoundsForSituations = 1,
            },
            Premium = new SubscriptionSettings
            {
                MatchAccessDurationInDays = 82,
                DailyMatchesLimit = -1,
                FirstAndLastRoundsForSituations = -1,
            },
            Ultimate = new SubscriptionSettings
            {
                MatchAccessDurationInDays = -1,
                DailyMatchesLimit = -1,
                FirstAndLastRoundsForSituations = -1,
            }
        };
        public SubscriptionConfig Config { get; }

        /// <summary>
        /// Create a MockedSubscriptionConfigLoader with a default configuration.
        /// </summary>
        public MockedSubscriptionConfigLoader()
        {
            Config = defaultConfig;
        }

        /// <summary>
        /// Create a MockedSubscriptionConfigLoader with a defined configuration.
        /// </summary>
        public MockedSubscriptionConfigLoader(SubscriptionConfig subscriptionConfig)
        {
            Config = subscriptionConfig;
        }
    }

}