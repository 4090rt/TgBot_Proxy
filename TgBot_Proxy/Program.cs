using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TgBot_Proxy.AppSettingsParse;
using TgBot_Proxy.BotService;
using TgBot_Proxy.Cache;
using TgBot_Proxy.DataModel;
using TgBot_Proxy.DataModelJson;
using TgBot_Proxy.HttpLocal;

namespace TgBot_Proxy
{
    public class Program
    {
        private static ILogger<Program> _logger = null!;
        private static HJsonStruct _settings = null!;

        public static async Task Main(string[] args)
        {
            _settings = ParseJson.Parse<HJsonStruct>();

            using var loggerFactory = LoggerFactory.Create(builder => ConfigureLogging(builder, _settings));
            _logger = loggerFactory.CreateLogger<Program>();

            var servise = new ServiceCollection();

            servise.AddLogging(build => ConfigureLogging(build, _settings));

            servise.AddSingleton(_settings);
            servise.AddCleintLocal(_settings, loggerFactory.CreateLogger(HTTPSettnigs.ClientName));

            servise.AddScoped<GetSocksReuqest>();
            servise.AddScoped<GetMTProtoRequest>();
            servise.AddScoped<CachedProxyList>();
            servise.AddMemoryCache();

            servise.AddSingleton<Service>();

            var serviceProvider = servise.BuildServiceProvider();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            var bot = serviceProvider.GetRequiredService<Service>();

            await bot.StartAsync(cts.Token);

            _logger.LogInformation("TgBot_Proxy запущен, ожидаю Ctrl+C");

            Timed(serviceProvider, cts.Token);

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
            }

            _logger.LogInformation("Останавливаю фоновые сервисы");

            await bot.StopAsync(CancellationToken.None);

            StopTimed();

            await serviceProvider.DisposeAsync();
        }

        public static void ConfigureLogging(ILoggingBuilder logging, HJsonStruct settings)
        {
            logging.AddConsole();

            logging.SetMinimumLevel(ParseLogLevel(settings.DataModelLogging.Default));
            logging.AddFilter("Microsoft", ParseLogLevel(settings.DataModelLogging.Default));
            logging.AddFilter("Microsoft.AspNetCore", ParseLogLevel(settings.DataModelLogging.MicrosoftAspNetCore));
        }

        public static Microsoft.Extensions.Logging.LogLevel ParseLogLevel(string level)
        {
            try
            {
                return level?.ToLower() switch
                {
                    "trace" => Microsoft.Extensions.Logging.LogLevel.Trace,
                    "debug" => Microsoft.Extensions.Logging.LogLevel.Debug,
                    "information" => Microsoft.Extensions.Logging.LogLevel.Information,
                    "warning" => Microsoft.Extensions.Logging.LogLevel.Warning,
                    "error" => Microsoft.Extensions.Logging.LogLevel.Error,
                    "critical" => Microsoft.Extensions.Logging.LogLevel.Critical,
                    "none" => Microsoft.Extensions.Logging.LogLevel.None,
                    _ => Microsoft.Extensions.Logging.LogLevel.Information
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                return Microsoft.Extensions.Logging.LogLevel.Information;
            }
        }

        public static Timer? _proxyTimer = null;

        public static void Timed(ServiceProvider serviceProvider, CancellationToken stoppingToken)
        {
            int _isRefreshing = 0;

            try
            {
                var MtprotoGet = serviceProvider.GetRequiredService<GetMTProtoRequest>();
                var SOCKSGet = serviceProvider.GetRequiredService<GetSocksReuqest>();
                var CacheProxy = serviceProvider.GetRequiredService<CachedProxyList>();

                _logger.LogInformation("Запускаю таймер обновления прокси (каждые 12 часов)");

                try
                {
                    _proxyTimer = new Timer(async _ =>
                    {
                        if (Interlocked.Exchange(ref _isRefreshing, 1) == 1)
                        {
                            _logger.LogWarning("Обновление уже выполняется, пропускаем тик");
                            return;
                        }

                        try
                        {
                            Task<List<MTProtoData>> mTProtoDatas = MtprotoGet.MTProtoDatasReuqst(stoppingToken);
                            Task<List<SOCKSData>> sOCKSDatas = SOCKSGet.SOCKSDatasReuqst(stoppingToken);

                            await Task.WhenAll(mTProtoDatas, sOCKSDatas).ConfigureAwait(false);

                            List<MTProtoData> list1 = await mTProtoDatas.ConfigureAwait(false);
                            List<SOCKSData> list2 = await sOCKSDatas.ConfigureAwait(false);

                            if (stoppingToken.IsCancellationRequested)
                            {
                                _logger.LogInformation("Приложение останавливается, кэш не обновляю");
                                return;
                            }

                            CacheProxy.Cache(list1);
                            CacheProxy.Cache(list2);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Обновление прокси отменено остановкой приложения, кэш не трогаю");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Ошибка обновления прокси в таймере");
                        }
                        finally
                        {
                            Interlocked.Exchange(ref _isRefreshing, 0);
                        }
                    }, null, TimeSpan.Zero, TimeSpan.FromHours(12));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Возникло исключение в таймере обновления" + ex.Message);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Возникло исключение в таймере обновления" + ex.Message);
                _proxyTimer?.Dispose();
                return;
            }
        }

        public static void StopTimed()
        {
            var timer = Interlocked.Exchange(ref _proxyTimer, null);

            if (timer is null)
                return;

            using var inFlight = new ManualResetEvent(false);

            if (!timer.Dispose(inFlight))
                return;

            if (inFlight.WaitOne(TimeSpan.FromSeconds(10)))
                _logger.LogInformation("Таймер обновления прокси остановлен");
            else
                _logger.LogWarning("Таймер обновления прокси не завершил текущий запрос за 10 секунд");
        }
    }
}
