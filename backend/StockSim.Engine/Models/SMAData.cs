namespace StockSim.Engine.Models;

/// <summary>
/// Violation types tracked by the SMA (StockSim Market Authority).
/// Bible 9.3: each has distinct detection criteria and score impact.
/// </summary>
public enum ViolationType
{
    InsiderTrading,    // 9.3.1: trade before event, profit > $1k
    PumpAndDump,       // 9.3.2: buy >10% volume, price +15%, sell in 5 days
    Spoofing,          // 9.3.3: cancel >80% of large orders within 10 min
    WashTrading,       // 9.3.4: buy+sell same stock within 5 min, 3+/day
    Cornering,         // 9.3.5: >20% of float + manipulation
    BearRaid,          // 9.3.7: large short + price drops >10% same day
}

/// <summary>
/// SMA regulatory status levels. Bible 9.2.
/// </summary>
public enum RegulatoryStatus
{
    Clear,              // 0-20: normal
    UnderReview,        // 21-40: SMA is watching
    UnderInvestigation, // 41-80: formal investigation
    EnforcementPending, // 81-100: charges incoming
}

/// <summary>
/// A single detected suspicious activity incident.
/// </summary>
public class SMAViolation
{
    public long Id { get; set; }
    public ViolationType Type { get; set; }
    public string Symbol { get; set; } = "";
    public DateTime DetectedAt { get; set; }
    public decimal EstimatedProfit { get; set; }
    public int ScoreImpact { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>
/// An active SMA investigation. Bible 9.4.2.
/// Duration: 30-60 game days, then acquittal (20%) or penalty (80%).
/// </summary>
public class SMAInvestigation
{
    public long Id { get; set; }
    public ViolationType Type { get; set; }
    public string Symbol { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public int DurationDays { get; set; }
    public int DaysElapsed { get; set; }
    public bool IsResolved { get; set; }
    public bool Acquitted { get; set; }
}

/// <summary>
/// A penalty imposed by the SMA. Bible 9.4.3-9.4.5.
/// </summary>
public class SMAPenalty
{
    public long Id { get; set; }
    public ViolationType Type { get; set; }
    public string Symbol { get; set; } = "";
    public DateTime ImposedAt { get; set; }
    public decimal FineAmount { get; set; }
    public int TradingBanDays { get; set; }
    public int MarginBanDays { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>
/// Trading restriction on a specific stock. Bible 9.4.2.
/// </summary>
public class TradingRestriction
{
    public string Symbol { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    /// <summary>If true, can only close existing positions (no new buys/shorts).</summary>
    public bool CloseOnly { get; set; }
}

/// <summary>
/// Complete SMA state for a player. Persisted with save games.
/// Bible 9.2: suspicion score 0-100, decay -1 per 5 clean days.
/// </summary>
public class SMAState
{
    /// <summary>Suspicion score 0-100. Not directly visible to player.</summary>
    public int SuspicionScore { get; set; }

    /// <summary>Game days without suspicious activity (for score decay).</summary>
    public int CleanDays { get; set; }

    /// <summary>All detected violations (history).</summary>
    public List<SMAViolation> Violations { get; set; } = new();

    /// <summary>Active and past investigations.</summary>
    public List<SMAInvestigation> Investigations { get; set; } = new();

    /// <summary>All penalties imposed.</summary>
    public List<SMAPenalty> Penalties { get; set; } = new();

    /// <summary>Active trading restrictions by symbol.</summary>
    public List<TradingRestriction> TradingRestrictions { get; set; } = new();

    /// <summary>Number of enforcement actions (3+ = account freeze). Bible 9.4.5.</summary>
    public int EnforcementActionCount { get; set; }

    /// <summary>Whether account has been frozen (game over scenario).</summary>
    public bool AccountFrozen { get; set; }

    /// <summary>Global trading ban expiry (null = no ban). Bible 9.4.4.</summary>
    public DateTime? TradingBanUntil { get; set; }

    /// <summary>Margin ban expiry (null = no ban). Bible 9.4.4.</summary>
    public DateTime? MarginBanUntil { get; set; }

    /// <summary>Max position size limit (0 = no limit). Bible 9.4.5.</summary>
    public decimal MaxPositionSizePercent { get; set; }

    /// <summary>Position size limit expiry.</summary>
    public DateTime? PositionSizeLimitUntil { get; set; }

    /// <summary>Short selling ban expiry. Bible 9.4.5.</summary>
    public DateTime? ShortSellingBanUntil { get; set; }

    /// <summary>Next violation ID counter.</summary>
    public long NextViolationId { get; set; } = 1;

    /// <summary>Next investigation ID counter.</summary>
    public long NextInvestigationId { get; set; } = 1;

    /// <summary>Next penalty ID counter.</summary>
    public long NextPenaltyId { get; set; } = 1;

    // -- Tracking data for detection algorithms --

    /// <summary>Recent player orders for pattern detection (rolling 5-day window).</summary>
    public List<SMAOrderRecord> RecentOrders { get; set; } = new();

    /// <summary>Recent cancellations for spoofing detection.</summary>
    public List<SMACancellationRecord> RecentCancellations { get; set; } = new();

    public RegulatoryStatus Status => SuspicionScore switch
    {
        <= 20 => RegulatoryStatus.Clear,
        <= 40 => RegulatoryStatus.UnderReview,
        <= 80 => RegulatoryStatus.UnderInvestigation,
        _ => RegulatoryStatus.EnforcementPending,
    };

    /// <summary>Whether the player can trade a given symbol right now.</summary>
    public bool CanTrade(string symbol, DateTime gameTime)
    {
        if (AccountFrozen) return false;
        if (TradingBanUntil.HasValue && gameTime < TradingBanUntil.Value) return false;
        var restriction = TradingRestrictions.Find(r => r.Symbol == symbol && r.ExpiresAt > gameTime);
        return restriction == null || !restriction.CloseOnly;
    }

    /// <summary>Whether the player can open NEW positions in a symbol.</summary>
    public bool CanOpenPosition(string symbol, DateTime gameTime)
    {
        if (!CanTrade(symbol, gameTime)) return false;
        var restriction = TradingRestrictions.Find(r => r.Symbol == symbol && r.ExpiresAt > gameTime);
        return restriction == null;
    }

    /// <summary>Whether short selling is currently banned.</summary>
    public bool IsShortSellingBanned(DateTime gameTime) =>
        ShortSellingBanUntil.HasValue && gameTime < ShortSellingBanUntil.Value;

    /// <summary>Whether margin is currently banned.</summary>
    public bool IsMarginBanned(DateTime gameTime) =>
        MarginBanUntil.HasValue && gameTime < MarginBanUntil.Value;
}

/// <summary>
/// Lightweight order record for SMA pattern detection.
/// </summary>
public class SMAOrderRecord
{
    public string Symbol { get; set; } = "";
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime Time { get; set; }
    public bool IsFilled { get; set; }
}

/// <summary>
/// Tracks cancelled orders for spoofing detection. Bible 9.3.3.
/// </summary>
public class SMACancellationRecord
{
    public string Symbol { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime CancelledAt { get; set; }
}
