using System.Text.Json.Serialization;
namespace TgBot_Proxy.DataModel
{
    public class SOCKSData
    {
        [JsonPropertyName("IpS")]
        public string? IP { get; set; }
        [JsonPropertyName("IpP")]
        public string? Port { get; set; }
    }
}
