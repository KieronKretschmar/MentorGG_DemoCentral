using DemoCentral.Enumerals;

namespace DemoCentral.Models
{
    public class UserIdentity
    {
        public int ApplicationUserId { get; set; }
        public long SteamId { get; set; }
        public SubscriptionType SubscriptionType { get; set; }
        public int DailyMatchesLimit { get; set; }
    }

}
