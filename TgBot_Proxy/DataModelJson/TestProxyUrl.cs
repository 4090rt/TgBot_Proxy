using System.Text.Json.Serialization;

namespace TgBot_Proxy.DataModelJson
{
    public struct TestProxyUrl
    {
        [JsonPropertyName("UrlTest")]
        public string UrlTest { get; set; }
    }
}
