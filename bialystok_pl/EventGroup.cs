namespace eventParser.bialystok_pl;

interface IEventGroup
{
    string Name { get; set; }
    string Url { get; set; }
}

class EventGroup : IEventGroup
{
    public required string Name { get; set; }
    public required string Url { get; set; }
}