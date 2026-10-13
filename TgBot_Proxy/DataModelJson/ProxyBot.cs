using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TgBot_Proxy.DataModelJson
{
    public struct ProxyBot
    {
        [JsonPropertyName("Socks")]
        public string IP { get; set; }

    }
}
