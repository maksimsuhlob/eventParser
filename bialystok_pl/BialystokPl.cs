using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace eventParser.bialystok_pl;

public class BialystokPl
{
    public async Task<Dictionary<string, List<IEvent>>> GetBialystokPlEvents()
    {
        var events = new Dictionary<string, List<IEvent>>();
        var doc = await GetPageAsync();
        var groups = GetGroups(doc);

        foreach (var group in groups)
        {
            // Console.WriteLine(group.Name);

            var allEvents = await GetEvents(group.Url);
            var todayEvents = allEvents.Where(item => DateParser.IsCurrentWeekInRange(item.Date)).ToList();
            events[group.Name.Trim()] = todayEvents;
            // todayEvents.ForEach(item => Console.WriteLine(item.ToString()));
        }

        return events;
    }

    static async Task<HtmlDocument> GetPageAsync(string? queryParams = null)
    {
        var url = UrlConstants.BaseUrl + queryParams;

        var http = new HttpClient();
        var html = await http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    static List<IEventGroup> GetGroups(HtmlDocument doc)
    {
        List<IEventGroup> eventGroups = [];

        foreach (var item in doc.DocumentNode.SelectNodes(HtmlSelectors.EventsItem))
        {
            var link = item.SelectSingleNode(HtmlSelectors.EventLink);
            var name = link.InnerText;
            var url = link.Attributes["href"].Value;
            eventGroups.Add(new EventGroup
            {
                Name = name,
                Url = url
            });
        }

        return eventGroups;
    }

    static async Task<List<IEvent>> GetEvents(string queryParams)
    {
        var currentYear = DateTime.Now.Year;
        var currentMonth = MonthDictiobary.GetCurrentMonthInPolish();

        List<IEvent> events = [];
        var yearsPage = await GetPageAsync(queryParams);
        var yearNodes = yearsPage.DocumentNode.SelectNodes(HtmlSelectors.EventsItem)
            ?.Where(item => item.SelectSingleNode(HtmlSelectors.EventLink)?.InnerText.Trim() == currentYear.ToString())
            .ToList();

        if (yearNodes != null && yearNodes.Count != 0)
        {
            var monthsPage =
                await GetPageAsync(yearNodes[0].SelectSingleNode(HtmlSelectors.EventLink)?.Attributes["href"].Value);

            var monthNodes = monthsPage.DocumentNode.SelectNodes(HtmlSelectors.EventsItem)
                ?.Where(item => item.SelectSingleNode(HtmlSelectors.EventLink)?.InnerText.Trim() == currentMonth)
                .ToList();

            if (monthNodes != null && monthNodes.Count != 0)
            {
                var eventsPageParams =
                    monthNodes[0].SelectSingleNode(HtmlSelectors.EventLink)?.Attributes["href"].Value;
                var eventsPage = await GetPageAsync(eventsPageParams);

                var eventNodes = eventsPage.DocumentNode.SelectNodes(HtmlSelectors.EventsItem);

                foreach (var item in eventNodes)
                {
                    var link = item.SelectSingleNode(HtmlSelectors.EventLink);
                    var dateNode = item.SelectSingleNode(HtmlSelectors.EventDate);
                    var placeNode = item.SelectSingleNode(HtmlSelectors.EventPlace);
                    var date = DateParser.ParseDateRange(dateNode.InnerText.Trim());
                    var title = link.InnerText.Trim();
                    var url = link.Attributes["href"].Value;
                    var place = placeNode.ChildNodes.First()?.ChildNodes.Last().InnerText.Trim();
                    var time = placeNode.ChildNodes.Last()?.ChildNodes.Last().InnerText.Trim();
                    events.Add(new Event
                    {
                        Date = date,
                        Title = title,
                        Url = UrlConstants.HostUrl + url,
                        Place = place ?? "",
                        Time = time ?? "",
                        OriginalLink = UrlConstants.BaseUrl + eventsPageParams
                    });
                }
            }
        }

        return events;
    }
}