using System;

namespace eventParser.bialystokonline_pl;

public interface IEvent
{
    (DateTime? StartDate, DateTime? EndDate) Date { get; set; }
    string Title { get; set; }
    string Url { get; set; }
    string Place { get; set; }
    string OriginalLink { get; set; }
    string Time { get; set; }
}
public interface IBOEvent
{
    string Date { get; set; }
    string Title { get; set; }
    string Url { get; set; }
    string Group { get; set; }
    string GroupUrl { get; set; }
    string Place { get; set; }
    string ImageUrl { get; set; }
}

class BOEvent : IBOEvent
{
    public required string Date { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string Group { get; set; }
    public required string GroupUrl { get; set; }
    public required string Place { get; set; }
    public required string ImageUrl { get; set; }

    public override string ToString()
    {
        return $"{Date}\n{Title}\n{Url}\n{Group}\n{GroupUrl}\n{Place}\n{ImageUrl}";
    }
}