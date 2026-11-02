using Microsoft.Extensions.Logging;
using System.Text.Json;
using TgBot_Proxy.DataModel;

namespace TgBot_Proxy.HttpLocal
{
    public class GetStats
    {
        private const string Endpoint = "STATS";
        private readonly ILogger<GetStats> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GetStats(ILogger<GetStats> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ProxyStats> StatsProxy(CancellationToken  cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(HTTPSettnigs.ClientName);

                using HttpResponseMessage httpResponseMessage = await client.GetAsync(Endpoint, cancellationToken: cancellation).ConfigureAwait(false);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    await using var json = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    var jsonser = await JsonSerializer.DeserializeAsync<ProxyStats>(json, cancellationToken: cancellation).ConfigureAwait(false);

                    if (jsonser is { ProxiesAll: > 0 })
                        return jsonser;
                    else
                        return new ProxyStats();
                }
                else
                {
                    _logger.LogError("Не удалось получить статистику прокси с локального хоста" + httpResponseMessage.StatusCode);
                    return new ProxyStats();
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить списки прокси с локального хоста");
                return new ProxyStats();
            }
        }
    }
}
