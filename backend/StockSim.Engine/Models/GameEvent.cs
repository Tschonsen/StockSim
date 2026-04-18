namespace StockSim.Engine.Models;

/// <summary>
/// Event type categories. See Spec 8.1.
/// </summary>
public enum EventType
{
    Macro,      // Affects entire market
    Sector,     // Affects all stocks in a sector
    Company,    // Affects a single stock
}

/// <summary>
/// Event severity level. See Spec 8.1.
/// </summary>
public enum EventSeverity
{
    Minor,
    Moderate,
    Major,
}

/// <summary>
/// Event tier controlling content complexity and difficulty gating.
/// Orthogonal to EventType and EventSeverity.
/// See AI_EVENT_SYSTEM.md section 1.2.
/// </summary>
public enum EventTier
{
    Tier1 = 1,  // Everyday events: earnings, upgrades, buybacks (all difficulties)
    Tier2 = 2,  // Notable events: FDA, fraud, activist investors (Normal+)
    Tier3 = 3,  // Crisis events: sector crash, bank run, commodity shock (Hard+)
    Tier4 = 4,  // Black swan events: multi-week mega arcs (Brutal + scenarios)
}

/// <summary>
/// A market event that affects prices and generates news.
/// See Spec 8.1 for event architecture.
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

    // --- Content depth (AI Event System Phase 1A) ---

    /// <summary>2-3 sentence context for the event</summary>
    public string? Summary { get; set; }

    /// <summary>Fictional analyst quote with assessment</summary>
    public string? AnalystQuote { get; set; }

    /// <summary>Analyst name (e.g. "Sarah Chen")</summary>
    public string? AnalystName { get; set; }

    /// <summary>Analyst firm (e.g. "Atlantic Research")</summary>
    public string? AnalystFirm { get; set; }

    /// <summary>Historical comparison (e.g. "Similar to 1973 Oil Embargo...")</summary>
    public string? HistoricalParallel { get; set; }

    /// <summary>Bullet points of what to watch</summary>
    public List<string>? WhatToWatch { get; set; }

    // --- Tier system ---

    /// <summary>Event tier (1-4), controls difficulty gating and content depth</summary>
    public EventTier Tier { get; set; } = EventTier.Tier1;

    /// <summary>Tags for categorization (e.g. "geopolitics", "energy", "supply-shock")</summary>
    public List<string>? Tags { get; set; }

    /// <summary>Season filter ("Q1"-"Q4", null = any)</summary>
    public string? Season { get; set; }

    /// <summary>Market phase filter ("bear", "bull", null = any)</summary>
    public string? RequiresPhase { get; set; }

    /// <summary>Market cap filter ("large", "small", null = any)</summary>
    public string? RequiresMarketCap { get; set; }

    // --- Differentiated impacts ---

    /// <summary>Per-sector impact breakdown (key = sector name)</summary>
    public Dictionary<string, SectorImpact>? SectorImpacts { get; set; }

    /// <summary>Per-company impact details</summary>
    public List<AffectedCompany>? DetailedImpacts { get; set; }

    // --- Follow-ups ---

    /// <summary>Possible follow-up scenarios with probabilities</summary>
    public List<FollowUpScenario>? PossibleOutcomes { get; set; }

    // --- Arc reference ---

    /// <summary>Arc this event belongs to (null if standalone)</summary>
    public string? ArcId { get; set; }

    /// <summary>Phase index within the arc</summary>
    public int? ArcPhaseIndex { get; set; }

    /// <summary>Path within the arc ("A", "B", "C", null if pre-branch)</summary>
    public string? ArcPath { get; set; }

    public GameEvent()
    {
        Id = _nextId++;
    }

    public static void ResetIdCounter() => _nextId = 1;
}
