using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot_Proxy.DataModel
{
    public struct ProxyStats
    {
        public int ProxiesAll { get; set; }
        public int ProxiesTested { get; set; }
        public DateTime DateTime { get; set; }
    }
}
