using Telegram.Bot;
using System.Text;
using eventParser.bialystok_pl;
using Telegram.Bot.Types.ReplyMarkups;

namespace eventParser;

public class TelegramBot
{
    private readonly TelegramBotClient _botClient;
    private readonly string _chatId;

    public TelegramBot(string botToken, string chatId)
    {
        _botClient = new TelegramBotClient(botToken);
        _chatId = chatId;
    }

    public async Task SendEventsMessage(Dictionary<string, List<IEvent>> events)
    {
        foreach (var kvp in events)
        {
            for (var i=0;i< kvp.Value.Count; i++)
            {
                var eventItem = kvp.Value[i];
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"📂 * {kvp.Key} * Wydarzenia na tydzień: {i+1}/{kvp.Value.Count} *");
                
                messageBuilder.AppendLine(
                    $"📅: {DateParser.FormatDateRange(eventItem.Date.StartDate, eventItem.Date.EndDate)}");
                if (!string.IsNullOrEmpty(eventItem.Time))
                {
                    messageBuilder.AppendLine($"🕐: {eventItem.Time}");
                }
                messageBuilder.AppendLine($"📍: {eventItem.Place}");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"{eventItem.Title}");
                

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("📄 Szczegóły", eventItem.Url),
                        InlineKeyboardButton.WithUrl("🔗 Białystok.pl", eventItem.OriginalLink)
                    }
                });
                messageBuilder.AppendLine();
            
                try
                {
                    await _botClient.SendMessage(
                        chatId: _chatId,
                        text: messageBuilder.ToString(),
                        replyMarkup: keyboard
                    );
                    Console.WriteLine("✅ Сообщение успешно отправлено в Telegram");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка при отправке сообщения: {ex.Message}");
                }
            }
        }
    }

}