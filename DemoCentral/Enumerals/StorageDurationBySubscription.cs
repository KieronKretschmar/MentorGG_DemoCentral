using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.Enumerals
{
    public class StorageDurationBySubscription
    {
        public static Dictionary<SubscriptionType, TimeSpan> Durations = new Dictionary<SubscriptionType, TimeSpan> 
            {
            {SubscriptionType.Free, TimeSpan.FromDays(14)},
            {SubscriptionType.Premium, TimeSpan.FromDays(60)},
            {SubscriptionType.Ultimate, TimeSpan.MaxValue}
        };

    }
}
