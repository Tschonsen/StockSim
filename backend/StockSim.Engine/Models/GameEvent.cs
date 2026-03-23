namespace StockSim.Engine.Models;

/// <summary>
/// Event type categories. See Bible 8.1.
/// </summary>
public enum EventType
{
    Macro,      // Affects entire market
    Sector,     // Affects all stocks in a sector
    Company,    // Affects a single stock
}

/// <summary>
/// Event severity level. See Bible 8.1.
/// </summary>
public enum EventSeverity
{
    Minor,
    Moderate,
    Major,
}

/// <summary>
/// A market event that affects prices and generates news.
/// See Bible 8.1 for event architecture.
/// </summary>
public class GameEvent
{
    private static long _nextId = 1;

    public long Id { get; }
    public EventType Type { get; set; }
    public EventSeverity Severity { get; set; }

    /// <summary>Sentiment: -1.0 (very negative) to +1.0 (very positive)</summary>
    public float Sentiment { get; set; }

    /// <summary>News headline shown in the ticker</summary>
    public string Headline { get; set; } = "";

    /// <summary>Affected stock symbols (empty for Macro events)</summary>
    public List<string> AffectedSymbols { get; set; } = new();

    /// <summary>Affected sectors (empty for Company events)</summary>
    public List<string> AffectedSectors { get; set; } = new();

    /// <summary>Price effect as decimal (e.g. -0.05 = -5%)</summary>
    public float PriceEffect { get; set; }

    /// <summary>Volatility multiplier (e.g. 2.0 = double volatility)</summary>
    public float VolatilityMultiplier { get; set; } = 1.0f;

    /// <summary>Volume multiplier (e.g. 3.0 = triple volume)</summary>
    public float VolumeMultiplier { get; set; } = 1.0f;

    /// <summary>How many game-minutes the effect lasts</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Remaining minutes of effect</summary>
    public int RemainingMinutes { get; set; }

    /// <summary>Game time when this event was triggered</summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>Whether this event is still actively affecting prices</summary>
    public bool IsActive => RemainingMinutes > 0;

    public GameEvent()
    {
        Id = _nextId++;
    }

    public static void ResetIdCounter() => _nextId = 1;
}
