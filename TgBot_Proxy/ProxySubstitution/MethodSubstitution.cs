using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot;
using TgBot_Proxy.AppSettingsParse;
using TgBot_Proxy.Cache;
using TgBot_Proxy.DataModel;
using TgBot_Proxy.DataModelJson;
using TgBot_Proxy.Socks5Additionaltest;

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

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Bot token is null or empty");           
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    var items = _cachedProxyList.GetCache<SOCKSData>();

                    foreach (var item in items)
                    {
                        string addurl = $"socks5://{item?.IP}:{item?.Port}";

                        var result = await _additionalTestClient.TestClass(item?.IP, item?.Port).ConfigureAwait(false);

                        if (result.Item1 == true)
                        {
                            HttpClient httpClient = new HttpClient(new HttpClientHandler
                            {
                                UseCookies = false,
                                UseProxy = true,
                                Proxy = new WebProxy(addurl)
                            })
                            {
                                Timeout = TimeSpan.FromSeconds(60)
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
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                }
                throw new OperationCanceledException(
                    "Не удалось создать TelegramBotClient: отмена или нет доступных прокси",
                    cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError("OperationCancled" + ex.Message + ex.StackTrace);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ex.StackTrace);
                throw;
            }
        }
    }
}
