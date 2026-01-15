using System;

namespace eventParser.bialystokonline_pl;

static class HtmlSelectors
{
    
    public const string Pagination = "//ul[@class='pag']";
    public const string PaginationLink = "//a[contains(@class, 'pag-a')]";
    public const string EventList = "//ul[contains(@class, 'list-pag')]";
    public const string EventItem = ".//li[contains(@class, 'event')]";
    public const string EventTitle = ".//a[contains(@class, 'event-title')]";
    public const string EventGroup = ".//a[contains(@class, 'event-c')]";
    public const string EventDate = ".//div[contains(@class, 'event-pill')]";
    public const string EventDesc = ".//div[contains(@class, 'event-desc')]";
}

static class UrlConstants
{
    public const string HostUrl = "https://www.bialystokonline.pl";

    public static readonly Func<string, string> BaseUrl = (string date) =>
        $"https://www.bialystokonline.pl/imprezy,data,{date}.html";
}