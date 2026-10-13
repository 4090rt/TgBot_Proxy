using System.Collections.Concurrent;
using System.Text.Json;
using Telegram.Bot.Types;
using TgBot_Proxy.DataModelJson;

namespace TgBot_Proxy.AppSettingsParse
{
    public static class ParseJson
    {
        public const string FileName = "AppSettings.json";
        private static readonly ConcurrentDictionary<Type, object> _cache = new ConcurrentDictionary<Type, object>();

        public static T Parse<T>() where T : new ()
        {
                if (_cache.TryGetValue(typeof(T), out var cached) && cached != null)
                    return (T)cached;

                var path = ResolvePath();

                T result;
                try
                {
                    var readed = System.IO.File.ReadAllText(path);
                    result = JsonSerializer.Deserialize<T>(readed)
                        ?? throw new InvalidDataException($"{FileName} не содержит {typeof(T).Name}");
                }
                catch (Exception e)
                {
                    if (_cache.TryGetValue(typeof(T), out var old) && old is T oldVal)
                        return oldVal;

                    throw new InvalidDataException($"Не удалось прочитать/десериализовать {path}", e);
                }
                _cache.TryAdd(typeof(T), result);
                return result;
        }
        public static void ResetCache() => _cache.Clear();

        private static string ResolvePath()
        {
            var fromOutput = Path.Combine(AppContext.BaseDirectory, FileName);

            return File.Exists(fromOutput)
                ? fromOutput
                : Path.Combine(Directory.GetCurrentDirectory(), FileName);
        }
    }
}
