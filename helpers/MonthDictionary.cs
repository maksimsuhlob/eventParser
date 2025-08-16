static class MonthDictiobary
{
    private const string January = "styczeń";
    private const string February = "luty";
    private const string March = "marzec";
    private const string April = "kwiecień";
    private const string May = "maj";
    private const string June = "czerwiec";
    private const string July = "lipiec";
    private const string August = "sierpień";
    private const string September = "wrzesień";
    private const string October = "październik";
    private const string November = "listopad";
    private const string December = "grudzień";

    private static readonly Dictionary<int, string> PolishMonths = new()
    {
        { 1, January },
        { 2, February },
        { 3, March },
        { 4, April },
        { 5, May },
        { 6, June },
        { 7, July },
        { 8, August },
        { 9, September },
        { 10, October },
        { 11, November },
        { 12, December }
    };

    public static string GetCurrentMonthInPolish()
    {
        return PolishMonths[DateTime.Now.Month];
    }
}