using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TgBot_Proxy.Socks5Additionaltest
{
    public class AdditionalTestClient
    {
        private readonly ILogger<AddtionalTestClass> _logger;

        public AdditionalTestClient(ILogger<AddtionalTestClass> logger)
        {
            _logger = logger;
        }

        public HttpClient TestCleint(string Ip, string port)
        {
            try
            {
                var handler = new SocketsHttpHandler
                {
                    UseCookies = false,
                    MaxAutomaticRedirections = 10,
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    MaxConnectionsPerServer = 1,
                    EnableMultipleHttp2Connections = false,

                    UseProxy = true,
                    Proxy = new WebProxy(new Uri($"socks5://{Ip}:{port}")),
                    ConnectTimeout = TimeSpan.FromSeconds(5)
                };

                var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
                    TimeSpan.FromSeconds(5),
                     Polly.Timeout.TimeoutStrategy.Pessimistic,
                     onTimeoutAsync: async (context, timespan, task) =>
                     {
                         await Task.CompletedTask;
                     });

                var client = new HttpClient(new PolicyHttpMessageHandler(timeout)
                {
                    InnerHandler = handler
                });

                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");

                client.DefaultRequestVersion = HttpVersion.Version11;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }
}
