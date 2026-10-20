namespace Valuator.Messaging;

public sealed class SimilarityCalculatedEvent
{
    public string EventType => "SimilarityCalculated";
    public string TextId { get; set; } = string.Empty;
    public double Similarity { get; set; }
}
