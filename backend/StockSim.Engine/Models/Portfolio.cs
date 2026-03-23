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
