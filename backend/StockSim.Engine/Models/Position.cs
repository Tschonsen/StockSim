namespace StockSim.Engine.Models;

/// <summary>
/// Represents a held stock position (long only for Phase 1 MVP).
/// See Spec 4.1 and 6.1 for portfolio mechanics.
/// </summary>
public class Position
{
    public string Symbol { get; }

    /// <summary>Number of shares held. Negative = short position.</summary>
    public decimal Shares { get; set; }

    /// <summary>Whether this is a short position.</summary>
    public bool IsShort => Shares < 0;

    /// <summary>Average cost basis per share.</summary>
    public decimal AverageCost { get; set; }

    /// <summary>Total cost basis (Shares × AverageCost).</summary>
    public decimal TotalCost => Shares * AverageCost;

    /// <summary>Current market value given a price.</summary>
    public decimal MarketValue(decimal currentPrice) => Shares * currentPrice;

    /// <summary>Unrealized P&L given a price. For shorts: profit when price falls.</summary>
    public decimal UnrealizedPnL(decimal currentPrice) =>
        IsShort
            ? Math.Abs(Shares) * (AverageCost - currentPrice)  // Short: profit = (sell price - current price) * shares
            : MarketValue(currentPrice) - TotalCost;            // Long: profit = current value - cost

    /// <summary>Unrealized P&L as percentage (always relative to absolute cost basis).</summary>
    public decimal UnrealizedPnLPercent(decimal currentPrice) =>
        TotalCost != 0 ? Math.Round(UnrealizedPnL(currentPrice) / Math.Abs(TotalCost) * 100, 2) : 0m;

    public Position(string symbol, decimal shares, decimal averageCost)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Shares = shares;
        AverageCost = averageCost;
    }

    /// <summary>
    /// Add shares to this position (from a buy).
    /// Recalculates average cost basis.
    /// </summary>
    public void AddShares(decimal newShares, decimal pricePerShare)
    {
        var newTotalCost = TotalCost + (newShares * pricePerShare);
        Shares += newShares;
        AverageCost = Shares > 0 ? Math.Round(newTotalCost / Shares, 4) : 0m;
    }

    /// <summary>
    /// Remove shares from this position (from a sell).
    /// Average cost stays the same.
    /// Returns the realized P&L.
    /// </summary>
    public decimal RemoveShares(decimal sharesToRemove, decimal pricePerShare)
    {
        if (sharesToRemove > Shares)
            throw new InvalidOperationException($"Cannot sell {sharesToRemove} shares, only {Shares} held.");

        var realizedPnL = sharesToRemove * (pricePerShare - AverageCost);
        Shares -= sharesToRemove;

        if (Shares == 0)
            AverageCost = 0;

        return Math.Round(realizedPnL, 2);
    }

    public override string ToString() => $"{Symbol}: {Shares} shares @ ${AverageCost:F2}";
}
