using Microsoft.Extensions.Logging;
using System.Text.Json;
using TgBot_Proxy.DataModel;

namespace TgBot_Proxy.HttpLocal
{
    public class GetAllProxy
    {
        private const string Endpoint = "/ALL";

        private readonly ILogger<GetAllProxy> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GetAllProxy(ILogger<GetAllProxy> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<AllProxies>> RequestAll(CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("CleintLocal");

                using HttpResponseMessage httpResponseMessage = await client.GetAsync(Endpoint, cancellationToken).ConfigureAwait(false);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    await using var json = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                    var jsonser = await JsonSerializer.DeserializeAsync<List<AllProxies>>(json, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (jsonser.Count != 0)
                        return jsonser;
                    else
                        return new List<AllProxies>();
                }
                else
                {
                    _logger.LogError("Не удалось получить список MTProto прокси с локального хоста" + httpResponseMessage.StatusCode);
                    return new List<AllProxies>();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить списки прокси с локального хоста");
                return new List<AllProxies>();
            }
        }
    }
}