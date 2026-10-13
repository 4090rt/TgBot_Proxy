using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TgBot_Proxy.DataModelJson
{
    public struct ModelToken
    {
        [JsonPropertyName("Token")]
        public string Token { get; set; }
    }
}
