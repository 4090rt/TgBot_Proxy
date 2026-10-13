using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TgBot_Proxy.DataModelJson;

namespace TgBot_Proxy.HttpLocal
{
    public static class HTTPSettnigs
    {
        public const string ClientName = "CleintLocal";

        public static IServiceCollection AddCleintLocal(this IServiceCollection serviceDescriptors, HJsonStruct settings, ILogger logger)
        {
            var clientBuilder = serviceDescriptors.AddHttpClient(ClientName, client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("TgBot_Proxy/1.0");

                client.Timeout =  Timeout.InfiniteTimeSpan;

                client.BaseAddress = new Uri(settings.LocalUri.URI);
            });

            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

                clientBuilder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromMinutes(5),
                TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    logger.LogWarning("⏰ Общий Запрос к локальному хосту превысил таймаут {Timeout} в {Time}", timespan, DateTime.UtcNow);
                    return Task.CompletedTask;
                }));

            clientBuilder.AddTransientHttpErrorPolicy(polly => polly.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (outcome, timespan) =>
                {
                    logger.LogWarning("Cистема CircuitBreaker LocalHost: circuit opened for {Duration}", timespan);
                },
                onHalfOpen: () =>
                {
                    logger.LogWarning("Cистема CircuitBreaker LocalHost: half-open");
                },
                onReset: () =>
                {
                    logger.LogWarning("Cистема CircuitBreaker LocalHost: circuit reset");
                }));

            clientBuilder.AddTransientHttpErrorPolicy(polly => polly.WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retrycount => TimeSpan.FromSeconds(Math.Pow(2, retrycount))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetryAsync: (outcome, timespan, retrycount, context) =>
                {
                    logger.LogWarning("Повтор запроса к локальному хосту #{Attempt} через {Delay}", retrycount, timespan);
                    return Task.CompletedTask;
                }));

            clientBuilder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromMinutes(1),
                Polly.Timeout.TimeoutStrategy.Optimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    logger.LogWarning("⏰ Запрос к локальному хосту превысил таймаут {Timeout} в {Time}", timespan, DateTime.UtcNow);
                    return Task.CompletedTask;
                }));

            return serviceDescriptors;
        }
    }
}
