using Microsoft.Extensions.Logging;
using System.Text.Json;
using TgBot_Proxy.DataModel;

namespace TgBot_Proxy.HttpLocal
{
    public class GetSocksReuqest
    {
        private const string Endpoint = "SOCKS5";

        private readonly ILogger<GetSocksReuqest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GetSocksReuqest(ILogger<GetSocksReuqest> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<SOCKSData>> SOCKSDatasReuqst(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(HTTPSettnigs.ClientName);

                using var responce = await client.GetAsync(Endpoint, cancellationToken);
                if (responce.IsSuccessStatusCode)
                {
                    await using var stream = await responce.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                    var json = await JsonSerializer.DeserializeAsync<List<SOCKSData>>(stream, cancellationToken: cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidDataException($"Пустой ответ {Endpoint}");

                    if (json.Count != 0)
                        return json;
                    else
                        return new List<SOCKSData>();
                }
                else
                {
                    _logger.LogError("Не удалось получить список SOCKS прокси с локального хоста" + responce.StatusCode);
                    return new List<SOCKSData>();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить список SOCKS прокси с локального хоста");
                return [];
            }
        }
    }
}
