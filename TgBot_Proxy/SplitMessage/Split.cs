using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot_Proxy.SplitMessage
{
    public class Split
    {
        private readonly ILogger<Split> _logger;
        private const int MaxLenght = 4000;

        public Split(ILogger<Split> logger)
        {
            _logger = logger;
        }

        public static List<string> SplitMessage(StringBuilder sb)
        {
            try
            {
            }
            catch (Exception ex)
            {
            
            }
        }
    }
}
