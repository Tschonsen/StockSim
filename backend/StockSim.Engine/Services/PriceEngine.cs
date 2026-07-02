using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Core price simulation engine using Geometric Brownian Motion.
/// Calculates new prices for each stock per simulation tick.
/// See Spec section 5.2 for the full price model specification.
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

    // --- Named constants (extracted from magic numbers) ---
    private const double JumpDiffusionChance = 0.00003;   // ~0.003% per tick ≈ 3 jumps/year/stock
    private const double JumpSizeMultiplier = 2.5;         // Jump = 2.5× normal move
    private const decimal EventSentimentDriftPerTick = 0.0002m; // ±0.02% per tick at max sentiment

    // Normal distribution cache for Box-Muller transform
    private double? _spareNormal;

    /// <summary>Realized volatility tracker per stock (GARCH-lite: yesterday's vol affects today's).</summary>
    private readonly Dictionary<string, double> _realizedVol = new();


    /// <summary>ONNX price model for hybrid predictions. Null if not loaded.</summary>
    private PriceModel? _onnxModel;

    /// <summary>
    /// Daily ONNX predictions per stock. Set once per day at market open, used during intraday ticks.
    /// Key: symbol → (expectedReturn per tick, expectedVolatility per tick).
    /// </summary>
    private readonly Dictionary<string, (decimal ReturnPerTick, decimal VolPerTick)> _dailyOnnxPredictions = new();

    /// <summary>Blend weight for ONNX predictions vs GBM (0=pure GBM, 1=pure ONNX).</summary>
    public decimal OnnxBlendWeight { get; set; } = 0.4m;

    // === Runtime Modifiers (set per-tick by GameLoop, applied to ONNX output) ===

    /// <summary>Overall market sentiment from EconomicEngine (-1=fear, +1=greed). Shifts drift.</summary>
    public decimal MarketSentiment { get; set; }

    /// <summary>Per-stock event sentiment for current tick. Key: symbol → net sentiment (-1 to +1).</summary>
    public Dictionary<string, float> StockEventSentiment { get; } = new();

    /// <summary>Per-stock event volatility multiplier. Key: symbol → multiplier (1.0 = no effect).</summary>
    public Dictionary<string, float> StockEventVolMultiplier { get; } = new();

    /// <summary>Per-stock event volume multiplier. Key: symbol → multiplier (1.0 = no effect, 5.0 = 5x volume).</summary>
    public Dictionary<string, float> StockEventVolumeMult { get; } = new();

    /// <summary>Per-stock max active event severity (0=none, 1=Minor, 2=Moderate, 3=Major, 4=Critical). Widens daily clamp.</summary>
    public Dictionary<string, int> StockMaxEventSeverity { get; } = new();

    /// <summary>Sector multipliers from EconomicEngine (macro-adjusted). Key: sector → multiplier.</summary>
    public Dictionary<string, decimal> SectorMultipliers { get; } = new();

    /// <summary>Reference to all stocks for supply chain / rivalry lookups.</summary>
    public IReadOnlyList<Stock>? _allStocks;

    /// <summary>Seasonal volatility multiplier (from SeasonalityEngine).</summary>
    public decimal SeasonalVolatilityMult { get; set; } = 1m;

    /// <summary>Seasonal volume multiplier (from SeasonalityEngine).</summary>
    public decimal SeasonalVolumeMult { get; set; } = 1m;

    /// <summary>Current market stress level (0=calm, 1=crisis). Affects correlation and spreads.</summary>
    public double MarketStress { get; set; }

    /// <summary>Current market phase. Bull adds positive drift, Bear adds negative drift to all stocks.</summary>
    public MarketPhase Phase { get; set; } = MarketPhase.Neutral;

    /// <summary>
    /// Spec 5.6: Sector correlation. Each sector gets a shared random shock per tick.
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
    /// Attach the ONNX price model for hybrid predictions.
    /// </summary>
    public void SetOnnxModel(PriceModel model)
    {
        _onnxModel = model;
        _log.Info("ONNX model attached to PriceEngine", new { loaded = model.IsLoaded });
    }

    /// <summary>
    /// Generate daily ONNX predictions for all stocks. Call once at market open.
    /// Predictions are cached and used throughout the trading day.
    /// </summary>
    public void GenerateDailyOnnxPredictions(IReadOnlyList<Stock> stocks, Dictionary<string, List<Candle>> dailyHistory)
    {
        _dailyOnnxPredictions.Clear();
        if (_onnxModel == null || !_onnxModel.IsLoaded) return;

        var ticksPerDay = 390;
        if (ticksPerDay <= 0) ticksPerDay = 1;
        var predicted = 0;

        foreach (var stock in stocks)
        {
            if (!dailyHistory.TryGetValue(stock.Symbol, out var candles)) continue;
            if (candles.Count < PriceModel.LOOKBACK + 1) continue;

            var prediction = _onnxModel.PredictFromCandles(candles, stock.Sector);
            if (prediction.HasValue)
            {
                // Distribute daily prediction across ticks
                var returnPerTick = prediction.Value.ExpectedReturn / ticksPerDay;
                var volPerTick = prediction.Value.ExpectedVolatility / (decimal)Math.Sqrt(ticksPerDay);
                _dailyOnnxPredictions[stock.Symbol] = (returnPerTick, volPerTick);
                predicted++;
            }
        }

        if (predicted > 0)
            _log.Debug("ONNX daily predictions generated", new { predicted, total = stocks.Count });
    }

    /// <summary>
    /// Generate sector-level shocks for this tick. Call once before ticking all stocks.
    /// Spec 5.6: intra-sector correlation.
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
    /// <summary>M0 "pure emergent" / deterministic-replay mode: zero every stochastic component
    /// (fat tails, sector shock, jumps) so a price move reflects only drift + mean reversion —
    /// i.e. the fundamentals. Off in normal play; real markets need the stochastic texture.</summary>
    public bool DeterministicMode { get; set; }

    public void Tick(Stock stock, TimeSpan tickDuration)
    {
        var oldPrice = stock.CurrentPrice;
        if (oldPrice <= 0) return;

        var tickMinutes = tickDuration.TotalMinutes;
        var sqrtTick = Math.Sqrt(tickMinutes / 390.0); // Normalize to trading day (390 min)

        // 1. Drift component (long-term trend)
        var drift = CalculateDrift(stock) * (decimal)tickMinutes;

        // 2. Random walk with fat tails + sector correlation + volatility clustering
        var idiosyncratic = DeterministicMode ? 0.0 : NextFatTail(); // t-distribution for fat tails (kurtosis ~5)
        var sectorShock = _sectorShocks.TryGetValue(stock.Sector, out var ss) ? ss : 0.0;

        // Dynamic correlation: rises during market stress (45% calm → 85% crisis)
        // Convex curve: correlation jumps fast at onset (stress 0.25 → 65% instead of 55%)
        var stressFactor = Math.Pow(Math.Clamp(MarketStress, 0, 1), 0.5);
        var correlation = BaseSectorCorrelation + (CrisisSectorCorrelation - BaseSectorCorrelation) * stressFactor;
        // DeterministicMode strips all stochastic texture so a move is attributable to fundamentals.
        var blendedRandom = DeterministicMode ? 0.0 : correlation * sectorShock + (1.0 - correlation) * idiosyncratic;

        // Flight-to-quality: mega-caps fall less, small-caps fall more during stress
        if (MarketStress > 0.4 && blendedRandom < 0)
        {
            if (stock.LiquidityScore >= 8)
                blendedRandom *= 0.7; // Mega-cap: 30% less downside
            else if (stock.LiquidityScore <= 3)
                blendedRandom *= 1.25; // Small-cap: 25% more downside
        }

        // Volatility clustering (GARCH-lite): realized vol affects current vol
        var baseVol = (double)stock.BaseVolatility;
        if (_realizedVol.TryGetValue(stock.Symbol, out var prevVol))
            baseVol = 0.7 * baseVol + 0.3 * prevVol; // 30% persistence from yesterday's vol

        // Credit rating → volatility modifier (junk bonds = more volatile)
        if (stock.Personality?.CreditRating != null)
        {
            baseVol *= stock.Personality.CreditRating switch
            {
                "AAA" or "AA" => 0.85, // Less volatile, stable
                "A" => 0.95,
                "BBB" => 1.0,
                "BB" => 1.15,           // Junk = more volatile
                "B" => 1.35,            // Distressed = much more volatile
                _ => 1.0,
            };
        }

        // CEO archetype → volatility (Disruptors/Turnarounds more volatile, Steady Hands less)
        if (stock.Personality?.CEOArchetype != null)
        {
            baseVol *= stock.Personality.CEOArchetype switch
            {
                "Disruptor" => 1.2,
                "Turnaround Artist" => 1.25,
                "Visionary" => 1.1,
                "Steady Hand" => 0.8,
                "Finance Veteran" => 0.85,
                "Cost-Cutter" => 0.9,
                _ => 1.0,
            };
        }

        // Company maturity → volatility: young firms swing more, old institutions are stable.
        if (stock.Personality != null && stock.Personality.FoundedYear > 0 && CurrentYear > 0)
        {
            var age = CurrentYear - stock.Personality.FoundedYear;
            baseVol *= FundamentalDynamics.MaturityModifiers(age).VolMultiplier;
        }

        // Apply seasonal volatility multiplier
        baseVol *= (double)SeasonalVolatilityMult;

        var randomComponent = (decimal)(blendedRandom * baseVol * sqrtTick);

        // 2b. Jump diffusion: rare large moves (Poisson process)
        // Real markets: ~2-3 jumps per stock per year ≈ 0.01/day ≈ 0.000026/tick
        if (!DeterministicMode && _rng.NextDouble() < JumpDiffusionChance) // ~0.003% per tick ≈ 0.012/day ≈ 3 per year per stock
        {
            var jumpSize = (decimal)(NextNormal() * baseVol * JumpSizeMultiplier); // 2.5x normal move
            randomComponent += jumpSize;
        }

        // 3. Mean reversion toward fair value
        var meanReversion = CalculateMeanReversion(stock) * (decimal)tickMinutes;

        // Combine GBM components
        var gbmReturn = drift + randomComponent + meanReversion;

        // === HYBRID: Blend with ONNX prediction + runtime modifiers ===
        var totalReturn = gbmReturn;
        if (_dailyOnnxPredictions.TryGetValue(stock.Symbol, out var onnxPred))
        {
            // ONNX provides drift direction + volatility scaling
            var onnxDrift = onnxPred.ReturnPerTick;
            var onnxVol = onnxPred.VolPerTick;

            // --- Runtime Modifier 1: Event Sentiment shifts drift ---
            // Active events for this stock push drift up (positive) or down (negative)
            if (StockEventSentiment.TryGetValue(stock.Symbol, out var evtSentiment) && evtSentiment != 0)
                onnxDrift += (decimal)evtSentiment * EventSentimentDriftPerTick; // ±0.02% per tick at max sentiment

            // --- Runtime Modifier 2: Market Sentiment shifts drift globally ---
            // Economic conditions: fear pulls drift down, greed pushes up
            onnxDrift += MarketSentiment * 0.00005m; // ±0.005% per tick

            // --- Runtime Modifier 3: Sector Multiplier from macro conditions ---
            // Interest rates, oil price, inflation affect sectors differently
            if (SectorMultipliers.TryGetValue(stock.Sector, out var sectorMult))
                onnxDrift *= sectorMult;

            // --- Runtime Modifier 4: Event Volatility amplifies vol ---
            // Active events increase expected volatility
            if (StockEventVolMultiplier.TryGetValue(stock.Symbol, out var evtVolMult) && evtVolMult > 1f)
                onnxVol *= (decimal)evtVolMult;

            // --- Runtime Modifier 5: Market Stress amplifies vol ---
            // High stress → higher volatility (on top of existing GBM stress correlation)
            onnxVol *= 1m + (decimal)MarketStress * 0.5m;

            // Blend: use ONNX for drift direction, keep GBM randomness but scale by ONNX vol
            var blendedDrift = (1m - OnnxBlendWeight) * drift + OnnxBlendWeight * onnxDrift;
            var volScale = onnxVol > 0 ? (1m - OnnxBlendWeight) + OnnxBlendWeight * (onnxVol / Math.Max((decimal)baseVol * (decimal)sqrtTick, 0.0001m)) : 1m;
            volScale = Math.Clamp(volScale, 0.5m, 2.0m); // Allow moderate event-driven vol spikes

            totalReturn = blendedDrift + randomComponent * volScale + meanReversion;
        }

        // Clamp per-tick return to prevent extreme moves (max ±1.5% per tick)
        // At 390 ticks/day, allows ~5-8% daily moves from sustained drift
        totalReturn = Math.Clamp(totalReturn, -0.015m, 0.015m);

        // Apply to price (multiplicative)
        var newPrice = oldPrice * (1m + totalReturn);

        // Daily return clamp: severity-dependent (Normal ±3%, Major ±5%, Critical ±8%)
        // Allows big events to feel impactful while preventing compound drift
        if (stock.PreviousClose > 0)
        {
            var clampPct = 0.03m; // Default ±3%
            if (StockMaxEventSeverity.TryGetValue(stock.Symbol, out var severity))
            {
                clampPct = severity switch
                {
                    >= 2 => 0.05m, // Major+: ±5%
                    1 => 0.04m,    // Moderate: ±4%
                    _ => 0.03m,    // Minor/None: ±3%
                };
            }
            var maxDailyPrice = stock.PreviousClose * (1m + clampPct);
            var minDailyPrice = stock.PreviousClose * (1m - clampPct);
            newPrice = Math.Clamp(newPrice, minDailyPrice, maxDailyPrice);
        }

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
        // Spec 5.2.1: Drift = long-term trend, +0.001% per tick for growth
        // Base drift: ~7-8% annual average (real S&P 500 long-term return)
        // 0.00003m per tick × 390 ticks/day × 252 days = ~2.95% → stocks layer on top
        decimal baseDrift = 0.00003m; // Market-wide baseline (~7.5% annual)

        if (stock.Traits.Contains("Growth Stock") || stock.Traits.Contains("Fast Grower"))
            baseDrift = 0.00005m;    // ~12.5% annual
        else if (stock.Traits.Contains("Slow Grower") || stock.Traits.Contains("Defensive"))
            baseDrift = 0.00002m;    // ~5% annual
        else if (stock.Traits.Contains("Speculative") || stock.Traits.Contains("Penny Stock"))
            baseDrift = 0.00001m;    // ~2.5% annual (volatile, low base)
        else if (stock.Traits.Contains("Compounder"))
            baseDrift = 0.00004m;    // ~10% annual
        else if (stock.Traits.Contains("Cash Cow"))
            baseDrift = 0.000025m;   // ~6% annual + dividends
        else if (stock.Traits.Contains("Turnaround"))
            baseDrift = 0.00006m;    // ~15% annual (high risk/reward)
        else if (stock.Traits.Contains("Value Stock"))
            baseDrift = 0.000025m;   // ~6% annual (undervalued)

        // Momentum Stock trait: trends persist longer
        if (stock.Traits.Contains("Momentum Stock"))
            baseDrift *= 1.5m;

        // Market phase modifier (symmetric: Bull and Bear roughly equal magnitude)
        // Bull: +0.00002 (~5% annual boost), Bear: -0.00002 (~5% annual drag)
        baseDrift += Phase switch
        {
            MarketPhase.Bull => 0.00002m,
            MarketPhase.Bear => -0.00002m,
            _ => 0m,
        };

        // Cyclical stocks are more sensitive to market phase
        if (stock.Traits.Contains("Cyclical"))
        {
            baseDrift += Phase switch
            {
                MarketPhase.Bull => 0.000015m,
                MarketPhase.Bear => -0.000015m,
                _ => 0m,
            };
        }

        // Defensive stocks resist bear markets
        if (stock.Traits.Contains("Defensive") && Phase == MarketPhase.Bear)
            baseDrift += 0.000015m; // Offsets most of the bear drag

        // CEO Archetype influences company trajectory
        if (stock.Personality != null)
        {
            baseDrift += stock.Personality.CEOArchetype switch
            {
                "Visionary" => 0.000012m,        // Ambitious growth, higher upside
                "Disruptor" => 0.000015m,         // High risk, high reward
                "Founder-CEO" => 0.00001m,        // Passionate, above-average returns
                "Empire Builder" => 0.000008m,    // Acquisitive growth
                "Sales Machine" => 0.000006m,     // Revenue-focused
                "Engineer-CEO" => 0.000005m,      // Product quality, steady
                "Turnaround Artist" => 0.00002m,  // Big if it works, volatile
                "Steady Hand" => 0.000002m,       // Conservative, low volatility
                "Finance Veteran" => 0.000003m,   // Capital allocation focused
                "Cost-Cutter" => -0.000002m,      // Short-term boost, long-term drag
                "Industry Insider" => 0.000004m,  // Knows the sector
                "Dealmaker" => 0.000007m,         // M&A driven growth
                _ => 0m,
            };
        }

        // Credit rating → risk premium (low-rated companies have higher volatility + slight negative drift)
        if (stock.Personality?.CreditRating != null)
        {
            baseDrift += stock.Personality.CreditRating switch
            {
                "AAA" or "AA" => 0.000003m,   // Blue-chip premium
                "A" => 0.000001m,              // Investment grade
                "BBB" => 0m,                   // Borderline
                "BB" => -0.000003m,            // Junk territory, higher yield but risky
                "B" => -0.000008m,             // High risk, distressed
                _ => 0m,
            };
        }

        // Rivalry effect: competitor's performance inversely affects this stock
        if (!string.IsNullOrEmpty(stock.Personality?.RivalSymbol) && _allStocks != null)
        {
            var rival = _allStocks.FirstOrDefault(s => s.Symbol == stock.Personality!.RivalSymbol);
            if (rival != null && rival.PreviousClose > 0)
            {
                var rivalReturn = (rival.CurrentPrice - rival.PreviousClose) / rival.PreviousClose;
                baseDrift -= rivalReturn * 0.05m; // 5% inverse: rival up → slight headwind
            }
        }

        // Supply chain effect: supplier/customer performance flows through
        if (stock.Personality?.Suppliers.Count > 0 && _allStocks != null)
        {
            foreach (var supplierSym in stock.Personality.Suppliers)
            {
                var supplier = _allStocks.FirstOrDefault(s => s.Symbol == supplierSym);
                if (supplier != null && supplier.PreviousClose > 0)
                {
                    var supplierReturn = (supplier.CurrentPrice - supplier.PreviousClose) / supplier.PreviousClose;
                    // Negative supplier performance hurts this stock (supply disruption)
                    if (supplierReturn < -0.03m)
                        baseDrift += supplierReturn * 0.15m; // 15% of supplier's loss flows through
                }
            }
        }

        // Autocorrelation: 5-day momentum (positive) + 20-day mean reversion (negative)
        // Real markets: winners keep winning for days, then revert over weeks
        if (stock.Return5Day != 0)
            baseDrift += stock.Return5Day * 0.00001m; // Positive autocorrelation (momentum)
        if (Math.Abs(stock.Return20Day) > 0.10m)
            baseDrift -= stock.Return20Day * 0.000005m; // Mean reversion for overextended moves

        // Flight to quality: during high stress, safe-haven stocks attract capital
        if (MarketStress > 0.5)
        {
            var stressMag = (decimal)MarketStress;
            if (stock.Traits.Contains("Defensive") || stock.Sector == "Utilities" || stock.Sector == "Healthcare")
                baseDrift += 0.00008m * stressMag; // Safe havens drift up
            else if (stock.Traits.Contains("Speculative") || stock.Traits.Contains("Penny Stock"))
                baseDrift -= 0.00015m * stressMag; // Speculative crushed harder
        }

        // Company maturity → drift: young firms compound faster (and crater harder), old firms
        // are steadier. Applied last so it scales the whole assembled drift profile.
        if (stock.Personality != null && stock.Personality.FoundedYear > 0 && CurrentYear > 0)
        {
            var age = CurrentYear - stock.Personality.FoundedYear;
            baseDrift *= FundamentalDynamics.MaturityModifiers(age).DriftMultiplier;
        }

        return baseDrift;
    }

    private decimal CalculateMeanReversion(Stock stock)
    {
        if (stock.FairValue <= 0) return 0m;

        var deviation = (stock.CurrentPrice - stock.FairValue) / stock.FairValue;

        // Progressive mean reversion: kicks in at 5%, grows quadratically
        // At 10% deviation: 0.1² × 0.02 × 390 = 0.78% daily pull-back
        // At 15% deviation: 0.15² × 0.02 × 390 = 1.76% daily pull-back
        // At 20% deviation: 0.2² × 0.02 × 390 = 3.12% daily pull-back (matches 3% clamp!)
        // Above 20%, mean reversion fully counteracts the daily clamp → equilibrium
        if (Math.Abs(deviation) < 0.05m) return 0m;

        // Quadratic strength: small deviations = gentle pull, large = strong pull
        var reversionStrength = 0.02m * Math.Abs(deviation);
        return -deviation * reversionStrength;
    }

    private void UpdateBidAsk(Stock stock)
    {
        // Spec 5.2.3: Spread = BaseSpread × VolatilityFactor × (1 / LiquidityFactor)
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

    /// <summary>Current in-game year, set by GameLoop. Used for company-age (maturity) effects.
    /// 0 = unset (maturity skipped — keeps unit tests of price dynamics deterministic).</summary>
    public int CurrentYear { get; set; }

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

        // Event volume spike: apply VolumeMultiplier from active events
        if (StockEventVolumeMult.TryGetValue(stock.Symbol, out var eventVolMult) && eventVolMult > 1f)
            baseTickVolume = (long)(baseTickVolume * eventVolMult);

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
