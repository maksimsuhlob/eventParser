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
                        .Replace("\n", "/").Trim(), @"\s+", " ");
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
            
            // Console.WriteLine(date);
            // Console.WriteLine(new BOEvent
            // {
            //     Date = date,
            //     Title = title,
            //     Url = UrlConstants.HostUrl + eventUrl,
            //     Place = description,
            //     ImageUrl = imgUrl ?? "",
            //     Group = groupTitle,
            //     GroupUrl = UrlConstants.HostUrl + "/" + groupUrl,
            // }.ToString());

            events.Add(new BOEvent
            {
                Date = date.Replace("/", ""),
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

    private static readonly HashSet<string> WeekDays = new(StringComparer.OrdinalIgnoreCase)
    {
        "poniedziałek","wtorek","środa","czwartek","piątek","sobota","niedziela",
        "pon","wt","śr","czw","pt","sob","ndz"
    };

    // Нормализуем, но сохраняем разделение "/" как требование задачи
    private static string[] TokenizeBySlashes(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
        s = s.Replace("\u00A0", " "); // NBSP
        // Удаляем лишние пробелы вокруг слэшей и схлопываем повторяющиеся слэши
        s = Regex.Replace(s, @"\s*/\s*", "/");
        s = Regex.Replace(s, @"/{2,}", "/");
        // Удаляем ведущие/замыкающие слэши
        s = s.Trim('/');
        // Разбиваем
        var raw = s.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Удаляем пустые и схлопываем пробелы
        return raw.Select(x => Regex.Replace(x, @"\s+", " ").Trim())
                  .Where(x => !string.IsNullOrWhiteSpace(x))
                  .ToArray();
    }

    private static bool IsWeekDay(string s) => WeekDays.Contains(s.Trim().TrimEnd('.'));

    private static bool TryParseTime(string s, out TimeSpan ts)
    {
        ts = default;
        s = s.Trim();
        // убрать "godz." / "g."
        s = Regex.Replace(s, @"\b(godz\.?|g\.)\b", "", RegexOptions.IgnoreCase).Trim();
        return TimeSpan.TryParseExact(s, new[] { "H\\:mm", "HH\\:mm" }, CultureInfo.InvariantCulture, out ts);
    }

    private static bool TryParseDayMonthYear(string text, out int day, out int month, out int? year)
    {
        day = 0; month = 0; year = null;
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Возможные формы: "27", "27 wrz", "27 wrz 2025", "wrz 2025", "2025"
        int? d = null;
        int? m = null;
        int? y = null;

        foreach (var p in parts)
        {
            var t = p.Trim();
            if (int.TryParse(t, out var n))
            {
                if (t.Length == 4 && n is >= 1900 and <= 2100)
                {
                    y = n;
                }
                else if (n is >= 1 and <= 31 && d is null)
                {
                    d = n;
                }
                continue;
            }

            var key = t.ToLowerInvariant();
            if (MonthMap.TryGetValue(key, out var mm))
            {
                m = mm;
            }
        }

        if (d.HasValue) day = d.Value;
        if (m.HasValue) month = m.Value;
        year = y;
        return d.HasValue || m.HasValue || y.HasValue;
    }

    private static bool TryBuildDate(int? day, int? month, int? year, out DateTime date)
    {
        date = default;
        if (day is null || month is null) return false;
        var y = year ?? DateTime.Now.Year;
        var d = Math.Min(day.Value, DateTime.DaysInMonth(y, month.Value));
        date = new DateTime(y, month.Value, d, 0, 0, 0, DateTimeKind.Local);
        return true;
    }

    private static bool TryParseSingleDateToken(string token, int? inheritedDay, int? inheritedMonth, int? inheritedYear, out DateTime date, out int? outDay, out int? outMonth, out int? outYear)
    {
        date = default;
        outDay = inheritedDay;
        outMonth = inheritedMonth;
        outYear = inheritedYear;

        // Удаляем дни недели
        var cleaned = string.Join(' ', token.Split(' ').Where(p => !IsWeekDay(p)));

        // Пробуем извлечь время, но дата составляется отдельно
        if (TryParseDayMonthYear(cleaned, out var d, out var m, out var y))
        {
            if (d != 0) outDay = d;
            if (m != 0) outMonth = m;
            if (y.HasValue) outYear = y;

            if (TryBuildDate(outDay, outMonth, outYear, out var built))
            {
                date = built;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseRangeToken(string token, int? inheritedYear, out DateTime start, out DateTime end, out int? outYear)
    {
        start = default; end = default; outYear = inheritedYear;

        // Формы диапазона внутри одного токена:
        // "01 wrz - 31 paź", "09 - 30 wrz", "09 - 30"
        var parts = Regex.Split(token, @"\s*-\s*");
        if (parts.Length != 2) return false;

        // Левая и правая части могут быть неполными, год можем взять из правой, месяца — из той части, где он указан
        int? lDay = null, lMonth = null, lYear = inheritedYear;
        int? rDay = null, rMonth = null, rYear = inheritedYear;

        TryParseDayMonthYear(parts[0], out var ld, out var lm, out var ly);
        TryParseDayMonthYear(parts[1], out var rd, out var rm, out var ry);

        if (ld != 0) lDay = ld;
        if (lm != 0) lMonth = lm;
        if (ly.HasValue) lYear = ly;

        if (rd != 0) rDay = rd;
        if (rm != 0) rMonth = rm;
        if (ry.HasValue) rYear = ry;

        // Наследование месяца/года между частями
        if (lMonth is null && rMonth is not null) lMonth = rMonth;
        if (rMonth is null && lMonth is not null) rMonth = lMonth;

        if (lYear is null && rYear is not null) lYear = rYear;
        if (rYear is null && lYear is not null) rYear = lYear;

        outYear = rYear ?? lYear ?? inheritedYear;

        if (TryBuildDate(lDay, lMonth, lYear, out var s) && TryBuildDate(rDay, rMonth, rYear, out var e))
        {
            start = s; end = e; return true;
        }

        return false;
    }

    public static (DateTime? StartDate, DateTime? EndDate) Parse(string input, out string timeText)
    {
        timeText = "";
        if (string.IsNullOrWhiteSpace(input)) return (null, null);

        var tokens = TokenizeBySlashes(input);
        if (tokens.Length == 0) return (null, null);

        DateTime? start = null;
        DateTime? end = null;
        TimeSpan? time = null;

        int? curDay = null;
        int? curMonth = null;
        int? curYear = null;

        foreach (var raw in tokens)
        {
            var t = raw;

            // 1) Если это чисто время или содержит "godz."
            if (TryParseTime(t, out var ts))
            {
                time = ts;
                continue;
            }

            // 2) Если это день недели — пропускаем
            if (IsWeekDay(t)) continue;

            // 3) Попытка распарсить диапазон внутри токена
            if (TryParseRangeToken(t, curYear, out var s, out var e, out var newYear))
            {
                start ??= s;
                end = e; // последний диапазон обновляет конец
                curYear = newYear ?? curYear;
                // Обновим текущие D/M для наследования после диапазона
                curDay = e.Day;
                curMonth = e.Month;
                continue;
            }

            // 4) Одиночная дата/фрагмент даты
            if (TryParseSingleDateToken(t, curDay, curMonth, curYear, out var d, out var nd, out var nm, out var ny))
            {
                if (start is null)
                {
                    start = d;
                }
                else
                {
                    // если start уже есть и нет end, а новая дата позже/другая — считаем это end
                    if (end is null)
                        end = d;
                    else
                        end = d; // при повторных датах берём последнюю как конец
                }

                curDay = nd;
                curMonth = nm;
                curYear = ny ?? curYear;
                continue;
            }

            // 5) Если токен содержит год/месяц без дня — обновим контекст года/месяца
            if (TryParseDayMonthYear(t, out var onlyD, out var onlyM, out var onlyY))
            {
                if (onlyY.HasValue) curYear = onlyY.Value;
                if (onlyM != 0) curMonth = onlyM;
                if (onlyD != 0) curDay = onlyD;
            }
        }

        // Если времени нет, но внутри исходной строки было "godz. XX:YY" — уже обработано.
        timeText = time?.ToString() ?? "";

        // Если есть только start без end — это одиночная дата
        if (start.HasValue && !end.HasValue) end = start;

        return (start, end);
    }
}