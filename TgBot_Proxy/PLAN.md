# TgBot_Proxy — план до запуска

> Обновлено: 20.10.2026. Сборка: **0 ошибок, 0 предупреждений** (`dotnet build`).
> Блокеры B1–B7 закрыты, схема `socks5://`, мёртвый код и warning'и убраны.

---

## 0. До запуска — чек-лист

Без этих пунктов бот либо не стартует, либо работает только «на бумаге».

- [ ] **`BotToken` в `AppSettings.json`** — сейчас пустой, бот падает молча
- [ ] **Локальный API поднят** — `http://127.0.0.1:2015/api/proxy/` отвечает
- [ ] **Health-check `GetMe` + переподключение** (см. 1.1) — сейчас бот не замечает смерть прокси
- [ ] **`/get` отдаёт адреса, а не счётчик** (см. 1.2) — иначе функционал бота пустой
- [ ] **`HttpClient` закрывается при остановке** — сейчас течёт
- [ ] **Живой прогон**: пул из 1 нерабочего + 1 рабочего прокси, `/get`, Ctrl+C

---

## 1. Обязательное

### 1.1 Health-check и переподключение
`BotService/Service.cs:59` — `await Task.Delay(Timeout.Infinite)` выходит **только по отмене**:
обрыва соединения бот не замечает.

- [ ] `PeriodicTimer` + `GetMe` каждые 3–5 мин
- [ ] При неудаче: dispose старого `HttpClient` → повтор `while` (подмена прокси через `MethodSubstitution.Methods`)
- [ ] Dispose текущего `HttpClient` в `StopAsync` (сейчас — никогда)

### 1.2 Команда `/get`
`BotService/Service.cs` — отдаёт только **число**.

- [ ] Выводить сами адреса: SOCKS5 → `ip:port`, MTProto → `tg://proxy?server=...&port=...&secret=...`
- [ ] `/get socks`, `/get mtproto`, `/get` — оба списка
- [ ] Обрезка под лимит Telegram 4096 символов

### 1.3 `async void` таймер
`Program.cs:133` — `new Timer(async _ => ...)` (исключения внутри теряются).

- [ ] Вынести в `ProxyRefreshService : BackgroundService` + `PeriodicTimer`
- [ ] Убрать статики `_proxyTimer`, `Timed`/`StopTimed`, `Interlocked`, `ManualResetEvent`
- [ ] Период обновления — в `AppSettings.json`, а не `FromHours(12)` в трёх местах

---

## 2. ShadowSocks — проверка и поддержка

### Что важно знать
- .NET `SocketsHttpHandler` **не умеет** `ss://` — поддерживаются только
  `http`, `https`, `socks4`, `socks4a`, `socks5`.
- Значит напрямую в `WebProxy("ss://...")` Shadowsocks не подключить.
- Варианты:
  1. **Локальный клиент** (`ss-local` / `v2ray` / `sing-box`) слушает `socks5://127.0.0.1:1080`
     → используем его как обычный SOCKS5-прокси. Самый надёжный путь.
  2. **Свой AEAD-клиент** (`aes-256-gcm`, `chacha20-ietf-poly1305`) — писать криптопротокол руками:
     IV/salt, HKDF, шифрование потока, UDP-relay. Долго и рискованно.

### Что нужно проверить, прежде чем кодить
- [ ] Отдаёт ли локальный API (`ALL` / `STATS`) Shadowsocks-конфиги? В какой форме:
      `ss://`-ссылка (SIP002, base64 `method:password@host:port`) или отдельные поля?
- [ ] Есть ли у прокси плагины (`obfs`, `v2ray-plugin`)? Если да — свой клиент почти обязателен
- [ ] Нужен ли UDP (для Telegram не критично, там TCP)

### План реализации (если ss-конфиги есть)
- [ ] Модель `ShadowsocksData` + парсер SIP002: base64 → `method:password@host:port#tag`
- [ ] Проверка доступности: тот же `AdditionalTestClient`, но через локальный `ss-local`
      (поднимать процесс/контейнер с конфигом на каждый прокси — либо один мультипортовый)
- [ ] Ветка в `MethodSubstitution.Methods`: для `ShadowsocksData` строить `socks5://127.0.0.1:<port>`
- [ ] `/get ss` — вывод `ss://`-ссылок пользователю
- [ ] Если API отдаёт только SOCKS5/MTProto — сначала прогнать `ALL` и посмотреть реальный формат

### Порядок
1. Сначала **только проверка** формата от API (без кода).
2. Если конфиги есть и без плагинов → локальный `ss-local` + переиспользовать SOCKS5-путь.
3. Свой AEAD — только если локальный клиент не вариант.

---

## 3. Мелочи / гигиена

### Мёртвый код
- [ ] `Socks5Additionaltest/AddtionalTestClass.cs:38-45` — ветки `Unauthorized`/`Found`/`MovedPermanently`
      внутри `if (IsSuccessStatusCode)` мертвы (401/301/302 не входят в 2xx)

### Стиль / чистота
- [ ] `ProxySubstitution/MethodSubstitution.cs:36` — токен читается из JSON внутри метода → инжектить `HJsonStruct`
- [ ] `Socks5Additionaltest/AddtionalTestClass.cs:30` — `ParseJson.Parse<HJsonStruct>()` на каждый вызов → инжектить `_settings`
- [ ] `Cache/CachedProxyList.cs:39,48,67` — логи через `$"..."` → structured (`LogInformation("...", args)`)
- [ ] `Cache/CachedProxyList.cs:48,71` — `LogError(ex.Message + ex.StackTrace)` → `LogError(ex, "...")`
- [ ] `Program.cs:24,29` — два `LoggerFactory` (`LoggerFactory.Create` + `AddLogging`), конфиг дублируется
- [ ] `DataModelJson/HJsonStruct.cs` — класс `ProxyBot` по смыслу = `TestProxyUrl` → переименовать
- [ ] `Cache/CachedProxyList.cs:29` — fresh TTL 12h при периоде 12h → окно stale (сделать 13h)

---

## 4. Идеи (после базы)

### Функционал
1. **Подписка на автообновление** — `/sub`, `/unsub`, хранение `chatId`, пуш после обновления кэша
   (логика для `/stop`, который сейчас — заглушка)
2. **`/stats`** — вывод `ProxyStats` из уже готового `GetStats`
3. **`/check`** — пинг прокси с локальной машины, топ-N по скорости
4. **Inline-кнопки пагинации** вместо простыни в одном сообщении
5. **`/status`** — uptime, размер кэша, состояние CircuitBreaker, время последнего обновления
6. **Экспорт** прокси как `.txt` / `.csv`-файлом
7. **Дельта при обновлении** — логировать «+12 / −3», а не просто счётчик

### Оптимизация
1. **Один generic `LocalApiClient`** вместо 4 почти одинаковых `Get*Request`
2. **Дедупликация + сортировка** при записи в кэш
3. **`IHostApplicationLifetime`** вместо ручного `CancellationTokenSource` в `Main`
4. **Статический `JsonSerializerOptions`** + `PropertyNameCaseInsensitive`
5. **Секреты в env** — `BOT_TOKEN` поверх JSON, `AppSettings.json` в `.gitignore`
6. **Health вместо логов** — `/status` читает состояние `CircuitBreaker` и кэша
