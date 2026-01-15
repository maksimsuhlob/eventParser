using System;
using System.Collections.Generic;
using Telegram.Bot;
using System.Text;
using System.Threading.Tasks;
using eventParser.bialystokonline_pl;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using IEvent = eventParser.bialystok_pl.IEvent;

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

    public async Task SendEventsMessageFromBialystok(Dictionary<string, List<IEvent>> events)
    {
        foreach (var kvp in events)
        {
            for (var i = 0; i < kvp.Value.Count; i++)
            {
                var eventItem = kvp.Value[i];
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"📂 * {kvp.Key} * Wydarzenia na tydzień: {i + 1}/{kvp.Value.Count} *");

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

    public async Task SendEventsMessageFromBialystokOnline(List<IBOEvent> events)
    {
        foreach (var e in events)
        {
            var messageBuilder = new StringBuilder();

            messageBuilder.AppendLine($"⭐: {e.Title}");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"📅: {e.Date}");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"📍: {e.Place}");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("📄 Szczegóły", e.Url),
                    InlineKeyboardButton.WithUrl($"🔗 {e.Group}", e.GroupUrl)
                }
            });

            try
            {
                if (e.ImageUrl!="")
                {
                    await _botClient.SendPhoto(
                        photo: InputFile.FromUri(e.ImageUrl),
                        chatId: _chatId,
                        caption: messageBuilder.ToString(),
                        replyMarkup: keyboard
                    );
                }
                else
                {
                    await _botClient.SendMessage(
                        chatId: _chatId,
                        text: messageBuilder.ToString(),
                        replyMarkup: keyboard);
                }

                Console.WriteLine("✅ Сообщение успешно отправлено в Telegram");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при отправке сообщения: {ex.Message}");
            }
        }
    }
}