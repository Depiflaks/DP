namespace Sharding;

public static class Countries
{
    public static IReadOnlyDictionary<string, string> Regions { get; } =
        new Dictionary<string, string>
        {
            ["Russia"] = "RU",
            ["France"] = "EU",
            ["Germany"] = "EU",
            ["UAE"] = "ASIA",
            ["India"] = "ASIA"
        }.AsReadOnly();
}
