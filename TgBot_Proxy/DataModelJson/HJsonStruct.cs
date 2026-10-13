using System.Text.Json.Serialization;

namespace TgBot_Proxy.DataModelJson
{
    public class HJsonStruct
    {
        [JsonPropertyName("LogLevel")]
        public DataModelLogging DataModelLogging { get; set; } = new DataModelLogging();

        [JsonPropertyName("BotToken")]
        public ModelToken ModelToken { get; set; } = new ModelToken();

        [JsonPropertyName("Proxy")]
        public ProxyBot ProxyBot { get; set; } = new ProxyBot();

        [JsonPropertyName("Local")]
        public LocalUri LocalUri { get; set; } = new LocalUri();
    }
}
