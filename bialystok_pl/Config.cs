namespace eventParser.bialystok_pl;

static class HtmlSelectors
{
    public const string EventsItem = "//li[@class='events__item']";
    public const string EventLink = ".//a[contains(@class, 'events__link')]";
    public const string EventDate = ".//div[@class='events__date']";
    public const string EventPlace = ".//div[@class='events__place-n-time']";
}

static class UrlConstants
{
    public const string HostUrl = "https://www.bialystok.pl";
    public const string BaseUrl = "https://www.bialystok.pl/pl/dla_turystow/kalendarz-imprez/";
}