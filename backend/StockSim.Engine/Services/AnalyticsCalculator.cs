using StockSim.Engine.Models;

namespace StockSim.Engine.Services;

/// <summary>
/// Calculates portfolio performance analytics.
/// Bible 20.2: Analytics-Tab with performance and risk metrics.
/// </summary>
public static class AnalyticsCalculator
{
    /// <summary>
    /// Calculate comprehensive portfolio analytics.
    /// </summary>
    public static PortfolioAnalytics Calculate(Portfolio portfolio, Func<string, decimal> getPrice, decimal startingCash)
    {
        var equity = portfolio.TotalEquity(getPrice);
        var totalReturn = equity - startingCash;
        var totalReturnPct = startingCash > 0 ? Math.Round(totalReturn / startingCash * 100, 2) : 0m;

        var winningTrades = portfolio.Orders
            .Where(o => o.IsFilled && o.Side == OrderSide.Sell && o.FillPrice.HasValue)
            .Count(o => o.FillPrice!.Value > GetAvgCostForOrder(portfolio, o));

        var losingTrades = portfolio.Orders
            .Where(o => o.IsFilled && o.Side == OrderSide.Sell && o.FillPrice.HasValue)
            .Count(o => o.FillPrice!.Value <= GetAvgCostForOrder(portfolio, o));

        var totalTrades = portfolio.TradeCount;
        var winRate = totalTrades > 0 ? Math.Round((decimal)winningTrades / Math.Max(1, winningTrades + losingTrades) * 100, 1) : 0m;

        return new PortfolioAnalytics
        {
            TotalEquity = equity,
            Cash = portfolio.Cash,
            PortfolioValue = portfolio.PortfolioValue(getPrice),
            TotalReturn = totalReturn,
            TotalReturnPercent = totalReturnPct,
            RealizedPnL = portfolio.RealizedPnL,
            UnrealizedPnL = portfolio.TotalUnrealizedPnL(getPrice),
            TotalCommissions = portfolio.TotalCommissions,
            TotalTrades = totalTrades,
            OpenPositions = portfolio.Positions.Count,
            WinRate = winRate,
            AvgTradeSize = totalTrades > 0
                ? Math.Round(portfolio.Orders.Where(o => o.IsFilled).Average(o => o.Quantity * (o.FillPrice ?? 0)), 2)
                : 0m,
        };
    }

    private static decimal GetAvgCostForOrder(Portfolio portfolio, Order order)
    {
        // Approximate: use current position avg cost or fill price
        if (portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            return pos.AverageCost;
        return order.FillPrice ?? 0;
    }
}

/// <summary>Portfolio analytics data.</summary>
public class PortfolioAnalytics
{
    public decimal TotalEquity { get; set; }
    public decimal Cash { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal TotalCommissions { get; set; }
    public int TotalTrades { get; set; }
    public int OpenPositions { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgTradeSize { get; set; }
}
