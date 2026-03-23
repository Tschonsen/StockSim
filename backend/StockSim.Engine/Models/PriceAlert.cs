namespace StockSim.Engine.Models;

/// <summary>
/// A price alert set by the player. Bible 3.5.4.
/// Triggers once when the condition is met, then deactivates.
/// </summary>
public class PriceAlert
{
    private static long _nextId = 1;

    public long Id { get; }
    public string Symbol { get; }

    /// <summary>"above" or "below"</summary>
    public string Condition { get; }

    /// <summary>Target price that triggers the alert.</summary>
    public decimal TargetPrice { get; }

    /// <summary>Whether this alert is still active.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Whether this alert has been triggered.</summary>
    public bool Triggered { get; set; }

    public PriceAlert(string symbol, string condition, decimal targetPrice)
    {
        Id = _nextId++;
        Symbol = symbol;
        Condition = condition;
        TargetPrice = targetPrice;
    }

    public static void ResetIdCounter() => _nextId = 1;
}
