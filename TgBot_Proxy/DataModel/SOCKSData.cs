using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
