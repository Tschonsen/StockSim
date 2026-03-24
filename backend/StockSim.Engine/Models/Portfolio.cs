namespace StockSim.Engine.Models;

/// <summary>
/// Player's portfolio: cash, positions, and trading history.
/// See Bible 4.1 for account model and 6.1 for portfolio.
/// </summary>
public class Portfolio
{
    /// <summary>Available cash balance. Bible 4.1: starts with $25k-$100k.</summary>
    public decimal Cash { get; set; }

    /// <summary>Total realized P&L from all closed trades.</summary>
    public decimal RealizedPnL { get; set; }

    /// <summary>Total commissions paid.</summary>
    public decimal TotalCommissions { get; set; }

    /// <summary>Open positions by symbol.</summary>
    public Dictionary<string, Position> Positions { get; } = new();

    /// <summary>All orders (open + historical).</summary>
    public List<Order> Orders { get; } = new();

    /// <summary>Number of completed trades.</summary>
    public int TradeCount { get; set; }

    /// <summary>Price alerts. Bible 3.5.4: max 20.</summary>
    public List<PriceAlert> PriceAlerts { get; } = new();

    // Margin Trading (Bible 4.5)
    /// <summary>Whether margin trading is enabled for this account.</summary>
    public bool MarginEnabled { get; set; }
    /// <summary>Amount borrowed on margin. Margin balance = how much the player owes.</summary>
    public decimal MarginBalance { get; set; }
    /// <summary>Maximum leverage ratio (2:1 = can borrow up to 1x equity).</summary>
    public decimal MaxLeverage { get; set; } = 2.0m;
    /// <summary>Maintenance margin requirement (25% = must maintain 25% equity).</summary>
    public decimal MaintenanceMargin { get; set; } = 0.25m;

    /// <summary>Buying power = Cash + available margin.</summary>
    public decimal BuyingPower(Func<string, decimal> getPrice) =>
        MarginEnabled ? Cash + Math.Max(0, TotalEquity(getPrice) * (MaxLeverage - 1) - MarginBalance) : Cash;

    /// <summary>Current margin used percentage. >75% is danger zone, >100% = margin call.</summary>
    public decimal MarginUsedPercent(Func<string, decimal> getPrice)
    {
        var equity = TotalEquity(getPrice);
        if (equity <= 0 || !MarginEnabled || MarginBalance <= 0) return 0;
        return Math.Round(MarginBalance / equity * 100, 2);
    }

    public Portfolio(decimal startingCash)
    {
        Cash = startingCash;
    }

    /// <summary>
    /// Total market value of all open positions.
    /// Requires a price lookup function.
    /// </summary>
    public decimal PortfolioValue(Func<string, decimal> getPrice)
    {
        return Positions.Values.Sum(p => p.MarketValue(getPrice(p.Symbol)));
    }

    /// <summary>
    /// Total equity = Cash + Portfolio Value.
    /// </summary>
    public decimal TotalEquity(Func<string, decimal> getPrice)
    {
        return Cash + PortfolioValue(getPrice);
    }

    /// <summary>
    /// Total unrealized P&L across all positions.
    /// </summary>
    public decimal TotalUnrealizedPnL(Func<string, decimal> getPrice)
    {
        return Positions.Values.Sum(p => p.UnrealizedPnL(getPrice(p.Symbol)));
    }
}
