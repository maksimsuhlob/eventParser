using System;

namespace eventParser.bialystok_pl;

public interface IEvent
{
    (DateTime? StartDate, DateTime? EndDate) Date { get; set; }
    string Title { get; set; }
    string Url { get; set; }
    string Place { get; set; }
    string OriginalLink { get; set; }
    string Time { get; set; }
}

class Event : IEvent
{
    public required (DateTime? StartDate, DateTime? EndDate) Date { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string Place { get; set; }
    public required string OriginalLink { get; set; }
    public required string Time { get; set; }

    public override string ToString()
    {
        return $"{Date} {Title} {Place} {Time} {Url}";
    }
}