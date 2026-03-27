namespace StockSim.Engine.Models;

/// <summary>
/// Differentiated impact on a specific sector from an event.
/// Replaces flat PriceEffect for events that affect sectors differently.
/// </summary>
public class SectorImpact
{
    /// <summary>Sector name (e.g. "Technology", "Energy")</summary>
    public string Sector { get; set; } = "";

    /// <summary>Price effect as decimal (e.g. -0.05 = -5%)</summary>
    public float PriceEffect { get; set; }

    /// <summary>Volatility multiplier (default 1.0 = no change)</summary>
    public float VolatilityMultiplier { get; set; } = 1.0f;

    /// <summary>Volume multiplier (default 1.0 = no change)</summary>
    public float VolumeMultiplier { get; set; } = 1.0f;

    /// <summary>Why this sector is affected (e.g. "Supply chain dependency")</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Specific company affected by an event with individualized impact.
/// Used for targeted effects within sector-wide or macro events.
/// </summary>
public class AffectedCompany
{
    /// <summary>Stock symbol</summary>
    public string Symbol { get; set; } = "";

    /// <summary>Price effect as decimal</summary>
    public float PriceEffect { get; set; }

    /// <summary>Role in the event: "primary", "supplier", "competitor", "beneficiary"</summary>
    public string? Role { get; set; }

    /// <summary>Explanation (e.g. "Direct competitor gains market share")</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// A possible follow-up outcome from an event, with probability and conditions.
/// Data-driven replacement for the hardcoded cascade system.
/// At runtime, the engine uses these to construct PendingFollowUp instances.
/// </summary>
public class FollowUpScenario
{
    /// <summary>Template identifier</summary>
    public string Id { get; set; } = "";

    /// <summary>Headline for the follow-up event</summary>
    public string Headline { get; set; } = "";

    /// <summary>2-3 sentence context</summary>
    public string? Summary { get; set; }

    /// <summary>Chance this follow-up fires (0.0-1.0)</summary>
    public float Probability { get; set; } = 0.5f;

    /// <summary>Minimum delay in game days before follow-up</summary>
    public int DelayMinDays { get; set; } = 1;

    /// <summary>Maximum delay in game days before follow-up</summary>
    public int DelayMaxDays { get; set; } = 5;

    /// <summary>Price effect of follow-up event</summary>
    public float PriceEffect { get; set; }

    /// <summary>Severity of the follow-up</summary>
    public EventSeverity Severity { get; set; } = EventSeverity.Minor;

    /// <summary>Sentiment (-1.0 to +1.0)</summary>
    public float Sentiment { get; set; }

    /// <summary>Only fire in this market phase ("bull", "bear", null = any)</summary>
    public string? RequiresMarketPhase { get; set; }

    /// <summary>Tags for categorization</summary>
    public List<string>? Tags { get; set; }
}
