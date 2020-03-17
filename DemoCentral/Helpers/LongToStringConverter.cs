using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DemoCentral.Helpers
{
    /// <summary>
    /// Converts values of type 'long' to 'string' upon serialization. 
    /// Required, because javascript does not have enough precision to accurately treat SteamIds as (long) integers.
    /// Example: The integer 76561198033880857 becomes 76561198033880860 in javascript.
    /// </summary>
    public class LongToStringConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jt = JValue.ReadFrom(reader);

            return jt.Value<long>();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(System.Int64).Equals(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }
    }
}
