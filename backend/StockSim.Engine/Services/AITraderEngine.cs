using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Simulates aggregate AI trader behavior that affects market dynamics.
/// Phase 1 MVP: Market Maker (liquidity/spreads) + Retail Trader (momentum/panic).
/// See Bible 7.1-7.2 for AI trader philosophy and types.
///
/// AI traders don't have individual portfolios or orders.
/// They affect the market through aggregate adjustments to:
///   - Spreads (Market Maker)
///   - Volume (all)
///   - Price pressure (Retail sentiment)
/// </summary>
public class AITraderEngine
{
    private readonly Random _rng;
    private readonly Logger _log = new("AITrader");

    // Retail sentiment tracks overall market mood (-1 to +1)
    private float _retailSentiment = 0f;

    public float RetailSentiment => _retailSentiment;

    public AITraderEngine(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// Called each market tick. Adjusts spreads, adds volume pressure, and
    /// applies retail sentiment to prices.
    /// </summary>
    public void Tick(IReadOnlyList<Stock> stocks, IReadOnlyList<GameEvent> activeEvents, bool isMarketOpen)
    {
        if (!isMarketOpen) return;

        // Update retail sentiment based on active events
        UpdateRetailSentiment(activeEvents);

        foreach (var stock in stocks)
        {
            // 1. Market Maker: adjust spreads based on volatility and liquidity
            ApplyMarketMaker(stock);

            // 2. Retail Trader: apply sentiment-driven volume and price pressure
            ApplyRetailPressure(stock);
        }
    }

    /// <summary>
    /// Market Maker AI: maintains bid/ask spreads and provides liquidity.
    /// Bible 7.2.1: Spread based on volatility and liquidity, tightens in calm markets.
    /// </summary>
    private void ApplyMarketMaker(Stock stock)
    {
        // Base spread from liquidity (Bible 7.2.1: spreadWidth 0.05-0.5%)
        var baseSpreadPct = stock.LiquidityScore switch
        {
            >= 9 => 0.0005m,  // 0.05% mega caps
            >= 7 => 0.001m,   // 0.10% large caps
            >= 5 => 0.002m,   // 0.20% mid caps
            >= 3 => 0.004m,   // 0.40% small caps
            _ => 0.008m,      // 0.80% micro caps
        };

        // Volatility widens spread (Market Makers pull back in volatile markets)
        var volMultiplier = 1m + stock.BaseVolatility * 5m;

        // Random micro-variation (market makers compete, spreads fluctuate)
        var noise = 1m + (decimal)(_rng.NextDouble() * 0.1 - 0.05); // ±5%

        var halfSpread = stock.CurrentPrice * baseSpreadPct * volMultiplier * noise;
        halfSpread = Math.Max(halfSpread, 0.005m); // Min $0.005

        stock.BidPrice = Math.Round(stock.CurrentPrice - halfSpread, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + halfSpread, 2);
        stock.BidPrice = Math.Max(stock.BidPrice, 0.01m);

        // Ensure ask is always strictly above bid (minimum $0.01 spread)
        if (stock.AskPrice <= stock.BidPrice)
            stock.AskPrice = stock.BidPrice + 0.01m;

        // Market Makers generate baseline volume (Bible 7.2.1)
        var mmVolume = (long)(stock.AverageVolume * (0.001 + _rng.NextDouble() * 0.002));
        stock.DayVolume += Math.Max(mmVolume, 1);
    }

    /// <summary>
    /// Retail Trader AI: applies crowd sentiment to prices.
    /// Bible 7.2.13: FOMO buying amplifies uptrends, panic selling amplifies downtrends.
    /// </summary>
    private void ApplyRetailPressure(Stock stock)
    {
        if (Math.Abs(_retailSentiment) < 0.05f) return; // No meaningful sentiment

        // Retail pressure: small per-tick price nudge in sentiment direction
        // Stronger for volatile/speculative stocks (retail loves them)
        var susceptibility = stock.BaseVolatility * 2m; // Volatile stocks move more
        if (stock.Traits.Contains("Speculative") || stock.Traits.Contains("Penny Stock"))
            susceptibility *= 2m;

        var pressurePerTick = (decimal)_retailSentiment * susceptibility * 0.0001m;

        // Apply with some randomness (not every stock reacts equally)
        if (_rng.NextDouble() < 0.3) // 30% of stocks affected per tick
        {
            stock.CurrentPrice += stock.CurrentPrice * pressurePerTick;
            stock.CurrentPrice = Math.Max(stock.CurrentPrice, 0.01m);
            stock.CurrentPrice = Math.Round(stock.CurrentPrice, 2);
        }

        // Retail adds volume during high sentiment (FOMO buying / panic selling)
        if (Math.Abs(_retailSentiment) > 0.2f)
        {
            var sentimentVolume = (long)(stock.AverageVolume * Math.Abs(_retailSentiment) * 0.005 * _rng.NextDouble());
            stock.DayVolume += Math.Max(sentimentVolume, 0);
        }
    }

    /// <summary>
    /// Update aggregate retail sentiment based on active events.
    /// Sentiment drifts toward 0 over time (mean reversion).
    /// Bible 7.2.13: Retail reacts strongly to news, follows trends.
    /// </summary>
    private void UpdateRetailSentiment(IReadOnlyList<GameEvent> activeEvents)
    {
        // Sentiment impact from active events
        float eventImpact = 0f;
        foreach (var evt in activeEvents)
        {
            // Stronger events have more impact on sentiment
            var weight = evt.Severity switch
            {
                EventSeverity.Major => 0.3f,
                EventSeverity.Moderate => 0.15f,
                EventSeverity.Minor => 0.05f,
                _ => 0.1f,
            };
            eventImpact += evt.Sentiment * weight;
        }

        // Apply event impact (smoothed)
        _retailSentiment += eventImpact * 0.01f;

        // Mean reversion: sentiment drifts toward 0
        _retailSentiment *= 0.999f;

        // Clamp to [-1, 1]
        _retailSentiment = Math.Clamp(_retailSentiment, -1f, 1f);
    }
}
