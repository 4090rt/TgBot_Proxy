using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgBot_Proxy.Cache;
using TgBot_Proxy.DataModel;
using TgBot_Proxy.ProxySubstitution;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;

namespace TgBot_Proxy.BotService
{
    public class Service : BackgroundService
    {
        private readonly ILogger<Service> _logger;
        private readonly CachedProxyList _cachedProxyList;
        private readonly MethodSubstitution _methodSubstitution;

        private const int PageSize = 10;
        private TelegramBotClient? _bot;
        private HttpClient? _httpclient;

        public Service(ILogger<Service> logger, CachedProxyList cachedProxyList, MethodSubstitution methodSubstitution)
        {
            _logger = logger;
            _cachedProxyList = cachedProxyList;
            _methodSubstitution = methodSubstitution;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var ctsiteration = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    var result = await _methodSubstitution.Methods(ctsiteration.Token).ConfigureAwait(false);

                    _bot = result.Item1;
                    _httpclient = result.Item2;

                    var receiverOptions = new ReceiverOptions
                    {
                        AllowedUpdates = [UpdateType.Message]
                    };

                    _bot.StartReceiving(
                        updateHandler: HandleUpdateAsync,
                        errorHandler: async (_, ex, src, _) =>
                        {
                            _logger.LogError(ex, "Ошибка при получении обновлений: {Source}", src);
                             RequestRestart(ctsiteration, "ошибка поллинга");
                        },
                        receiverOptions: receiverOptions,
                        cancellationToken: ctsiteration.Token);

                    var  watchtimer = WatchDogsAsync(_bot, ctsiteration);

                    try
                    {
                        await Task.Delay(Timeout.Infinite, ctsiteration.Token);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Бот остановлен");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Переподключение после обрыва");
                    }
                    finally
                    {
                        _httpclient?.Dispose();
                    }

                    await watchtimer.ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogError("OperationCancled" + ex.Message + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message + ex.StackTrace);
                }
            }
        }

        public void RequestRestart(CancellationTokenSource cancellationTokenSource, string reason)
        {
            if (cancellationTokenSource.IsCancellationRequested)
                return;

            _logger.LogWarning("Переподключение бота: {Reason}", reason);
            _ = cancellationTokenSource.CancelAsync();
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _httpclient?.Dispose();
            return base.StopAsync(cancellationToken);
        }

        private async Task WatchDogsAsync(TelegramBotClient telegram, CancellationTokenSource cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(3));

            while (await timer.WaitForNextTickAsync(cancellationToken.Token))
            {
                try
                {
                    await telegram.GetMe(cancellationToken.Token);
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Getme не прошел - соединение мертво");
                    RequestRestart(cancellationToken, "Getme не отвечает");
                    return;
                        
                }
            }
        }
        private static int ParsePage(string text)
        {
            // Поддерживаем форматы: "/getM 2", "/getM_2", "/getM2", просто "/getM" → страница 1
            var parts = text.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1 && int.TryParse(parts[1], out var page) && page > 0)
                return page;

            return 1; 
        }
        private static InlineKeyboardButton[] BuildNav(int page, int total, string prefix)
        {
            const int pageSize = 10;
            int totalPages = Math.Max(1, (total + pageSize - 1) / pageSize);

            var nav = new List<InlineKeyboardButton>();

            if (page > 1)
                nav.Add(InlineKeyboardButton.WithCallbackData("◀️", $"{prefix}_{page - 1}"));

            nav.Add(InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "noop"));

            if (page < totalPages)
                nav.Add(InlineKeyboardButton.WithCallbackData("▶️", $"{prefix}_{page + 1}"));

            return nav.ToArray();
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

            if (text.StartsWith("/getM"))
            {
                var page = ParsePage(text); 
                var list = _cachedProxyList.GetCache<MTProtoData>();

                int total = list.Count;
                if (total == 0)
                {
                    await bot.SendMessage(msg.Chat.Id, "📭 Список SOCKS5 пуст",
                        cancellationToken: stoppingToken);
                    return;
                }

                var slice = list.Skip((page - 1) * PageSize).Take(PageSize).ToList();
                var buttons = slice.Select(item => new[]
                {
                    InlineKeyboardButton.WithUrl(
                            $"🌐 {item.ServerKey}:{item.PortKey}",
                            $"https://t.me/proxy?server={item.ServerKey}&port={item.PortKey}&secret={item.SecretKey}")
                }).ToList();

                // Навигация
                var nav = new List<InlineKeyboardButton>();

                //назад
                if (page > 1) 
                    nav.Add(InlineKeyboardButton.WithCallbackData("◀️", $"proxy_m_{page - 1}"));

                // кнопка-индикатор
                nav.Add(InlineKeyboardButton.WithCallbackData($"{page}/{(list.Count + PageSize - 1) / PageSize}", "noop"));

                //вперед
                if (page * PageSize < list.Count) nav.Add(InlineKeyboardButton.WithCallbackData("▶️", $"proxy_m_{page + 1}"));

                //добавили кнопку
                if (nav.Count > 0) buttons.Add(nav.ToArray());

                await bot.SendMessage(msg.Chat.Id, "Выберите прокси:",
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: stoppingToken);
            }
            if (text.StartsWith("/getS"))
            {
                var page = ParsePage(text);
                var list = _cachedProxyList.GetCache<SOCKSData>();

                int total = list.Count;
                if (total == 0)
                {
                    await bot.SendMessage(msg.Chat.Id, "📭 Список SOCKS5 пуст",
                        cancellationToken: stoppingToken);
                    return;
                }
                var slice = list.Skip((page - 1) * PageSize).Take(PageSize).ToList();

                var sb = new StringBuilder("<b>🧦 SOCKS5 прокси:</b>\n\n");
                foreach (var item in slice)
                    sb.AppendLine($"<code>{item.IP}:{item.Port}</code>");

                var nav = BuildNav(page, list.Count, "proxy_s");
                await bot.SendMessage(msg.Chat.Id, sb.ToString(),
                    parseMode: ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[] { nav }),
                    cancellationToken: stoppingToken);
            }

            if (text.StartsWith("/stop"))
            {
                await bot.SendMessage(msg.Chat.Id, "Рассылка остановлена.", cancellationToken: stoppingToken);
                await StopAsync(stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
