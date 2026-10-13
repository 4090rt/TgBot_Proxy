using Microsoft.Extensions.Logging;
using System.Text.Json;
using TgBot_Proxy.DataModel;

namespace TgBot_Proxy.HttpLocal
{
    public class GetMTProtoRequest
    {
        private const string Endpoint = "MTPROTO";

        private readonly ILogger<GetMTProtoRequest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GetMTProtoRequest(ILogger<GetMTProtoRequest> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<MTProtoData>> MTProtoDatasReuqst(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(HTTPSettnigs.ClientName);

                using var responce = await client.GetAsync(Endpoint, cancellationToken).ConfigureAwait(false);
                if (responce.IsSuccessStatusCode)
                {
                    await using var result = await responce.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);  

                    var json = await JsonSerializer.DeserializeAsync<List<MTProtoData>>(result, cancellationToken: cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidDataException($"Пустой ответ {Endpoint}");

                    if (json.Count != 0)
                        return json;
                    else
                        return new List<MTProtoData>();
                }
                else
                {
                    _logger.LogError("Не удалось получить список MTProto прокси с локального хоста" + responce.StatusCode);
                    return new List<MTProtoData>();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить список MTProto прокси с локального хоста");
                return [];
            }
        }
    }
}
