using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgBot_Proxy.AppSettingsParse;
using TgBot_Proxy.Cache;
using TgBot_Proxy.DataModel;
using TgBot_Proxy.DataModelJson;

namespace TgBot_Proxy.BotService
{
    public class Service : BackgroundService
    {
        private const int ProxiesPerMessage = 5;

        private readonly ILogger<Service> _logger;
        private readonly HJsonStruct _settings;
        private readonly CachedProxyList _cachedProxyList;
        private readonly IServiceScopeFactory _scopeFactory;

        private TelegramBotClient _bot = null!;

        public Service(ILogger<Service> logger, HJsonStruct settings, IServiceScopeFactory scopeFactory, CachedProxyList cachedProxyList)
        {
            _logger = logger;
            _settings = settings;
            _scopeFactory = scopeFactory;
            _cachedProxyList = cachedProxyList; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var token = _settings.ModelToken.Token;

            // Прокси для Telegram будем получать из отдельного метода, пока отключён:
            // string proxy = await GetProxyAsync(stoppingToken);

            _bot = new TelegramBotClient(token);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message]
            };

            _bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: (_, ex, src, _) =>
                {
                    _logger.LogError(ex, "Ошибка при получении обновлений: {Source}", src);
                    return Task.CompletedTask;
                },
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken stoppingToken)
        {
            if (update.Message is not { Text: { } text } msg) return;

            if (text.StartsWith("/start"))
            {
                await bot.SendMessage(msg.Chat.Id,
                    "Привет. Команды:\n/get — получить прокси\n/stop — остановить рассылку",
                    cancellationToken: stoppingToken);
                return;
            }

            if (text.StartsWith("/get"))
            {
                var listproxy = _cachedProxyList.GetCache<MTProtoData>();
                await bot.SendMessage(msg.Chat.Id, $"Получено прокси: {listproxy.Count}", cancellationToken: stoppingToken);
                return;
            }

            if (text.StartsWith("/stop"))
            {
                await bot.SendMessage(msg.Chat.Id, "Рассылка остановлена.", cancellationToken: stoppingToken);
            }
        }
    }
}
