namespace RankCalculator.Models;

public sealed class RankCalculatedEvent
{
    public string EventType => "RankCalculated";
    public string TextId { get; set; } = string.Empty;
    public double Rank { get; set; }
}
