using System.Text.Json.Serialization;
namespace TgBot_Proxy.DataModelJson
{
    public struct ModelToken
    {
        [JsonPropertyName("Token")]
        public string Token { get; set; }
    }
}
