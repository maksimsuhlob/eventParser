using eventParser.bialystok_pl;
using eventParser.bialystokonline_pl;
using Microsoft.Extensions.Configuration;

namespace eventParser;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables() // Переменные окружения имеют приоритет над appsettings.json
            .Build();

        string botToken = configuration["Telegram:BotToken"]
                          ?? throw new InvalidOperationException("Telegram:BotToken не найден в конфигурации");

        string chatId = configuration["Telegram:ChatId"]
                        ?? throw new InvalidOperationException("Telegram:ChatId не найден в конфигурации");


        BialystokOnlinePl bialystokonlinePl = new();
        var events = await bialystokonlinePl.GetBialystokOnlinePlEvents();
// Console.WriteLine(events.Count);
        var telegramBot = new TelegramBot(botToken, chatId);
        await telegramBot.SendEventsMessageFromBialystokOnline(events);
    }
}