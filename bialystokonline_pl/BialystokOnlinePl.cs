using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Globalization;

namespace eventParser.bialystokonline_pl;

public class BialystokOnlinePl
{
    public async Task<List<IBOEvent>> GetBialystokOnlinePlEvents()
    {
        DateTime today = DateTime.Now;

        List<IBOEvent> events = [];
        var mainPageUrl = UrlConstants.BaseUrl(today.ToString("yyyy-MM-dd"));
        var mainPage = await GetPageAsync(mainPageUrl);

        var pages = GetPages(mainPage);
        foreach (var page in pages)
        {
            var eventsPage = await GetPageAsync(UrlConstants.HostUrl + page.Url);
            Console.WriteLine(UrlConstants.HostUrl + page.Url);
            var eventsFromPage = await GetEvents(eventsPage);
            events.AddRange(eventsFromPage);
        }

        return events;
    }

    static async Task<HtmlDocument> GetPageAsync(string url)
    {
        var http = new HttpClient();
        var html = await http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    static List<IPage> GetPages(HtmlDocument doc)
    {
        List<IPage> pages = [];
        var pagination = doc.DocumentNode.SelectNodes(HtmlSelectors.Pagination);
        if (pagination.Count > 0)
        {
            var links = doc.DocumentNode.SelectNodes(HtmlSelectors.PaginationLink);
            foreach (var link in links)
            {
                var classes = link.Attributes["class"].Value.Split(' ');
                List<String> pageModificator = ["pag-first", "pag-prev", "pag-next", "pag-last"];
                if (classes.Length > 1 && pageModificator.Contains(classes[1]))
                {
                    break;
                }

                pages.Add(new Page
                    {
                        PageNumber = link.InnerText,
                        Url = link.Attributes["href"].Value
                    }
                );
            }
        }

        return pages;
    }


    static async Task<List<IBOEvent>> GetEvents(HtmlDocument doc)
    {
        List<IBOEvent> events = [];
        var pageEventList = doc.DocumentNode.SelectNodes(HtmlSelectors.EventList);
        var pageEvents = pageEventList[0].SelectNodes(HtmlSelectors.EventItem);

        foreach (var pageEvent in pageEvents)
        {
            string imgUrl;
            string title;
            string eventUrl;
            string groupTitle;
            string groupUrl;
            string date;
            string description;
            DateTime? startDate;
            DateTime? endDate;
            try
            {
                var eventLeft =
                    pageEvent.ChildNodes.FirstOrDefault(node => node.Attributes["class"].Value.Contains("event-left"));
                var eventRight =
                    pageEvent.ChildNodes.First(node => node.Attributes["class"].Value.Contains("event-right"));
                imgUrl = eventLeft?.SelectSingleNode(".//img").GetAttributeValue("src","");
                title = eventRight.SelectSingleNode(HtmlSelectors.EventTitle).InnerText;
                eventUrl = eventRight.SelectSingleNode(HtmlSelectors.EventTitle).GetAttributeValue("href", "");
                groupTitle = eventRight.SelectSingleNode(HtmlSelectors.EventGroup).InnerText;
                groupUrl = eventRight.SelectSingleNode(HtmlSelectors.EventGroup).GetAttributeValue("href", "");
                date = Regex.Replace(
                    string.Join("",
                            eventRight.SelectSingleNode(HtmlSelectors.EventDate).ChildNodes
                                .Select(node => node.InnerText))
                        .Replace("\n", "").Trim(), @"\s+", " ");
                description = string.Join(", ",
                    eventRight.SelectSingleNode(HtmlSelectors.EventDesc).ChildNodes.Select(node => node.InnerText));

                (startDate, endDate) = DateParserPl.Parse(date, out var time);
                // Console.WriteLine(start);
                // Console.WriteLine(end);
                // Console.WriteLine(time);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            // Console.WriteLine(startDate.Value);
            // Console.WriteLine(endDate.Value);
            // Console.WriteLine((endDate.Value - startDate.Value).Days);
            if (startDate.HasValue && endDate.HasValue)
                if (( endDate.Value-startDate.Value ).Days > 7)
                    if (DateTime.Now.DayOfWeek != DayOfWeek.Monday)
                        continue;


            events.Add(new BOEvent
            {
                Date = date,
                Title = title,
                Url = UrlConstants.HostUrl + eventUrl,
                Place = description,
                ImageUrl = imgUrl ?? "",
                Group = groupTitle,
                GroupUrl = UrlConstants.HostUrl + "/" + groupUrl,
            });
        }


        return events;
    }
}

public static class DateParserPl
{
    private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sty"] = 1, ["stycznia"] = 1,
        ["lut"] = 2, ["lutego"] = 2,
        ["mar"] = 3, ["marca"] = 3,
        ["kwi"] = 4, ["kwietnia"] = 4,
        ["maj"] = 5, ["maja"] = 5,
        ["cze"] = 6, ["czerwca"] = 6,
        ["lip"] = 7, ["lipca"] = 7,
        ["sie"] = 8, ["sierpnia"] = 8,
        ["wrz"] = 9, ["września"] = 9, ["wrzesnia"] = 9,
        ["paź"] = 10, ["października"] = 10, ["paz"] = 10, ["pazdziernika"] = 10,
        ["lis"] = 11, ["listopada"] = 11,
        ["gru"] = 12, ["grudnia"] = 12,
    };

    // Удаляем шум: день недели, "godz.", лишние пробелы, множественные пробелы
    private static string Normalize(string s)
    {
        s = s.Replace("\u00A0", " "); // NBSP
        s = Regex.Replace(s, @"\s+", " ").Trim();

        // убрать "Piątek", "Sobota", и пр. (дни недели на польском)
        s = Regex.Replace(s,
            @"\b(poniedziałek|wtorek|środa|czwartek|piątek|sobota|niedziela|pon|wt|śr|czw|pt|sob|ndz)\b\.?",
            "", RegexOptions.IgnoreCase);

        // убрать "godz." и возможное "g." перед временем
        s = Regex.Replace(s, @"\b(godz\.?|g\.)\b", "", RegexOptions.IgnoreCase);

        // снова нормализуем пробелы
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private static bool TryParseOneDate(string token, out DateTime date, out TimeSpan? time)
    {
        date = default;
        time = null;

        // варианты:
        //  - 26 wrz 2025
        //  - 26 wrz
        //  - 26 wrz 2025 17:00
        //  - 26 wrz 17:00
        //  - 17:00 (только время — применим к уже разобранной дате снаружи)

        var parts = token.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        int? day = null;
        int? month = null;
        int? year = null;
        TimeSpan? parsedTime = null;

        foreach (var p in parts)
        {
            var pp = p.Trim();

            // время HH:mm
            if (Regex.IsMatch(pp, @"^\d{1,2}:\d{2}$"))
            {
                if (TimeSpan.TryParseExact(pp, "h\\:mm", CultureInfo.InvariantCulture, out var ts) ||
                    TimeSpan.TryParseExact(pp, "hh\\:mm", CultureInfo.InvariantCulture, out ts))
                {
                    parsedTime = ts;
                    continue;
                }
            }

            // год
            if (int.TryParse(pp, out var n) && pp.Length == 4)
            {
                if (n is >= 1900 and <= 2100) year = n;
                continue;
            }

            // день
            if (int.TryParse(pp, out n) && pp.Length <= 2)
            {
                if (n is >= 1 and <= 31)
                {
                    day = n;
                    continue;
                }
            }

            // месяц
            var key = pp.ToLowerInvariant();
            if (MonthMap.TryGetValue(key, out var m))
            {
                month = m;
                continue;
            }
        }

        if (day is null || month is null)
            return false;

        var y = year ?? DateTime.Now.Year;
        // ТРЕБОВАНИЕ: если год не указан — использовать текущий год без переноса на следующий
        // (ранее здесь происходил автосдвиг на следующий год для прошедших дат)
        var d = Math.Min(day.Value, DateTime.DaysInMonth(y, month.Value));
        date = new DateTime(y, month.Value, d, 0, 0, 0, DateTimeKind.Local);
        time = parsedTime;
        return true;
    }

    // Главная функция: возвращает (StartDate, EndDate) и отдельно строку времени (если есть) через out
    public static (DateTime? StartDate, DateTime? EndDate) Parse(string input, out string timeText)
    {
        timeText = "";
        if (string.IsNullOrWhiteSpace(input))
            return (null, null);

        var s = Normalize(input);

        // Сначала пробуем распознать явные диапазоны:
        // 1) "26 maj - 09 gru 2025"
        var rangeDash = Regex.Split(s, @"\s-\s");
        if (rangeDash.Length == 2)
        {
            if (TryParseOneDate(rangeDash[0], out var d1, out var t1) &&
                TryParseOneDate(rangeDash[1], out var d2, out var t2))
            {
                timeText = (t1 ?? t2)?.ToString() ?? "";
                return (d1, d2);
            }
        }

        // 2) Две даты подряд с пробелом: "24 lip 2025 06 sie 2026"
        // Разобьём строку на две логические даты, пытаясь найти позицию второго дня (числа 1..31)
        var mDays = Regex.Matches(s, @"\b([12]?\d|3[01])\b");
        if (mDays.Count >= 2)
        {
            // попытаемся разделить по позиции второго совпадения
            var secondIndex = mDays[1].Index;
            var left = s[..secondIndex].Trim();
            var right = s[secondIndex..].Trim();

            if (TryParseOneDate(left, out var d1, out var t1) &&
                TryParseOneDate(right, out var d2, out var t2))
            {
                timeText = (t1 ?? t2)?.ToString() ?? "";
                return (d1, d2);
            }
        }

        // 3) Обычная одиночная дата, возможно с временем: "26 wrz 2025 Piątek godz. 17:00"
        if (TryParseOneDate(s, out var d, out var t))
        {
            timeText = t?.ToString() ?? "";
            return (d, d);
        }

        // 4) Если осталось только время без даты — вернём текущую дату
        var onlyTimeMatch = Regex.Match(s, @"^\d{1,2}:\d{2}$");
        if (onlyTimeMatch.Success)
        {
            if (TimeSpan.TryParseExact(onlyTimeMatch.Value, "h\\:mm", CultureInfo.InvariantCulture, out var ts) ||
                TimeSpan.TryParseExact(onlyTimeMatch.Value, "hh\\:mm", CultureInfo.InvariantCulture, out ts))
            {
                var baseDate = DateTime.Now.Date + ts;
                timeText = ts.ToString();
                return (baseDate, baseDate);
            }
        }

        return (null, null);
    }
}