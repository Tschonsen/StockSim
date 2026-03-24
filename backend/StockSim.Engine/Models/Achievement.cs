namespace StockSim.Engine.Models;

/// <summary>
/// Achievement categories from Bible 1.4.1.
/// </summary>
public enum AchievementCategory
{
    Wealth,
    Trading,
    Market,
    Risk,
}

/// <summary>
/// A single achievement definition + unlock state.
/// </summary>
public class Achievement
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public AchievementCategory Category { get; }
    public bool Unlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }

    public Achievement(string id, string name, string description, AchievementCategory category)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
    }
}

/// <summary>
/// Tracks stats needed for achievement checks.
/// Persisted with save games.
/// </summary>
public class PlayerStats
{
    public int ConsecutiveWins { get; set; }
    public int ConsecutiveLosses { get; set; }
    public int MaxConsecutiveWins { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public int TradesInCurrentDay { get; set; }
    public DateTime CurrentTradeDay { get; set; }
    public decimal LargestSingleGain { get; set; }
    public decimal LargestSingleLoss { get; set; }
    public decimal TotalGainFromWins { get; set; }
    public decimal TotalLossFromLosses { get; set; }
    public int WinningTradeCount { get; set; }
    public int LosingTradeCount { get; set; }
    public bool SurvivedFlashCrash { get; set; }
    public bool ExperiencedBullMarket { get; set; }
    public bool ExperiencedBearMarket { get; set; }
    public int DaysPlayed { get; set; }
    public decimal MaxPortfolioValue { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public decimal ShortSellingPnL { get; set; }
    public int EarningsCorrectPredictions { get; set; }
    public Dictionary<string, decimal> SectorPnL { get; set; } = new();

    /// <summary>Equity snapshots for performance chart. Key = game date, Value = total equity.</summary>
    public List<EquitySnapshot> EquityHistory { get; set; } = new();

    /// <summary>Closed trade records for journal / analytics.</summary>
    public List<TradeRecord> TradeHistory { get; set; } = new();
}

/// <summary>
/// Single equity snapshot for performance chart.
/// </summary>
public class EquitySnapshot
{
    public long Time { get; set; }  // Unix timestamp
    public decimal Equity { get; set; }
    public decimal Cash { get; set; }
    public decimal MarketIndex { get; set; }  // Average market change % from game start
}

/// <summary>
/// Record of a completed (closed) trade for trading journal.
/// </summary>
public class TradeRecord
{
    public long Id { get; set; }
    public string Symbol { get; set; } = "";
    public string Sector { get; set; } = "";
    public string Side { get; set; } = "";  // "Long" or "Short"
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPercent { get; set; }
    public decimal Commission { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime ExitTime { get; set; }
    public int HoldingDays { get; set; }
}
