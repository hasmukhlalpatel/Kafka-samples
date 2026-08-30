using Confluent.Kafka;
using System.Text;

namespace Kafka.Schemas.Shared.Extensions
{
    public static class HeadersExtensions
    {
        public static void AddHeader(this Headers headers, string key, Guid value)
        {
            AddHeader(headers, key, value.ToString());
        }

        public static void AddHeader(this Headers headers, string key, string value)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            headers.Add(key, Encoding.UTF8.GetBytes(value));
        }

        public static bool TryGetHeader(this Headers headers, string key, out string value)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (key == null) throw new ArgumentNullException(nameof(key));
            var header = headers.FirstOrDefault(h => h.Key == key);
            
            if (header == null)
            {
                value = null;
                return false;
            }

            value = Encoding.UTF8.GetString(header.GetValueBytes());
            return true;
        }
    }
}
