using StockSim.Engine.Models;

namespace StockSim.Engine.Services;

/// <summary>
/// Calculates comprehensive portfolio performance analytics.
/// Bible 20.2: Analytics-Tab with performance and risk metrics.
/// </summary>
public static class AnalyticsCalculator
{
    /// <summary>
    /// Calculate comprehensive portfolio analytics including trade statistics.
    /// </summary>
    public static PortfolioAnalytics Calculate(Portfolio portfolio, Func<string, decimal> getPrice, decimal startingCash, PlayerStats? stats = null)
    {
        var equity = portfolio.TotalEquity(getPrice);
        var totalReturn = equity - startingCash;
        var totalReturnPct = startingCash > 0 ? Math.Round(totalReturn / startingCash * 100, 2) : 0m;

        var totalTrades = portfolio.TradeCount;

        // Use PlayerStats for accurate trade data if available
        int winningTrades = 0, losingTrades = 0;
        decimal bestTradePnL = 0, worstTradePnL = 0;
        decimal avgWin = 0, avgLoss = 0;
        decimal maxDrawdown = 0;
        decimal profitFactor = 0;
        int maxConsecutiveWins = 0, maxConsecutiveLosses = 0;
        decimal sharpeRatio = 0;
        string bestTradeSymbol = "", worstTradeSymbol = "";

        if (stats != null)
        {
            winningTrades = stats.WinningTradeCount;
            losingTrades = stats.LosingTradeCount;
            maxConsecutiveWins = stats.MaxConsecutiveWins;
            maxConsecutiveLosses = stats.MaxConsecutiveLosses;
            maxDrawdown = Math.Round(stats.MaxDrawdownPercent, 2);
            bestTradePnL = stats.LargestSingleGain;
            worstTradePnL = stats.LargestSingleLoss;
            avgWin = winningTrades > 0 ? Math.Round(stats.TotalGainFromWins / winningTrades, 2) : 0;
            avgLoss = losingTrades > 0 ? Math.Round(stats.TotalLossFromLosses / losingTrades, 2) : 0;
            profitFactor = stats.TotalLossFromLosses > 0
                ? Math.Round(stats.TotalGainFromWins / stats.TotalLossFromLosses, 2)
                : stats.TotalGainFromWins > 0 ? 999.99m : 0;

            // Best/worst trade symbols
            if (stats.TradeHistory.Count > 0)
            {
                var best = stats.TradeHistory.MaxBy(t => t.PnL);
                var worst = stats.TradeHistory.MinBy(t => t.PnL);
                bestTradeSymbol = best?.Symbol ?? "";
                worstTradeSymbol = worst?.Symbol ?? "";
            }

            // Sharpe ratio approximation (annualized)
            if (stats.EquityHistory.Count > 1)
            {
                var returns = new List<decimal>();
                for (int i = 1; i < stats.EquityHistory.Count; i++)
                {
                    var prev = stats.EquityHistory[i - 1].Equity;
                    if (prev > 0)
                        returns.Add((stats.EquityHistory[i].Equity - prev) / prev);
                }
                if (returns.Count > 1)
                {
                    var avgReturn = returns.Average();
                    var variance = returns.Sum(r => (r - avgReturn) * (r - avgReturn)) / (returns.Count - 1);
                    var stdDev = (decimal)Math.Sqrt((double)variance);
                    if (stdDev > 0)
                        sharpeRatio = Math.Round(avgReturn / stdDev * (decimal)Math.Sqrt(252), 2);
                }
            }
        }
        else
        {
            // Fallback to basic calculation from orders
            winningTrades = portfolio.Orders
                .Where(o => o.IsFilled && o.Side == OrderSide.Sell && o.FillPrice.HasValue)
                .Count(o => o.FillPrice!.Value > GetAvgCostForOrder(portfolio, o));
            losingTrades = portfolio.Orders
                .Where(o => o.IsFilled && o.Side == OrderSide.Sell && o.FillPrice.HasValue)
                .Count(o => o.FillPrice!.Value <= GetAvgCostForOrder(portfolio, o));
        }

        var winRate = (winningTrades + losingTrades) > 0
            ? Math.Round((decimal)winningTrades / (winningTrades + losingTrades) * 100, 1)
            : 0m;

        // Sector allocation
        var sectorAllocation = portfolio.Positions.Values
            .GroupBy(p => p.Symbol) // Will be enriched with sector data in the caller
            .Select(g => new { Symbol = g.Key, Value = g.Sum(p => p.MarketValue(getPrice(p.Symbol))) })
            .ToList();

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
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            AvgTradeSize = totalTrades > 0
                ? Math.Round(portfolio.Orders.Where(o => o.IsFilled).DefaultIfEmpty().Average(o => (o?.Quantity ?? 0) * (o?.FillPrice ?? 0)), 2)
                : 0m,
            BestTradePnL = bestTradePnL,
            BestTradeSymbol = bestTradeSymbol,
            WorstTradePnL = worstTradePnL,
            WorstTradeSymbol = worstTradeSymbol,
            AvgWin = avgWin,
            AvgLoss = avgLoss,
            MaxDrawdownPercent = maxDrawdown,
            ProfitFactor = profitFactor,
            SharpeRatio = sharpeRatio,
            MaxConsecutiveWins = maxConsecutiveWins,
            MaxConsecutiveLosses = maxConsecutiveLosses,
        };
    }

    private static decimal GetAvgCostForOrder(Portfolio portfolio, Order order)
    {
        if (portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            return pos.AverageCost;
        return order.FillPrice ?? 0;
    }
}

/// <summary>Portfolio analytics data.</summary>
public class PortfolioAnalytics
{
    // Core
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

    // Trade Statistics
    public decimal WinRate { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal AvgTradeSize { get; set; }
    public decimal BestTradePnL { get; set; }
    public string BestTradeSymbol { get; set; } = "";
    public decimal WorstTradePnL { get; set; }
    public string WorstTradeSymbol { get; set; } = "";
    public decimal AvgWin { get; set; }
    public decimal AvgLoss { get; set; }

    // Risk Metrics
    public decimal MaxDrawdownPercent { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public int MaxConsecutiveWins { get; set; }
    public int MaxConsecutiveLosses { get; set; }
}
