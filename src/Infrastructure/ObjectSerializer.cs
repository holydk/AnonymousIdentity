using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnonymousIdentity.Infrastructure
{
    internal static class ObjectSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        static ObjectSerializer()
        {
            Settings.Converters.Add(new NameValueCollectionConverter());
        }

        public static string ToString(object o)
        {
            return JsonConvert.SerializeObject(o, Settings);
        }

        public static T FromString<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, Settings);
        }

        public static JObject ToJObject(object o)
        {
            return JObject.FromObject(o, Serializer);
        }
    }
}