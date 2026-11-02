using System.Text.Json.Serialization;

namespace TgBot_Proxy.DataModel
{
    public struct ProxyStats
    {
        [JsonPropertyName("AllProxy")]
        public int ProxiesAll { get; set; }
        [JsonPropertyName("Tested")]
        public int ProxiesTested { get; set; }
        [JsonPropertyName("Date")]
        public DateTime DateTime { get; set; }
    }
}
