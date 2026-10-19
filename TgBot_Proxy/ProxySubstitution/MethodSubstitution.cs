using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using TgBot_Proxy.AppSettingsParse;
using TgBot_Proxy.Cache;
using TgBot_Proxy.DataModel;
using TgBot_Proxy.DataModelJson;
using TgBot_Proxy.Socks5Additionaltest;
using static System.Net.WebRequestMethods;

namespace TgBot_Proxy.ProxySubstitution
{
    public class MethodSubstitution
    {
        private readonly ILogger<MethodSubstitution> _logger;
        private readonly CachedProxyList _cachedProxyList;
        private readonly AddtionalTestClass _additionalTestClient;

        public MethodSubstitution(ILogger<MethodSubstitution> logger, CachedProxyList cachedProxyList, AddtionalTestClass
            additionalTestClient)
        {
            _logger = logger;
            _cachedProxyList = cachedProxyList;
            _additionalTestClient = additionalTestClient;
        }

        public async Task<(TelegramBotClient, HttpClient)> Methods(CancellationToken cancellationToken)
        {
            try
            {
                string token = ParseJson.Parse<HJsonStruct>().ModelToken.Token;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var items = _cachedProxyList.GetCache<SOCKSData>();

                    foreach (var item in items)
                    {
                        string addurl = $"http://{item?.IP}:{item?.Port}";

                        var result = await _additionalTestClient.TestClass(item?.IP, item?.Port).ConfigureAwait(false);

                        if (result.Item1 == true)
                        {
                            using HttpClient httpClient = new HttpClient(new HttpClientHandler
                            {
                                UseCookies = false,
                                UseProxy = true,
                                Proxy = new WebProxy(addurl)
                            })
                            {
                                Timeout = TimeSpan.FromSeconds(10)
                            };
                            try
                            {
                                TelegramBotClient telegramBotClient = new TelegramBotClient(token, httpClient);
                                await telegramBotClient.GetMe(cancellationToken);
                                return (telegramBotClient, httpClient);
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                httpClient.Dispose();
                            }
                            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                        }
                    }
                }
                throw new OperationCanceledException(
                    "Не удалось создать TelegramBotClient: отмена или нет доступных прокси",
                    cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError("OperationCancled" + ex.Message + ex.StackTrace);
                throw new Exception();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ex.StackTrace);
                throw new Exception();
            }
        }
    }
}
