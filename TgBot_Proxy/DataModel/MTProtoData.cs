using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TgBot_Proxy.DataModel
{
    public struct MTProtoData
    {
        [JsonPropertyName("Server")]
        public string ServerKey { get; set; }
        [JsonPropertyName("Port")]
        public string PortKey { get; set; }
        [JsonPropertyName("Secret")]
        public string SecretKey { get; set; }
    }
}
