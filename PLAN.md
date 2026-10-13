# PLAN.md — TgBot_Proxy

## Полная структура проекта

```
TgBot_Proxy.sln
TgBot_Proxy/
├── AppSettings.json                 # конфиг (LogLevel, BotToken, Proxy, Local)
├── TgBot_Proxy.csproj               # net9.0, Telegram.Bot 22.10, Polly, MemoryCache
├── Program.cs                       # DI, старт бота, таймер обновления раз в 12ч
├── AppSettingsParse/
│   └── ParseJson.cs                 # чтение+десериализация AppSettings.json
├── BotService/
│   └── Service.cs                   # BackgroundService: /start, /get, /stop (каркас)
├── Cache/
│   └── CachedProxyList.cs           # fresh 12ч + stale 24ч в IMemoryCache
├── DataModel/
│   ├── MTProtoData.cs               # Server / Port / Secret
│   └── SOCKSData.cs                 # IpS / IpP
├── DataModelJson/
│   ├── HJsonStruct.cs               # корень конфига
│   ├── DataModelLogging.cs          # LogLevel.Dafault / Microsoft.AspNetCore
│   ├── ModelToken.cs                # BotToken.Token
│   ├── ProxyBot.cs                  # Proxy.Socks
│   └── LocalUri.cs                  # Local.UriLocal
├── HttpLocal/
│   ├── HTTPSettnigs.cs              # HttpClient + Polly (timeout/circuit/retry)
│   ├── GetMTProtoRequest.cs         # GET {Local}/MTPROTO
│   └── GetSocksReuqest.cs           # GET {Local}/SOCKS5
├── bin/                             # сборка
└── obj/                             # сборка
```

Поток данных: `AppSettings.json` → `ParseJson` → `HJsonStruct` → таймер `Program.Timed` → `HttpLocal.Get*Request` → `CachedProxyList` → **должен идти в `Service` (бот) — НЕ реализовано**.

## Быстрый план (что не хватает до MVP)

1. **Нет метода отдачи прокси** — в `Service.cs` нет `SendCachedProxiesAsync`, `CachedProxyList.GetCache<T>()` нигде не вызывается.
2. **`/get` пустой** — вызов закомментирован (`Service.cs:80`), команда ничего не возвращает.
3. **Нет форматирования ссылок** — не строятся `t.me/proxy?server=..&port=..&secret=..` (MTProto) и `t.me/socks?server=..&port=..` (SOCKS5).
4. **Нет батчинга** — `ProxiesPerMessage = 5` объявлен, но не используется; длинные списки не разбиваются.
5. **Токен бота пустой** — `AppSettings.json` `BotToken.Token = ""`, нет валидации при старте → падение `TelegramBotClient`.
6. **`/stop` вводит в заблуждение** — пишет «рассылка остановлена», хотя подписок/рассылки нет вообще.
7. **Прокси для самого бота не подключён** — `GetProxyAsync` только в комментарии (`Service.cs:37-38`), `Proxy.Socks` нигде не применяется.
8. **DI-scope баг** — scoped-сервисы берутся из root-провайдера в `Program.Timed` (`Program.cs:113-115`), а не из scope на каждый тик.
9. **Контракт JSON не подтверждён** — сверить `MTProtoData` (`Server/Port/Secret`) и `SOCKSData` (`IpS/IpP`) с ответом локального хоста.
10. **Мелочи** — `FreshTtl`/`StaleTtl` не используются (хардкод 12/24ч); структуры с non-nullable `string` (CS8618); опечатка `"Dafault"`.

## Шаги реализации

1. Заполнить и валидировать конфиг (токен, URI, прокси) — `Program.cs`, `AppSettings.json`.
2. Добавить `BotService/ProxyFormatter.cs` — формирование `t.me`-ссылок.
3. Реализовать `SendCachedProxiesAsync` + вызвать в `/get` — `Service.cs`.
4. Ппределиться с `/stop`/рассылкой (убрать или реализовать подписчиков) — `Service.cs`.
5. (опц.) Подключить SOCKS-прокси к `TelegramBotClient` — `Service.cs`, `AppSettings.json`.
6. Починить scope в `Program.Timed` — `Program.cs`.
7. Сверить JSON-контракт и убрать мелочи — `DataModel/*`, `Cache/CachedProxyList.cs`.

## Критерии готовности MVP

- [ ] Бот стартует с валидным токеном; при пустом — понятная ошибка.
- [ ] `/start` показывает команды.
- [ ] `/get` отдаёт прокси из кэша ссылками `t.me/proxy` и/или `t.me/socks`, батчами по 5.
- [ ] Пустой кэш обрабатывается без исключений.
- [ ] Обновление кэша раз в 12ч работает без scope-ошибок.
- [ ] `/stop` осмыслен или убран.
