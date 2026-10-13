using System.Text.Json.Serialization;

namespace TgBot_Proxy.DataModelJson
{
    public struct DataModelLogging
    {
        [JsonPropertyName("Dafault")]
        public string Default { get; set; }

        [JsonPropertyName("Microsoft.AspNetCore")]
        public string MicrosoftAspNetCore { get; set; }
    }
}
