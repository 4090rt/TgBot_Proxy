using System.Text.Json.Serialization;

namespace TgBot_Proxy.DataModelJson
{
    public struct LocalUri
    {
        [JsonPropertyName("UriLocal")]
        public string URI { get; set; }
    }
}
