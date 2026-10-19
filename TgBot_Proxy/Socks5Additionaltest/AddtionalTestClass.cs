using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgBot_Proxy.AppSettingsParse;
using TgBot_Proxy.DataModelJson;

namespace TgBot_Proxy.Socks5Additionaltest
{
    public class AddtionalTestClass
    {
        private readonly ILogger _logger;
        private readonly AdditionalTestClient _additionalTestClient;

        public AddtionalTestClass(ILogger logger, AdditionalTestClient additionalTestClient)
        {
            _logger = logger;
            _additionalTestClient = additionalTestClient;
        }

        public async Task<(bool, string)> TestClass(string Ip, string Port)
        {
            try
            {
                if (!string.IsNullOrEmpty(Ip) || !string.IsNullOrEmpty(Port))
                    return (false, "Айпи или порт пусты");

                string url = ParseJson.Parse<HJsonStruct>().ProxyUrl.UrlTest;

                if (!string.IsNullOrEmpty(url))
                    return (false, "Url пуст");

                using HttpClient client = _additionalTestClient.TestCleint(Ip, Port);

                HttpResponseMessage httpResponseMessage = await client.GetAsync(url).ConfigureAwait(false);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.OK || httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        || httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Found || httpResponseMessage.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                        return (true, "Успех");
                    else
                        return (false, $"{httpResponseMessage.StatusCode}");
                }
                else
                {
                    return (false, $"{httpResponseMessage.StatusCode}");
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError("OperationCancled" + ex.Message + ex.StackTrace);
                return (false, $"{ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ex.StackTrace);
                return (false, $"{ex.Message}");
            }
        }
    }
}
