namespace eventParser.bialystokonline_pl;

interface IPage
{
    string PageNumber { get; set; }
    string Url { get; set; }
}
class Page : IPage
{
    public required string PageNumber { get; set; }
    public required string Url { get; set; }
}