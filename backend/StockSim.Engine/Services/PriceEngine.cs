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

    /// <summary>Realized volatility tracker per stock (GARCH-lite: yesterday's vol affects today's).</summary>
    private readonly Dictionary<string, double> _realizedVol = new();

    /// <summary>Current market stress level (0=calm, 1=crisis). Affects correlation and spreads.</summary>
    public double MarketStress { get; set; }

    /// <summary>
    /// Bible 5.6: Sector correlation. Each sector gets a shared random shock per tick.
    /// Base correlation 45%, rises to 85% during crisis.
    /// </summary>
    private readonly Dictionary<string, double> _sectorShocks = new();
    private const double BaseSectorCorrelation = 0.45;
    private const double CrisisSectorCorrelation = 0.85;

    public PriceEngine(int seed)
    {
        _rng = new Random(seed);
        _log.Info("PriceEngine initialized", new { seed });
    }

    /// <summary>
    /// Generate sector-level shocks for this tick. Call once before ticking all stocks.
    /// Bible 5.6: intra-sector correlation.
    /// </summary>
    public void GenerateSectorShocks(IEnumerable<string> sectors)
    {
        _sectorShocks.Clear();
        foreach (var sector in sectors)
        {
            _sectorShocks[sector] = NextNormal();
        }
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

        // 2. Random walk with fat tails + sector correlation + volatility clustering
        var idiosyncratic = NextFatTail(); // t-distribution for fat tails (kurtosis ~5)
        var sectorShock = _sectorShocks.TryGetValue(stock.Sector, out var ss) ? ss : 0.0;

        // Dynamic correlation: rises during market stress (45% calm → 85% crisis)
        var correlation = BaseSectorCorrelation + (CrisisSectorCorrelation - BaseSectorCorrelation) * MarketStress;
        var blendedRandom = correlation * sectorShock + (1.0 - correlation) * idiosyncratic;

        // Volatility clustering (GARCH-lite): realized vol affects current vol
        var baseVol = (double)stock.BaseVolatility;
        if (_realizedVol.TryGetValue(stock.Symbol, out var prevVol))
            baseVol = 0.7 * baseVol + 0.3 * prevVol; // 30% persistence from yesterday's vol

        var randomComponent = (decimal)(blendedRandom * baseVol * sqrtTick);

        // 2b. Jump diffusion: rare large moves (Poisson process, ~1% chance per tick)
        if (_rng.NextDouble() < 0.001) // ~0.1% per tick = ~0.4 per day = ~100 per year across all stocks
        {
            var jumpSize = (decimal)(NextNormal() * baseVol * 3.0); // 3x normal move (reduced from 8x)
            randomComponent += jumpSize;
        }

        // 3. Mean reversion toward fair value
        var meanReversion = CalculateMeanReversion(stock) * (decimal)tickMinutes;

        // Combine components
        var totalReturn = drift + randomComponent + meanReversion;

        // Clamp per-tick return to prevent extreme moves (max ±3% per tick)
        // Allows ~10-15% daily moves from sustained drift but prevents single-tick blowouts
        totalReturn = Math.Clamp(totalReturn, -0.03m, 0.03m);

        // Apply to price (multiplicative)
        var newPrice = oldPrice * (1m + totalReturn);

        // Round number resistance: slight pull toward $10/$50/$100/$500 levels
        // Real markets: retail limit orders cluster at round numbers, creating support/resistance
        newPrice = ApplyRoundNumberEffect(newPrice);

        // Floor at $0.001 (never zero or negative)
        newPrice = Math.Max(newPrice, 0.001m);

        // Round to 2 decimal places
        stock.CurrentPrice = Math.Round(newPrice, 2);

        // Bid/ask spread is managed by AITraderEngine (Market Maker logic)
        // Only do a simple adjustment here to keep bid/ask near current price
        var currentSpread = stock.AskPrice - stock.BidPrice;
        if (currentSpread <= 0) currentSpread = stock.CurrentPrice * 0.002m;
        stock.BidPrice = Math.Round(stock.CurrentPrice - currentSpread / 2, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + currentSpread / 2, 2);
        stock.BidPrice = Math.Max(stock.BidPrice, 0.001m);

        // Track realized volatility for GARCH clustering
        var tickReturn = oldPrice > 0 ? Math.Abs((double)((newPrice - oldPrice) / oldPrice)) : 0;
        _realizedVol[stock.Symbol] = tickReturn * Math.Sqrt(390.0); // Annualize approx

        // Update day high/low
        if (stock.CurrentPrice > stock.DayHigh)
            stock.DayHigh = stock.CurrentPrice;
        if (stock.CurrentPrice < stock.DayLow || stock.DayLow == 0)
            stock.DayLow = stock.CurrentPrice;

        // Update 52-week high/low
        if (stock.CurrentPrice > stock.YearHigh || stock.YearHigh == 0)
            stock.YearHigh = stock.CurrentPrice;
        if (stock.CurrentPrice < stock.YearLow || stock.YearLow == 0)
            stock.YearLow = stock.CurrentPrice;

        // Generate tick volume
        UpdateVolume(stock, tickMinutes);

        // Debug logging removed for performance (was 102k+ entries/day at 263 stocks)
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
        else if (stock.Traits.Contains("Compounder"))
            baseDrift = 0.00002m; // Steady grower
        else if (stock.Traits.Contains("Cash Cow"))
            baseDrift = 0.000015m; // Reliable income
        else if (stock.Traits.Contains("Turnaround"))
            baseDrift = 0.00004m; // Recovery momentum (higher risk/reward)
        else if (stock.Traits.Contains("Value Stock"))
            baseDrift = 0.000008m; // Slight upward drift (undervalued)

        // Momentum Stock trait: trends persist longer
        if (stock.Traits.Contains("Momentum Stock"))
            baseDrift *= 1.5m;

        // Autocorrelation: 5-day momentum (positive) + 20-day mean reversion (negative)
        // Real markets: winners keep winning for days, then revert over weeks
        if (stock.Return5Day != 0)
            baseDrift += stock.Return5Day * 0.00001m; // Positive autocorrelation (momentum)
        if (Math.Abs(stock.Return20Day) > 0.10m)
            baseDrift -= stock.Return20Day * 0.000005m; // Mean reversion for overextended moves

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
            >= 9 => 0.0001m,  // 0.01% for mega caps (SPY-like)
            >= 7 => 0.0005m,  // 0.05% large caps
            >= 5 => 0.002m,   // 0.20% mid caps
            >= 3 => 0.008m,   // 0.80% small caps
            _ => 0.03m,       // 3.00% micro caps (realistic: $1 stock = $0.03 spread)
        };

        // Volatility widens spread
        var volFactor = 1m + stock.BaseVolatility * 10m;

        var halfSpread = stock.CurrentPrice * baseSpreadPercent * volFactor * stock.SpreadMultiplier;
        halfSpread = Math.Max(halfSpread, 0.005m); // Minimum $0.005 half-spread

        // Decay spread multiplier toward 1.0 (10% per tick)
        stock.SpreadMultiplier = 1.0m + (stock.SpreadMultiplier - 1.0m) * 0.99m;

        stock.BidPrice = Math.Round(stock.CurrentPrice - halfSpread, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + halfSpread, 2);

        // Ensure bid is never negative
        stock.BidPrice = Math.Max(stock.BidPrice, 0.001m);
    }

    /// <summary>Current tick within the trading day (0-389). Set by caller for U-shaped volume.</summary>
    public int CurrentDayTick { get; set; }

    private void UpdateVolume(Stock stock, double tickMinutes)
    {
        // Generate realistic tick volume with U-shaped intraday profile
        // Real markets: ~30% vol in first hour, ~10% midday, ~20% last hour
        var dailyVolume = stock.AverageVolume > 0 ? stock.AverageVolume : 100_000;

        // U-shaped multiplier based on time-of-day (tick 0-389)
        var t = CurrentDayTick;
        double uMultiplier;
        if (t < 60)        uMultiplier = 2.5 - (t / 60.0 * 1.5);      // 2.5x → 1.0x (first hour: high)
        else if (t < 270)  uMultiplier = 0.5 + (_rng.NextDouble() * 0.3); // 0.5-0.8x (midday: low)
        else if (t < 330)  uMultiplier = 0.8 + ((t - 270) / 60.0 * 0.7); // 0.8x → 1.5x (ramp up)
        else               uMultiplier = 1.5 + ((t - 330) / 60.0 * 1.5); // 1.5x → 3.0x (last hour: highest)

        var tickFraction = tickMinutes / 390.0;
        var baseTickVolume = (long)(dailyVolume * tickFraction * uMultiplier);

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

    /// <summary>
    /// Subtle pull toward psychological price levels ($10, $25, $50, $100, $250, $500).
    /// Simulates retail order clustering at round numbers.
    /// </summary>
    private static decimal ApplyRoundNumberEffect(decimal price)
    {
        if (price <= 0) return price;

        // Find nearest round number
        decimal[] levels = { 5m, 10m, 25m, 50m, 100m, 250m, 500m, 1000m };
        foreach (var level in levels)
        {
            var distance = Math.Abs(price - level) / level;
            if (distance < 0.02m) // Within 2% of round number
            {
                // Gentle pull toward the level (0.01% per tick)
                var pull = (level - price) * 0.0001m;
                price += pull;
                break; // Only apply to nearest level
            }
        }

        return price;
    }

    /// <summary>
    /// Generate random number from Student's t-distribution (df=5) for fat tails.
    /// Kurtosis ~9 (vs Gaussian 3) — produces realistic extreme moves.
    /// Uses the ratio of Normal / sqrt(ChiSquared/df).
    /// </summary>
    private double NextFatTail()
    {
        const int df = 5;
        var normal = NextNormal();

        // Chi-squared with df degrees of freedom = sum of df squared normals
        double chi2 = 0;
        for (int i = 0; i < df; i++)
        {
            var n = NextNormal();
            chi2 += n * n;
        }

        return normal / Math.Sqrt(chi2 / df);
    }
}
