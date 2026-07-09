using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Simulates macroeconomic indicators that drive market behavior.
/// Indicators change gradually over time with occasional shocks.
/// Scheduled data releases create market-moving events.
/// </summary>
public class EconomicEngine
{
    private readonly Logger _log = new("EconomicEngine");
    private readonly Random _rng;
    private int _daysSinceFOMC;
    private int _daysSinceElection;
    private string? _currentAdministration;

    /// <summary>Sector multiplier shifts from the current political administration.</summary>
    public Dictionary<string, decimal> ElectionSectorShifts { get; set; } = new();

    public EconomicData Data { get; set; }
    public List<EconomicEvent> UpcomingEvents { get; } = new();
    public List<EconomicEvent> ReleasedThisTick { get; } = new();
    public List<EconomicEvent> EventHistory { get; } = new();

    /// <summary>World-layer commodity markets (M3, WORLD_SIM_VISION.md §5) backing emergent pricing.
    /// Events/future slices move a region's ProductionModifier to inject supply shocks.</summary>
    public CommodityMarket OilMarket { get; } = CommodityMarket.CreateOilWorld();
    public CommodityMarket GasMarket { get; } = CommodityMarket.CreateGasWorld();
    public CommodityMarket GoldMarket { get; } = CommodityMarket.CreateGoldWorld();

    /// <summary>When true (default), commodity prices EMERGE from world supply/demand (M3) instead of a
    /// random walk: demand tracks the economy and events become transient supply/demand shocks. Set false
    /// to fall back to the legacy random walk + direct price-setting. See UpdateEmergentOil/Gas.</summary>
    public bool EmergentCommodityPricing { get; set; } = true;

    // Emergent-commodity calibration (see UpdateEmergentOil/UpdateEmergentGas).
    private const decimal OilBasePrice = 75m;
    private const decimal OilDemandBeta = 0.10m;    // procyclical demand sensitivity to economic activity
    private const decimal OilReversionRate = 0.10m; // fraction of the price↔fundamental gap closed per day
    private const decimal OilShockDecay = 0.9m;     // daily decay of an event demand-shock toward 0
    private const decimal OilEventShock = 0.5m;     // event surprise → demand-shock scale
    private const decimal GasBasePrice = 3.5m;
    private const decimal GasDemandBeta = 0.14m;    // gas demand swings a bit more than oil with activity
    private const decimal GasReversionRate = 0.10m;
    private const decimal GasOilLink = 0.10m;       // oil→gas substitution: expensive oil lifts gas demand
    private const decimal GoldBasePrice = 1900m;
    private const decimal GoldDemandBeta = 0.06m;   // safe-haven demand sensitivity (small flows, big moves)
    private const decimal GoldReversionRate = 0.08m;

    /// <summary>Country roster of the World layer (M3, §5): stability drives commodity supply.</summary>
    public WorldState World { get; }

    /// <summary>Daily probability of a geopolitical instability shock hitting a random country (rare).
    /// Settable so tests/scenarios can disable or tune it.</summary>
    public double CountryInstabilityChance { get; set; } = 0.004;
    private const decimal CountryRecoveryRate = 0.02m; // fraction of the stability gap healed per day

    // Track initial values for comparison
    private readonly EconomicData _initial;

    public EconomicEngine(int seed)
    {
        _rng = new Random(seed);
        Data = GenerateInitialConditions();
        _initial = new EconomicData
        {
            InterestRate = Data.InterestRate,
            InflationRate = Data.InflationRate,
            UnemploymentRate = Data.UnemploymentRate,
            GDPGrowth = Data.GDPGrowth,
            ConsumerConfidence = Data.ConsumerConfidence,
        };
        World = WorldState.CreateDefault(OilMarket, GasMarket, GoldMarket);
    }

    private EconomicData GenerateInitialConditions()
    {
        return new EconomicData
        {
            InterestRate = Math.Round((decimal)(_rng.NextDouble() * 4 + 1), 2),        // 1-5%
            InflationRate = Math.Round((decimal)(_rng.NextDouble() * 3 + 1), 2),        // 1-4%
            UnemploymentRate = Math.Round((decimal)(_rng.NextDouble() * 4 + 3), 1),     // 3-7%
            GDPGrowth = Math.Round((decimal)(_rng.NextDouble() * 4 + 0.5), 2),          // 0.5-4.5%
            ConsumerConfidence = Math.Round((decimal)(_rng.NextDouble() * 40 + 70), 1), // 70-110
            TreasuryYield10Y = Math.Round((decimal)(_rng.NextDouble() * 3 + 2), 2),     // 2-5%
            OilPrice = Math.Round((decimal)(_rng.NextDouble() * 60 + 50), 2),           // 50-110
            GoldPrice = Math.Round((decimal)(_rng.NextDouble() * 800 + 1500), 0),       // 1500-2300
            HousingStarts = Math.Round((decimal)(_rng.NextDouble() * 800 + 800), 0),    // 800-1600
            ManufacturingPMI = Math.Round((decimal)(_rng.NextDouble() * 15 + 45), 1),   // 45-60
        };
    }

    /// <summary>
    /// Schedule upcoming economic events for the next 30 days.
    /// Called at game start and periodically.
    /// </summary>
    public void ScheduleEvents(DateTime gameTime)
    {
        UpcomingEvents.Clear();
        var templates = new[]
        {
            ("fed_rate", "Fed Interest Rate Decision", "InterestRate", "High", 42),     // Every ~6 weeks
            ("cpi", "CPI Inflation Report", "InflationRate", "High", 30),               // Monthly
            ("jobs", "Jobs Report (NFP)", "UnemploymentRate", "High", 30),              // Monthly
            ("gdp", "GDP Growth (Quarterly)", "GDPGrowth", "High", 90),                 // Quarterly
            ("consumer", "Consumer Confidence Index", "ConsumerConfidence", "Medium", 30),
            ("treasury", "10Y Treasury Auction", "TreasuryYield10Y", "Medium", 14),
            ("oil", "EIA Crude Inventory", "OilPrice", "Medium", 7),                    // Weekly
            ("housing", "Housing Starts Report", "HousingStarts", "Low", 30),
            ("pmi", "Manufacturing PMI", "ManufacturingPMI", "Medium", 30),
        };

        foreach (var (id, name, indicator, impact, intervalDays) in templates)
        {
            // Schedule multiple releases over next 90 days
            var nextDate = gameTime.AddDays(_rng.Next(1, intervalDays));
            while (nextDate < gameTime.AddDays(90))
            {
                // Only on weekdays at 8:30 AM
                while (nextDate.DayOfWeek == DayOfWeek.Saturday || nextDate.DayOfWeek == DayOfWeek.Sunday)
                    nextDate = nextDate.AddDays(1);

                var currentValue = GetIndicatorValue(indicator);
                var expected = currentValue + (decimal)((_rng.NextDouble() - 0.5) * 0.3) * currentValue * 0.01m;

                UpcomingEvents.Add(new EconomicEvent
                {
                    Id = $"{id}_{nextDate:yyyyMMdd}",
                    Name = name,
                    Indicator = indicator,
                    ScheduledDate = new DateTime(nextDate.Year, nextDate.Month, nextDate.Day, 8, 30, 0),
                    PreviousValue = currentValue,
                    ExpectedValue = Math.Round(expected, 2),
                    Impact = impact,
                });

                nextDate = nextDate.AddDays(intervalDays + _rng.Next(-3, 4));
            }
        }

        UpcomingEvents.Sort((a, b) => a.ScheduledDate.CompareTo(b.ScheduledDate));
        _log.Info("Economic events scheduled", new { count = UpcomingEvents.Count });
    }

    /// <summary>
    /// Tick the economic engine. Called daily at market open.
    /// Gradually evolves indicators and releases scheduled data.
    /// </summary>
    public void TickDay(DateTime gameTime)
    {
        ReleasedThisTick.Clear();

        // Advance the World layer first so commodity supply reflects country state this tick.
        if (EmergentCommodityPricing) TickWorld();

        // Gradual drift of all indicators (small random walk)
        DriftIndicators();

        // Check for scheduled releases today
        var todayEvents = UpcomingEvents
            .Where(e => !e.Released && e.ScheduledDate.Date <= gameTime.Date)
            .ToList();

        foreach (var ev in todayEvents)
        {
            ReleaseEvent(ev);
            ReleasedThisTick.Add(ev);
            EventHistory.Add(ev);
        }

        // Remove released events
        UpcomingEvents.RemoveAll(e => e.Released);

        // Re-schedule if running low
        if (UpcomingEvents.Count < 10)
        {
            ScheduleEvents(gameTime);
        }
    }

    private void DriftIndicators()
    {
        // Small daily random walk for each indicator
        Data.InterestRate = Clamp(Data.InterestRate + Drift(0.01m), 0, 15);
        Data.InflationRate = Clamp(Data.InflationRate + Drift(0.02m), -1, 15);
        Data.UnemploymentRate = Clamp(Data.UnemploymentRate + Drift(0.02m), 2, 15);
        Data.GDPGrowth = Clamp(Data.GDPGrowth + Drift(0.03m), -5, 8);
        Data.ConsumerConfidence = Clamp(Data.ConsumerConfidence + Drift(0.5m), 20, 120);
        Data.TreasuryYield10Y = Clamp(Data.TreasuryYield10Y + Drift(0.01m), 0.5m, 10);
        Data.OilPrice = EmergentCommodityPricing
            ? UpdateEmergentOil()
            : Clamp(Data.OilPrice + Drift(0.5m), 20, 150);
        Data.GoldPrice = EmergentCommodityPricing
            ? UpdateEmergentGold()
            : Clamp(Data.GoldPrice + Drift(5m), 800, 3000);
        Data.HousingStarts = Clamp(Data.HousingStarts + Drift(5m), 500, 2000);
        Data.ManufacturingPMI = Clamp(Data.ManufacturingPMI + Drift(0.1m), 30, 65);
        Data.WageIndex = Clamp(Data.WageIndex + Drift(0.3m), 85, 140);
        Data.NatGasPrice = EmergentCommodityPricing
            ? UpdateEmergentGas()
            : Clamp(Data.NatGasPrice + Drift(0.1m), 1.5m, 15);
    }

    /// <summary>
    /// Emergent oil price (M3 slice 1, WORLD_SIM_VISION.md §5): demand tracks the economy — a boom lifts
    /// consumption, a recession destroys it — and the price mean-reverts toward the world supply/demand
    /// fundamental, plus micro-noise texture (§S5). So an oil move has a real CAUSE (the economy, or a
    /// producer outage via ProductionModifier) instead of being a random walk. Reads the current macro
    /// state; the acyclic driver coupling (oil→inflation→rate→growth) then closes a realistic commodity cycle.
    /// </summary>
    private decimal UpdateEmergentOil()
    {
        var activity = 0.5m * GetDriverDeviation("GDPGrowth") + 0.5m * GetDriverDeviation("ManufacturingPMI");
        OilMarket.DemandModifier = 1m + OilDemandBeta * activity + OilMarket.EventDemandShock;
        var fundamental = OilMarket.FundamentalPrice(OilBasePrice);
        OilMarket.EventDemandShock *= OilShockDecay; // fade the transient shock (rebalancing)
        var reverted = Data.OilPrice + (fundamental - Data.OilPrice) * OilReversionRate;
        return Clamp(reverted + Drift(0.5m), 20, 150);
    }

    /// <summary>
    /// Emergent natural-gas price (M3): demand tracks industrial/power activity (PMI-weighted, plus some
    /// GDP) with an oil-substitution linkage — expensive oil pushes users toward gas — and the price
    /// mean-reverts toward the gas supply/demand fundamental plus micro-noise. Replaces the random walk
    /// AND the old ad-hoc oil→gas price nudge (that linkage now lives durably in gas demand).
    /// </summary>
    private decimal UpdateEmergentGas()
    {
        var activity = 0.6m * GetDriverDeviation("ManufacturingPMI") + 0.2m * GetDriverDeviation("GDPGrowth");
        var oilSub = GasOilLink * GetDriverDeviation("OilPrice");
        GasMarket.DemandModifier = 1m + GasDemandBeta * activity + oilSub + GasMarket.EventDemandShock;
        var fundamental = GasMarket.FundamentalPrice(GasBasePrice);
        GasMarket.EventDemandShock *= OilShockDecay;
        var reverted = Data.NatGasPrice + (fundamental - Data.NatGasPrice) * GasReversionRate;
        return Clamp(reverted + Drift(0.1m), 1.5m, 15);
    }

    /// <summary>
    /// Emergent gold price (M3): gold is a safe-haven/monetary asset, not a consumption commodity, so its
    /// demand is driven by fear (low confidence) and low real rates (high inflation and/or low nominal
    /// rates) — NOT economic activity. Mine supply is near-static, so investment demand moves the price
    /// (high elasticity). Price mean-reverts toward the fundamental + micro-noise.
    /// </summary>
    private decimal UpdateEmergentGold()
    {
        var fear = -GetDriverDeviation("ConsumerConfidence");                                    // risk-off bids gold
        var lowRealRates = GetDriverDeviation("InflationRate") - GetDriverDeviation("InterestRate"); // negative real rates favour gold
        var safeHaven = 0.5m * fear + 0.4m * lowRealRates;
        GoldMarket.DemandModifier = 1m + GoldDemandBeta * safeHaven;
        var fundamental = GoldMarket.FundamentalPrice(GoldBasePrice);
        var reverted = Data.GoldPrice + (fundamental - Data.GoldPrice) * GoldReversionRate;
        return Clamp(reverted + Drift(5m), 800, 3000);
    }

    /// <summary>
    /// Advance the World layer (M3, §5): occasionally a rare geopolitical shock destabilises a random
    /// country (severity 0.2–0.6), then every country's stability drives its commodity output and heals
    /// toward baseline. An unstable producer's supply drop lifts its commodities' prices via the markets —
    /// so a war/embargo becomes an endogenous supply shock, not an injected price move.
    /// </summary>
    private void TickWorld()
    {
        if (_rng.NextDouble() < CountryInstabilityChance && World.Countries.Count > 0)
        {
            var c = World.Countries[_rng.Next(World.Countries.Count)];
            var severity = (decimal)(_rng.NextDouble() * 0.4 + 0.2);
            c.Stability = Math.Max(0m, c.Stability - severity);
            _log.Info("Geopolitical instability", new { country = c.Name, severity = Math.Round(severity, 2), stability = Math.Round(c.Stability, 2) });
        }
        World.TickMacro(CountryRecoveryRate);
    }

    private void ReleaseEvent(EconomicEvent ev)
    {
        // Generate actual value with surprise element
        var expected = ev.ExpectedValue ?? GetIndicatorValue(ev.Indicator);
        var surprise = (decimal)((_rng.NextDouble() - 0.5) * 2); // -1 to +1
        var magnitude = ev.Impact == "High" ? 0.03m : ev.Impact == "Medium" ? 0.015m : 0.008m;

        var actual = expected * (1 + surprise * magnitude);
        actual = Math.Round(actual, 2);
        ev.ActualValue = actual;
        ev.Released = true;

        // Apply the actual value to the indicator. Under emergent pricing, an oil release is an
        // inventory surprise → a transient demand shock on the market (which drives the price with
        // inertia), not a direct price override that would just decay away.
        if (ev.Indicator == "OilPrice" && EmergentCommodityPricing)
            OilMarket.EventDemandShock += surprise * magnitude * OilEventShock;
        else
            SetIndicatorValue(ev.Indicator, actual);

        _log.Info("Economic data released", new
        {
            name = ev.Name,
            indicator = ev.Indicator,
            expected,
            actual,
            surprise = ev.Surprise,
            impact = ev.Impact,
        });
    }

    /// <summary>
    /// Get sector drift multipliers based on current economic conditions.
    /// Called by PriceEngine to adjust sector performance.
    /// </summary>
    public Dictionary<string, decimal> GetSectorMultipliers()
    {
        var m = new Dictionary<string, decimal>();
        var rate = Data.InterestRate;
        var inflation = Data.InflationRate;
        var unemployment = Data.UnemploymentRate;
        var confidence = Data.ConsumerConfidence;
        var pmi = Data.ManufacturingPMI;

        // High rates hurt growth/tech, help financials
        var rateFactor = (rate - 3m) / 3m; // Normalized: 0 = neutral, +1 = high, -1 = low

        // Amplified impact (3x more realistic than before)
        // Real: 2% rate hike crushes Tech -20-30%, helps Financials +10-15%
        m["Technology"] = 1m - rateFactor * 0.8m;      // Strong rate sensitivity (growth stocks)
        m["Financials"] = 1m + rateFactor * 0.5m;      // Banks profit from higher rates
        m["Real Estate"] = 1m - rateFactor * 1.0m;     // REITs extremely rate-sensitive
        m["Utilities"] = 1m - rateFactor * 0.3m;       // Rate-sensitive (capital-intensive)
        m["Energy"] = 1m + (Data.OilPrice - 75m) / 75m * 0.6m; // Strong oil correlation
        m["Healthcare"] = 1m - rateFactor * 0.1m;      // Relatively immune
        m["Consumer Goods"] = 1m + (confidence - 90m) / 90m * 0.4m;
        m["Industrials"] = 1m + (pmi - 50m) / 50m * 0.5m;
        m["Materials"] = 1m + (inflation - 2m) / 5m * 0.4m;
        m["Telecommunications"] = 1m - rateFactor * 0.3m;
        m["Luxury Goods"] = 1m + (confidence - 90m) / 90m * 0.5m - (unemployment - 4m) / 10m * 0.4m;
        m["Transportation"] = 1m - (Data.OilPrice - 75m) / 75m * 0.4m + (pmi - 50m) / 50m * 0.3m;

        // Commodities sector: driven by gold and oil prices relative to baseline
        m["Commodities"] = 1m + (Data.GoldPrice - 1900m) / 1900m * 0.5m + (Data.OilPrice - 75m) / 75m * 0.3m;

        // ETF sector uses first word match
        m["ETF"] = 1m;

        // Apply election sector shifts (political administration bias)
        foreach (var (sector, shift) in ElectionSectorShifts)
            if (m.ContainsKey(sector)) m[sector] *= shift;

        // Clamp all sector multipliers to ±3% to prevent extreme sector drift
        foreach (var key in m.Keys.ToList())
            m[key] = Math.Clamp(m[key], 0.97m, 1.03m);

        return m;
    }

    /// <summary>
    /// Get overall market sentiment from economic indicators. Range: -1 (bearish) to +1 (bullish).
    /// </summary>
    public decimal GetMarketSentiment()
    {
        var sentiment = 0m;
        sentiment += (Data.GDPGrowth - 2m) / 5m * 0.3m;
        sentiment += (Data.ConsumerConfidence - 80m) / 40m * 0.2m;
        sentiment += (Data.ManufacturingPMI - 50m) / 15m * 0.2m;
        sentiment -= (Data.UnemploymentRate - 5m) / 10m * 0.15m;
        sentiment -= (Data.InterestRate - 3m) / 5m * 0.15m;
        return Math.Max(-1m, Math.Min(1m, sentiment));
    }

    /// <summary>Get Fear & Greed index (0-100). Based on multiple economic factors.</summary>
    public int GetFearGreedIndex()
    {
        var sentiment = GetMarketSentiment();
        // Map -1..+1 to 0..100
        return (int)Math.Round((sentiment + 1m) / 2m * 100m);
    }

    /// <summary>
    /// Market Volatility Index (VIX-equivalent, 0-80).
    /// Based on recent market moves, event severity, and economic uncertainty.
    /// VIX 12-15 = calm, 20-25 = normal, 30+ = elevated, 50+ = crisis.
    /// </summary>
    public double MarketVolatilityIndex { get; set; } = 18.0;

    /// <summary>Update VIX based on market conditions. Called daily.</summary>
    public void UpdateVolatilityIndex(IReadOnlyList<Stock> stocks, int activeEventCount, double hedgeFundStress)
    {
        // Component 1: Average stock volatility (realized)
        var avgVol = stocks.Count > 0
            ? (double)stocks.Average(s => s.BaseVolatility) * 100 * Math.Sqrt(252) // Annualize
            : 20.0;

        // Component 2: Event intensity
        var eventComponent = Math.Min(activeEventCount * 1.5, 20.0);

        // Component 3: Economic uncertainty
        var fearGreed = GetFearGreedIndex();
        var uncertaintyComponent = fearGreed < 30 ? (30 - fearGreed) * 0.5 : 0; // Fear adds vol

        // Component 4: Hedge fund stress
        var stressComponent = hedgeFundStress * 15.0;

        var rawVix = avgVol * 0.4 + eventComponent + uncertaintyComponent + stressComponent + 10;
        rawVix = Math.Clamp(rawVix, 9, 80);

        // Smooth: 70% old + 30% new (prevent jumps)
        MarketVolatilityIndex = Math.Round(MarketVolatilityIndex * 0.7 + rawVix * 0.3, 1);
    }

    /// <summary>Get commodity prices for dashboard display.</summary>
    public CommodityPrices GetCommodityPrices()
    {
        return new CommodityPrices
        {
            CrudeOil = Data.OilPrice,
            Gold = Data.GoldPrice,
            NatGas = Math.Round(Data.NatGasPrice, 2), // real driver now (utilities input cost / gas producer output)
            Silver = Math.Round(Data.GoldPrice * 0.035m + _rng.Next(-2, 2), 2),
            Copper = Math.Round(3.5m + (Data.ManufacturingPMI - 50m) * 0.02m, 2),
            Bitcoin = Math.Round(40000m + (Data.ConsumerConfidence - 80m) * 200m + _rng.Next(-500, 500), 0),
        };
    }

    /// <summary>Current value of a named macro driver (e.g. "OilPrice", "InterestRate"), for the
    /// emergent driver-coupling model. Unknown drivers return 0. See design/EMERGENT_COUPLING.md.</summary>
    public decimal GetDriverValue(string driver) => GetIndicatorValue(driver);

    /// <summary>Per-driver (baseline, scale) for normalisation — a driver's "signal" is its deviation
    /// from baseline in scale units. Same anchors as GetSectorMultipliers, so the emergent model and the
    /// legacy decorated one agree on what "off-normal" means. See design/EMERGENT_COUPLING.md.</summary>
    private static readonly Dictionary<string, (decimal Baseline, decimal Scale)> DriverNorm = new()
    {
        ["InterestRate"] = (3m, 3m),
        ["InflationRate"] = (2m, 5m),
        ["UnemploymentRate"] = (4m, 10m),
        ["GDPGrowth"] = (2m, 5m),
        ["ConsumerConfidence"] = (90m, 90m),
        ["ManufacturingPMI"] = (50m, 50m),
        ["OilPrice"] = (75m, 75m),
        ["GoldPrice"] = (1900m, 1900m),
        ["WageIndex"] = (100m, 100m),
        ["NatGasPrice"] = (3.5m, 3.5m),
    };

    /// <summary>Normalised deviation of a driver from its baseline (0 = normal, +1 = one scale-unit high).
    /// The level-based signal for the Demand and Valuation channels. Unknown/unscaled drivers return 0.</summary>
    public decimal GetDriverDeviation(string driver)
        => DriverNorm.TryGetValue(driver, out var n) && n.Scale != 0m
            ? (GetDriverValue(driver) - n.Baseline) / n.Scale
            : 0m;

    /// <summary>
    /// Driver interdependence (design/EMERGENT_COUPLING.md §7 — the "drivers are independent" realism gap).
    /// A curated set of real macro links so ONE shock ripples through the whole economy:
    /// oil → inflation → rates → growth → unemployment → confidence/PMI. Small daily pushes, each proportional
    /// to the SOURCE driver's deviation from normal and read from the same start-of-day snapshot (so effects
    /// propagate with a one-day lag, not instantly). The chain is ACYCLIC — no loop feeds back to its own
    /// source — so it is stabilising, not runaway; the ±clamps bound everything. Called daily after TickDay.
    /// </summary>
    public void PropagateDriverCoupling()
    {
        // Snapshot deviations up front so within one day the links don't compound on each other.
        var oilDev = GetDriverDeviation("OilPrice");
        var inflDev = GetDriverDeviation("InflationRate");
        var rateDev = GetDriverDeviation("InterestRate");
        var gdpDev = GetDriverDeviation("GDPGrowth");
        var unempDev = GetDriverDeviation("UnemploymentRate");
        var wageDev = GetDriverDeviation("WageIndex");

        Data.InflationRate = Clamp(Data.InflationRate + oilDev * 0.05m + wageDev * 0.02m, -1, 15); // oil + wage cost-push
        Data.InterestRate = Clamp(Data.InterestRate + inflDev * 0.03m, 0, 15);          // central-bank reaction
        Data.GDPGrowth = Clamp(Data.GDPGrowth - rateDev * 0.04m, -5, 8);                // tight money slows growth
        Data.UnemploymentRate = Clamp(Data.UnemploymentRate - gdpDev * 0.03m, 2, 15);   // Okun's law
        Data.WageIndex = Clamp(Data.WageIndex - unempDev * 1.5m, 85, 140);              // tight labour market lifts wages
        Data.ConsumerConfidence = Clamp(Data.ConsumerConfidence + gdpDev * 0.5m - unempDev * 0.5m, 20, 120);
        Data.ManufacturingPMI = Clamp(Data.ManufacturingPMI + gdpDev * 0.3m, 30, 65);   // sentiment follows real economy
        // Oil→gas substitution: emergent gas carries this in its demand (GasOilLink); only nudge here in legacy mode.
        if (!EmergentCommodityPricing)
            Data.NatGasPrice = Clamp(Data.NatGasPrice + oilDev * 0.08m, 1.5m, 15);      // gas loosely tracks oil
    }

    private decimal GetIndicatorValue(string indicator) => indicator switch
    {
        "InterestRate" => Data.InterestRate,
        "InflationRate" => Data.InflationRate,
        "UnemploymentRate" => Data.UnemploymentRate,
        "GDPGrowth" => Data.GDPGrowth,
        "ConsumerConfidence" => Data.ConsumerConfidence,
        "TreasuryYield10Y" => Data.TreasuryYield10Y,
        "OilPrice" => Data.OilPrice,
        "GoldPrice" => Data.GoldPrice,
        "HousingStarts" => Data.HousingStarts,
        "ManufacturingPMI" => Data.ManufacturingPMI,
        "WageIndex" => Data.WageIndex,
        "NatGasPrice" => Data.NatGasPrice,
        _ => 0,
    };

    private void SetIndicatorValue(string indicator, decimal value)
    {
        switch (indicator)
        {
            case "InterestRate": Data.InterestRate = Clamp(value, 0, 15); break;
            case "InflationRate": Data.InflationRate = Clamp(value, -1, 15); break;
            case "UnemploymentRate": Data.UnemploymentRate = Clamp(value, 2, 15); break;
            case "GDPGrowth": Data.GDPGrowth = Clamp(value, -5, 8); break;
            case "ConsumerConfidence": Data.ConsumerConfidence = Clamp(value, 20, 120); break;
            case "TreasuryYield10Y": Data.TreasuryYield10Y = Clamp(value, 0.5m, 10); break;
            case "OilPrice": Data.OilPrice = Clamp(value, 20, 150); break;
            case "GoldPrice": Data.GoldPrice = Clamp(value, 800, 3000); break;
            case "HousingStarts": Data.HousingStarts = Clamp(value, 500, 2000); break;
            case "ManufacturingPMI": Data.ManufacturingPMI = Clamp(value, 30, 65); break;
            case "WageIndex": Data.WageIndex = Clamp(value, 85, 140); break;
            case "NatGasPrice": Data.NatGasPrice = Clamp(value, 1.5m, 15); break;
        }
    }

    // === MONETARY POLICY ENGINE ===

    /// <summary>Policy change events generated this tick (for news feed).</summary>
    public List<string> PolicyEventsThisTick { get; } = new();

    /// <summary>
    /// Evaluate and update monetary policy stance based on economic conditions.
    /// Called daily. Generates news events on policy transitions.
    /// </summary>
    public void UpdateMonetaryPolicy()
    {
        PolicyEventsThisTick.Clear();
        var rate = Data.InterestRate;
        var inflation = Data.InflationRate;
        var gdp = Data.GDPGrowth;
        var unemployment = Data.UnemploymentRate;
        var oldStance = Data.PolicyStance;

        // Evaluate transitions based on macro conditions
        var newStance = oldStance;
        switch (oldStance)
        {
            case MonetaryPolicyStance.Neutral:
                if (inflation > 4m && gdp > 1m)
                    newStance = MonetaryPolicyStance.Tightening;
                else if (gdp < 0.5m && unemployment > 6m)
                    newStance = MonetaryPolicyStance.Easing;
                break;

            case MonetaryPolicyStance.Tightening:
                if (inflation < 2.5m && rate > 4m)
                    newStance = MonetaryPolicyStance.Neutral;
                else if (gdp < 0m) // Recession override
                    newStance = MonetaryPolicyStance.Easing;
                break;

            case MonetaryPolicyStance.Easing:
                if (rate < 0.5m && gdp < 1m)
                    newStance = MonetaryPolicyStance.QE;
                else if (inflation > 3m || gdp > 2.5m)
                    newStance = MonetaryPolicyStance.Neutral;
                break;

            case MonetaryPolicyStance.QE:
                if (gdp > 1.5m && unemployment < 5.5m)
                    newStance = MonetaryPolicyStance.Neutral; // Tapering → Neutral
                else if (inflation > 4m)
                    newStance = MonetaryPolicyStance.Tightening; // Emergency pivot
                break;
        }

        if (newStance != oldStance)
        {
            Data.PolicyStance = newStance;
            var headline = GeneratePolicyHeadline(oldStance, newStance);
            PolicyEventsThisTick.Add(headline);
            _log.Info("Monetary policy changed", new { from = oldStance.ToString(), to = newStance.ToString(), rate, inflation, gdp });
        }

        // Scheduled FOMC meetings every ~30 trading days (≈6 weeks)
        _daysSinceFOMC++;
        if (_daysSinceFOMC >= 30)
        {
            _daysSinceFOMC = 0;
            var decision = Data.PolicyStance switch
            {
                MonetaryPolicyStance.Tightening => _rng.NextDouble() < 0.7 ? "hiked rates by 25 basis points" : "held rates steady but signaled further tightening",
                MonetaryPolicyStance.Easing => _rng.NextDouble() < 0.7 ? "cut rates by 25 basis points" : "held rates but signaled readiness to cut",
                MonetaryPolicyStance.QE => "maintained current rate and announced continued asset purchases",
                _ => "held the federal funds rate unchanged, citing balanced risks",
            };
            var toneWords = Data.PolicyStance switch
            {
                MonetaryPolicyStance.Tightening => new[] { "inflation remains elevated", "committed to price stability", "prepared to act decisively" },
                MonetaryPolicyStance.Easing => new[] { "downside risks have increased", "labor market softening", "monitoring economic conditions closely" },
                MonetaryPolicyStance.QE => new[] { "extraordinary measures remain appropriate", "recovery remains fragile", "will maintain accommodation as long as needed" },
                _ => new[] { "economy is on solid footing", "risks are roughly balanced", "data-dependent approach continues" },
            };
            var tone = toneWords[_rng.Next(toneWords.Length)];

            PolicyEventsThisTick.Add($"FOMC DECISION: Fed {decision}. Statement: \"{tone}.\" Rate: {Data.InterestRate:F2}%");

            // Rate adjustment
            if (decision.Contains("hiked"))
                Data.InterestRate = Clamp(Data.InterestRate + 0.25m, 0, 15);
            else if (decision.Contains("cut"))
                Data.InterestRate = Clamp(Data.InterestRate - 0.25m, 0, 15);

            _log.Info("FOMC meeting", new { decision, rate = Data.InterestRate, stance = Data.PolicyStance.ToString() });
        }

        // Elections: every ~500 trading days (≈2 years)
        _daysSinceElection++;
        if (_daysSinceElection >= 500)
        {
            _daysSinceElection = 0;

            var candidates = new[]
            {
                ("pro-business conservative", new Dictionary<string, decimal>
                {
                    ["Energy"] = 1.02m, ["Financials"] = 1.015m, ["Industrials"] = 1.01m,
                    ["Healthcare"] = 0.99m, ["Utilities"] = 0.99m,
                }),
                ("progressive reformer", new Dictionary<string, decimal>
                {
                    ["Healthcare"] = 1.02m, ["Utilities"] = 1.015m, ["Technology"] = 1.01m,
                    ["Energy"] = 0.985m, ["Financials"] = 0.99m,
                }),
                ("centrist pragmatist", new Dictionary<string, decimal>
                {
                    ["Technology"] = 1.01m, ["Consumer Goods"] = 1.01m,
                    ["Industrials"] = 1.005m,
                }),
                ("populist outsider", new Dictionary<string, decimal>
                {
                    ["Industrials"] = 1.02m, ["Materials"] = 1.015m,
                    ["Technology"] = 0.99m, ["Financials"] = 0.985m,
                }),
            };

            var (platform, sectorShifts) = candidates[_rng.Next(candidates.Length)];
            _currentAdministration = platform;

            // Apply sector shifts to multipliers for the next election cycle
            ElectionSectorShifts = sectorShifts;

            PolicyEventsThisTick.Add(
                $"ELECTION RESULT: New {platform} administration takes office. Markets react to anticipated policy shifts in energy, healthcare, and financial regulation.");

            _log.Info("Election", new { platform, sectors = sectorShifts.Count });
        }

        // Balance sheet drift based on policy
        var bsDrift = Data.PolicyStance switch
        {
            MonetaryPolicyStance.QE => 0.01m + (decimal)_rng.NextDouble() * 0.005m,       // Growing ~$10-15B/day
            MonetaryPolicyStance.Easing => 0.002m + (decimal)_rng.NextDouble() * 0.002m,   // Slight growth
            MonetaryPolicyStance.Tightening => -0.005m - (decimal)_rng.NextDouble() * 0.003m, // QT ~$5-8B/day
            _ => (decimal)(_rng.NextDouble() - 0.5) * 0.001m,                              // Neutral: flat
        };
        Data.FedBalanceSheet = Clamp(Data.FedBalanceSheet + bsDrift, 2m, 12m);
    }

    private string GeneratePolicyHeadline(MonetaryPolicyStance from, MonetaryPolicyStance to)
    {
        return (from, to) switch
        {
            (_, MonetaryPolicyStance.Tightening) => $"FOMC signals hawkish pivot: rate hikes expected as inflation hits {Data.InflationRate:F1}%",
            (_, MonetaryPolicyStance.Easing) => $"Fed pivots dovish: rate cuts on the table as growth slows to {Data.GDPGrowth:F1}%",
            (_, MonetaryPolicyStance.QE) => $"Fed launches emergency QE program — balance sheet expansion begins as rates hit {Data.InterestRate:F2}%",
            (MonetaryPolicyStance.QE, MonetaryPolicyStance.Neutral) => $"Fed announces tapering: balance sheet at ${Data.FedBalanceSheet:F1}T, purchases to wind down",
            (_, MonetaryPolicyStance.Neutral) => $"Fed signals pause — monetary policy enters wait-and-see mode",
            _ => $"Fed monetary policy shifts to {to}",
        };
    }

    /// <summary>
    /// Get sector multipliers from monetary policy stance.
    /// These stack on top of the existing rate-based multipliers.
    /// </summary>
    public Dictionary<string, decimal> GetPolicyMultipliers()
    {
        var m = new Dictionary<string, decimal>();
        var policyFactor = Data.PolicyStance switch
        {
            MonetaryPolicyStance.QE => 1.0m,          // Strong easing
            MonetaryPolicyStance.Easing => 0.4m,       // Mild easing
            MonetaryPolicyStance.Neutral => 0.0m,
            MonetaryPolicyStance.Tightening => -0.6m,  // Hawkish
            _ => 0.0m,
        };

        // QE/Easing: Growth + Real Estate rally, Financials lag (margin compression)
        // Tightening: Financials benefit, Growth/Real Estate suffer
        m["Technology"] = 1m + policyFactor * 0.003m;
        m["Financials"] = 1m - policyFactor * 0.002m;
        m["Real Estate"] = 1m + policyFactor * 0.003m;
        m["Healthcare"] = 1m + policyFactor * 0.001m;
        m["Consumer Goods"] = 1m + policyFactor * 0.001m;
        m["Utilities"] = 1m + policyFactor * 0.002m;
        m["Industrials"] = 1m + policyFactor * 0.001m;
        m["Energy"] = 1m;
        m["Materials"] = 1m + policyFactor * 0.001m;
        m["Telecommunications"] = 1m + policyFactor * 0.001m;
        m["Luxury Goods"] = 1m + policyFactor * 0.002m;
        m["Transportation"] = 1m + policyFactor * 0.001m;
        m["ETF"] = 1m;

        return m;
    }

    // === DOLLAR STRENGTH INDEX ===

    /// <summary>
    /// Update Dollar Index based on interest rate differentials and economic strength.
    /// Called daily. DXY rises with high rates/strong economy, falls with QE/weak economy.
    /// </summary>
    public void UpdateDollarIndex()
    {
        // "Foreign" central bank rate (simplified: tracks Fed with lag + noise)
        var foreignRateProxy = 2.0m; // ECB/BoE/BoJ average baseline

        // Rate differential drives DXY: higher US rates → stronger dollar
        var rateDiff = Data.InterestRate - foreignRateProxy;
        var ratePull = rateDiff * 0.15m; // Each 1% rate advantage → +0.15 DXY/day

        // Policy stance impact: QE weakens dollar, tightening strengthens
        var policyPull = Data.PolicyStance switch
        {
            MonetaryPolicyStance.QE => -0.08m,
            MonetaryPolicyStance.Easing => -0.03m,
            MonetaryPolicyStance.Tightening => 0.05m,
            _ => 0m,
        };

        // Economic strength: strong GDP + low unemployment → strong dollar
        var econPull = (Data.GDPGrowth - 2m) * 0.02m - (Data.UnemploymentRate - 5m) * 0.01m;

        // Random noise
        var noise = (decimal)(_rng.NextDouble() - 0.5) * 0.3m;

        // Mean reversion toward 100
        var meanReversion = (100m - Data.DollarIndex) * 0.005m;

        var totalDrift = ratePull + policyPull + econPull + noise + meanReversion;
        Data.DollarIndex = Clamp(Math.Round(Data.DollarIndex + totalDrift, 2), 80m, 120m);
    }

    /// <summary>
    /// Get sector multipliers from dollar strength.
    /// Strong dollar: hurts exporters, helps importers. Weak dollar: opposite.
    /// </summary>
    public Dictionary<string, decimal> GetDollarMultipliers()
    {
        var m = new Dictionary<string, decimal>();
        // Normalized: 0 = neutral (DXY=100), +1 = very strong (DXY=120), -1 = very weak (DXY=80)
        var dxyFactor = (Data.DollarIndex - 100m) / 20m;

        // Strong dollar hurts exporters (Tech, Industrials, Materials), helps importers (Consumer)
        m["Technology"] = 1m - dxyFactor * 0.002m;       // Big tech has huge international revenue
        m["Industrials"] = 1m - dxyFactor * 0.002m;      // Export-heavy
        m["Materials"] = 1m - dxyFactor * 0.003m;        // Commodities priced in USD
        m["Energy"] = 1m - dxyFactor * 0.003m;           // Oil/gas priced in USD
        m["Consumer Goods"] = 1m + dxyFactor * 0.001m;   // Imports cheaper
        m["Luxury Goods"] = 1m + dxyFactor * 0.001m;     // Import-heavy
        m["Financials"] = 1m + dxyFactor * 0.001m;       // Slightly benefits from strong USD
        m["Healthcare"] = 1m;
        m["Real Estate"] = 1m;
        m["Utilities"] = 1m;
        m["Telecommunications"] = 1m;
        m["Transportation"] = 1m - dxyFactor * 0.001m;   // Fuel costs in USD
        m["ETF"] = 1m;

        return m;
    }

    private decimal Drift(decimal scale) => (decimal)((_rng.NextDouble() - 0.5) * 2) * scale;
    private static decimal Clamp(decimal v, decimal min, decimal max) => Math.Max(min, Math.Min(max, v));
}

public class CommodityPrices
{
    public decimal CrudeOil { get; set; }
    public decimal Gold { get; set; }
    public decimal NatGas { get; set; }
    public decimal Silver { get; set; }
    public decimal Copper { get; set; }
    public decimal Bitcoin { get; set; }
}
