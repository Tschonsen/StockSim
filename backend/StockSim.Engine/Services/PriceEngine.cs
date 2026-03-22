using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Core price simulation engine using Geometric Brownian Motion.
/// Calculates new prices for each stock per simulation tick.
/// See Bible section 5.2 for the full price model specification.
///
/// Price formula per tick:
///   NewPrice = OldPrice × (1 + Drift + Random + MeanReversion)
///
/// Where:
///   Drift = long-term trend (positive for growth stocks, near-zero for value)
///   Random = Volatility × NormalRandom × √TickDuration
///   MeanReversion = pull toward FairValue when price deviates significantly
/// </summary>
public class PriceEngine
{
    private readonly Random _rng;
    private readonly Logger _log = new("PriceEngine");

    // Normal distribution cache for Box-Muller transform
    private double? _spareNormal;

    public PriceEngine(int seed)
    {
        _rng = new Random(seed);
        _log.Info("PriceEngine initialized", new { seed });
    }

    /// <summary>
    /// Advance one stock's price by one tick.
    /// </summary>
    /// <param name="stock">The stock to update</param>
    /// <param name="tickDuration">Duration of one tick in game time</param>
    public void Tick(Stock stock, TimeSpan tickDuration)
    {
        var oldPrice = stock.CurrentPrice;
        if (oldPrice <= 0) return;

        var tickMinutes = tickDuration.TotalMinutes;
        var sqrtTick = Math.Sqrt(tickMinutes / 390.0); // Normalize to trading day (390 min)

        // 1. Drift component (long-term trend)
        var drift = CalculateDrift(stock) * (decimal)tickMinutes;

        // 2. Random walk (Geometric Brownian Motion)
        var randomComponent = (decimal)(NextNormal() * (double)stock.BaseVolatility * sqrtTick);

        // 3. Mean reversion toward fair value
        var meanReversion = CalculateMeanReversion(stock) * (decimal)tickMinutes;

        // Combine components
        var totalReturn = drift + randomComponent + meanReversion;

        // Apply to price (multiplicative)
        var newPrice = oldPrice * (1m + totalReturn);

        // Floor at $0.001 (never zero or negative)
        newPrice = Math.Max(newPrice, 0.001m);

        // Round to 2 decimal places
        stock.CurrentPrice = Math.Round(newPrice, 2);

        // Update bid/ask spread
        UpdateBidAsk(stock);

        // Update day high/low
        if (stock.CurrentPrice > stock.DayHigh)
            stock.DayHigh = stock.CurrentPrice;
        if (stock.CurrentPrice < stock.DayLow || stock.DayLow == 0)
            stock.DayLow = stock.CurrentPrice;

        // Generate tick volume
        UpdateVolume(stock, tickMinutes);

        _log.Debug("Price tick", new
        {
            symbol = stock.Symbol,
            oldPrice,
            newPrice = stock.CurrentPrice,
            drift,
            random = randomComponent,
            meanReversion,
            spread = stock.Spread
        });
    }

    /// <summary>
    /// Reset daily tracking values (called at market open).
    /// </summary>
    public void ResetDailyValues(Stock stock)
    {
        stock.PreviousClose = stock.CurrentPrice;
        stock.DayHigh = stock.CurrentPrice;
        stock.DayLow = stock.CurrentPrice;
        stock.DayVolume = 0;
    }

    private decimal CalculateDrift(Stock stock)
    {
        // Base drift from stock traits
        // Growth stocks: slight positive drift, Value stocks: near zero
        // Bible 5.2.1: Drift = long-term trend, +0.001% per tick for growth
        decimal baseDrift = 0.00001m; // ~2.5% annual at 390 ticks/day, 252 days/year

        if (stock.Traits.Contains("Growth Stock") || stock.Traits.Contains("Fast Grower"))
            baseDrift = 0.00003m;
        else if (stock.Traits.Contains("Slow Grower") || stock.Traits.Contains("Defensive"))
            baseDrift = 0.000005m;
        else if (stock.Traits.Contains("Speculative") || stock.Traits.Contains("Penny Stock"))
            baseDrift = 0m; // No clear trend

        return baseDrift;
    }

    private decimal CalculateMeanReversion(Stock stock)
    {
        if (stock.FairValue <= 0) return 0m;

        var deviation = (stock.CurrentPrice - stock.FairValue) / stock.FairValue;

        // Only apply mean reversion when price deviates >20% from fair value
        // Bible 5.2.1: Mean reversion strength 0.001-0.01% per tick
        if (Math.Abs(deviation) < 0.20m) return 0m;

        // Pull toward fair value: negative when overpriced, positive when underpriced
        var reversionStrength = 0.00005m;
        return -deviation * reversionStrength;
    }

    private void UpdateBidAsk(Stock stock)
    {
        // Bible 5.2.3: Spread = BaseSpread × VolatilityFactor × (1 / LiquidityFactor)
        var baseSpreadPercent = stock.LiquidityScore switch
        {
            >= 9 => 0.0001m,  // 0.01% for mega caps
            >= 7 => 0.0005m,  // 0.05%
            >= 5 => 0.001m,   // 0.10%
            >= 3 => 0.003m,   // 0.30%
            _ => 0.01m,       // 1.00% for micro caps
        };

        // Volatility widens spread
        var volFactor = 1m + stock.BaseVolatility * 10m;

        var halfSpread = stock.CurrentPrice * baseSpreadPercent * volFactor;
        halfSpread = Math.Max(halfSpread, 0.005m); // Minimum $0.005 half-spread

        stock.BidPrice = Math.Round(stock.CurrentPrice - halfSpread, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + halfSpread, 2);

        // Ensure bid is never negative
        stock.BidPrice = Math.Max(stock.BidPrice, 0.001m);
    }

    private void UpdateVolume(Stock stock, double tickMinutes)
    {
        // Generate realistic tick volume based on average daily volume
        // Bible 5.4: U-shaped intraday volume profile
        var dailyVolume = stock.AverageVolume > 0 ? stock.AverageVolume : 100_000;
        var tickFraction = tickMinutes / 390.0; // Fraction of trading day
        var baseTickVolume = (long)(dailyVolume * tickFraction);

        // Add randomness (±30%)
        var variation = 1.0 + (NextNormal() * 0.3);
        var tickVolume = (long)(baseTickVolume * Math.Max(variation, 0.1));
        tickVolume = Math.Max(tickVolume, 1);

        stock.DayVolume += tickVolume;
    }

    /// <summary>
    /// Generate normally distributed random number using Box-Muller transform.
    /// Returns values centered around 0 with standard deviation of 1.
    /// </summary>
    private double NextNormal()
    {
        if (_spareNormal.HasValue)
        {
            var spare = _spareNormal.Value;
            _spareNormal = null;
            return spare;
        }

        double u, v, s;
        do
        {
            u = _rng.NextDouble() * 2.0 - 1.0;
            v = _rng.NextDouble() * 2.0 - 1.0;
            s = u * u + v * v;
        } while (s >= 1.0 || s == 0.0);

        s = Math.Sqrt(-2.0 * Math.Log(s) / s);
        _spareNormal = v * s;
        return u * s;
    }
}
