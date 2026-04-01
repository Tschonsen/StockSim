using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Main game loop that orchestrates the simulation.
/// Manages time, ticks the price engine, and coordinates all systems.
/// See Bible section 10.5 for tick architecture.
///
/// Tick order:
///   1. AdvanceTime
///   2. ProcessEvents (future)
///   3. AIDecisions (future)
///   4. CalculatePrices
///   5. UpdateVolume
///   6. CheckMargins (future)
///   7. BuildUpdatePacket
/// </summary>
public class GameLoop
{
    private readonly PriceEngine _priceEngine;
    private readonly EventEngine _eventEngine;
    private readonly AITraderEngine _aiTraderEngine;
    private readonly DividendEngine _dividendEngine;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly EconomicCycleEngine _economicCycle;
    private readonly IPOEngine _ipoEngine;
    private readonly AchievementEngine _achievementEngine;
    private readonly ETFEngine _etfEngine;
    private readonly EconomicEngine _economicEngine;
    private readonly EarningsEngine _earningsEngine;
    private readonly TaxEngine _taxEngine;
    private readonly SMAEngine _smaEngine;
    private readonly RumorEngine _rumorEngine;
    private readonly NarrativeEngine _narrativeEngine;
    private readonly OptionsEngine _optionsEngine;
    private readonly SeasonalityEngine _seasonalityEngine;
    private readonly MemeStockEngine _memeStockEngine;
    private readonly PriceModel _priceModel;
    private readonly Logger _log = new("GameLoop");
    private readonly int _seed;
    private readonly decimal _startingCash;

    public List<Stock> MutableStocks { get; }
    public IReadOnlyList<Stock> Stocks => MutableStocks;
    /// <summary>O(1) stock lookup by symbol.</summary>
    public Dictionary<string, Stock> StocksBySymbol { get; } = new();
    private bool _marketOpenProcessedToday;
    public Dictionary<string, PriceHistory> PriceHistories { get; } = new();
    public Dictionary<string, List<Candle>> DailyHistory { get; } = new();
    public Portfolio Portfolio { get; }
    public OrderEngine OrderEngine { get; }
    public EventEngine EventEngine => _eventEngine;
    public DividendEngine DividendEngine => _dividendEngine;
    public CircuitBreaker CircuitBreaker => _circuitBreaker;
    public EconomicCycleEngine EconomicCycle => _economicCycle;
    public IPOEngine IPOEngine => _ipoEngine;
    public AchievementEngine AchievementEngine => _achievementEngine;
    public ETFEngine ETFEngine => _etfEngine;
    public EconomicEngine EconomicEngine => _economicEngine;
    public EarningsEngine EarningsEngine => _earningsEngine;
    public AITraderEngine AITraderEngine => _aiTraderEngine;
    public TaxEngine TaxEngine => _taxEngine;
    public SMAEngine SMAEngine => _smaEngine;
    public MemeStockEngine MemeStockEngine => _memeStockEngine;
    public RumorEngine RumorEngine => _rumorEngine;
    public PriceEngine PriceEngine => _priceEngine;
    public NarrativeEngine NarrativeEngine => _narrativeEngine;
    public OptionsEngine OptionsEngine => _optionsEngine;
    public PlayerReputation Reputation { get; } = new();
    public decimal StartingCash => _startingCash;
    public Scenario? ActiveScenario { get; set; }
    public ScenarioResult? ScenarioResult { get; private set; }
    public bool IsBankrupt { get; set; }
    public string PlayerName { get; set; } = "Trader";
    public List<StockSplitEvent> SplitsThisTick { get; } = new();
    public bool MarginCallThisTick { get; set; }
    public List<InsiderTradeEvent> InsiderTradesThisTick { get; } = new();
    /// <summary>Short squeeze warnings generated this tick (Bible 4.4.5).</summary>
    public List<ShortSqueezeWarning> ShortSqueezeWarningsThisTick { get; } = new();
    /// <summary>Rolling price tracker: symbol → price 60 ticks ago (for short squeeze detection).</summary>
    private readonly Dictionary<string, Queue<decimal>> _priceHistory60 = new();
    /// <summary>Track last reverse split date per symbol to prevent split loops (60-day cooldown).</summary>
    private readonly Dictionary<string, DateTime> _lastReverseSplit = new();
    public MarketPhase Phase { get; }
    public DateTime GameTime { get; set; }
    public GameSpeed Speed { get; private set; } = GameSpeed.Paused;
    /// <summary>Skip weekends automatically (Bible 16.2). Fast-forward to Monday 9:00.</summary>
    public bool SkipWeekends { get; set; }
    /// <summary>Auto-pause preferences (Bible 16.2). Configurable from frontend settings.</summary>
    public bool AutoPauseOnShortSqueeze { get; set; } = true;
    public bool AutoPauseOnSMA { get; set; } = true;
    public bool AutoPauseOnNews { get; set; } = true;
    public bool AutoPauseOnMarginCall { get; set; } = true;
    public bool AutoPauseOnMarketOpen { get; set; } = false;
    public bool AutoPauseOnOrderExecution { get; set; } = false;
    public bool AutoPauseOnAlert { get; set; } = true;
    public bool IsPaused => Speed == GameSpeed.Paused;
    /// <summary>Orders filled this tick by automatic execution (limit/stop/pending). Cleared each tick.</summary>
    public List<Order> OrdersFilledThisTick { get; } = new();
    public long TickCount { get; private set; }

    private static readonly string[] Sectors = new[]
    {
        "Technology", "Energy", "Financials", "Healthcare",
        "Consumer Goods", "Industrials", "Materials", "Real Estate",
        "Telecommunications", "Utilities", "Luxury Goods", "Transportation"
    };

    /// <summary>Subsectors per sector for deeper categorization and more unique companies.</summary>
    private static readonly Dictionary<string, string[]> Subsectors = new()
    {
        ["Technology"] = new[] { "Software", "Semiconductors", "Cloud Computing", "Cybersecurity", "AI & Machine Learning", "Consumer Electronics", "Enterprise SaaS", "Gaming" },
        ["Energy"] = new[] { "Oil & Gas", "Renewable Energy", "Solar", "Wind", "Nuclear", "Utilities Infrastructure", "Energy Storage" },
        ["Financials"] = new[] { "Banks", "Insurance", "Asset Management", "FinTech", "Payment Processing", "Private Equity", "Mortgage & Lending" },
        ["Healthcare"] = new[] { "Pharmaceuticals", "Biotechnology", "Medical Devices", "Health Insurance", "Telehealth", "Diagnostics", "Hospital & Clinics" },
        ["Consumer Goods"] = new[] { "Food & Beverage", "Retail", "E-Commerce", "Apparel", "Home & Garden", "Personal Care", "Pet Industry" },
        ["Industrials"] = new[] { "Aerospace & Defense", "Construction", "Machinery", "Waste Management", "Engineering", "3D Printing", "Robotics" },
        ["Materials"] = new[] { "Mining", "Chemicals", "Steel", "Packaging", "Construction Materials", "Rare Earth", "Forestry" },
        ["Real Estate"] = new[] { "Commercial REIT", "Residential REIT", "Data Center REIT", "Healthcare REIT", "Industrial REIT", "PropTech" },
        ["Telecommunications"] = new[] { "Wireless", "Broadband", "Social Media", "Streaming", "Advertising Tech", "5G Infrastructure" },
        ["Utilities"] = new[] { "Electric Utilities", "Water Utilities", "Gas Utilities", "Renewable Utilities", "Waste & Recycling" },
        ["Luxury Goods"] = new[] { "Fashion & Apparel", "Jewelry & Watches", "Automotive Luxury", "Spirits & Wine", "Travel & Hospitality" },
        ["Transportation"] = new[] { "Airlines", "Shipping", "Trucking", "Rail", "Ride-Sharing", "Electric Vehicles", "Logistics" },
    };

    public GameLoop(int seed, int stockCount = 500, decimal startingCash = 50_000m)
    {
        _seed = seed;
        _startingCash = startingCash;
        _priceEngine = new PriceEngine(seed);
        var templateLoader = new TemplateLoader();
        templateLoader.LoadAll();
        _eventEngine = new EventEngine(seed + 5000, templateLoader);
        _aiTraderEngine = new AITraderEngine(seed + 7000);
        _dividendEngine = new DividendEngine();
        _circuitBreaker = new CircuitBreaker();
        _economicCycle = new EconomicCycleEngine(seed + 9000);
        _ipoEngine = new IPOEngine(seed + 11000);
        _achievementEngine = new AchievementEngine();
        _etfEngine = new ETFEngine();
        _economicEngine = new EconomicEngine(seed + 13000);
        _earningsEngine = new EarningsEngine(seed + 15000);
        _taxEngine = new TaxEngine();
        _smaEngine = new SMAEngine(seed + 17000);
        _rumorEngine = new RumorEngine(seed + 19000);
        _narrativeEngine = new NarrativeEngine(seed + 21000);
        _narrativeEngine.LoadArcs(templateLoader.DataPath);
        _optionsEngine = new OptionsEngine(seed + 23000);
        _seasonalityEngine = new SeasonalityEngine();
        _memeStockEngine = new MemeStockEngine(seed + 25000);

        // ONNX Price Model (Session 22-23): load if available, fallback to pure GBM
        _priceModel = new PriceModel();
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var mlDir = Directory.Exists(Path.Combine(baseDir, "ml"))
            ? Path.Combine(baseDir, "ml")
            : Path.Combine(baseDir, "..", "..", "..", "..", "..", "ml");
        var modelPath = Path.Combine(mlDir, "price_model.onnx");
        var scalerPath = Path.Combine(mlDir, "scaler_params.json");
        if (_priceModel.Load(modelPath, scalerPath))
            _priceEngine.SetOnnxModel(_priceModel);

        // Start on a Monday at market pre-open
        GameTime = new DateTime(2027, 1, 4, 9, 0, 0); // Mon, Jan 4 2027

        // Initialize portfolio and order engine (Bible 4.1)
        Portfolio = new Portfolio(startingCash);
        OrderEngine = new OrderEngine(Portfolio) { TaxEngine = _taxEngine };

        // Determine market phase (Bible 11.4: Bull 40%, Neutral 40%, Bear 20%)
        Phase = HistoryGenerator.DeterminePhase(seed);

        var stocks = GenerateStocks(seed, stockCount);
        MutableStocks = stocks;
        foreach (var s in stocks) StocksBySymbol[s.Symbol] = s;

        // Initialize price history for each stock (1-minute candles)
        foreach (var stock in stocks)
        {
            PriceHistories[stock.Symbol] = new PriceHistory(stock.Symbol, CandleInterval.OneMinute);
        }

        // Generate 252 trading days of historical daily candles (Bible 11.4)
        GenerateHistoricalPrices(seed, stocks);

        // Generate company personalities (Session 12: CEO, products, stories, rivalries)
        CompanyPersonalityGenerator.GenerateAll(stocks, seed);

        // Initialize analyst ratings and target prices
        foreach (var stock in stocks)
        {
            var ratingRng = new Random(seed + stock.Symbol.GetHashCode());
            stock.AnalystRating = Math.Round((decimal)(ratingRng.NextDouble() * 3 + 2), 1); // 2.0-5.0
            stock.TargetPrice = Math.Round(stock.CurrentPrice * (decimal)(0.8 + ratingRng.NextDouble() * 0.5), 2); // ±20-30%
        }

        // Schedule initial economic events and earnings
        _economicEngine.ScheduleEvents(GameTime);
        _earningsEngine.GenerateSchedule(stocks, GameTime);

        // Create ETFs based on generated stocks
        var etfs = _etfEngine.CreateETFs(stocks);
        foreach (var etf in etfs)
        {
            MutableStocks.Add(etf);
            StocksBySymbol[etf.Symbol] = etf;
            PriceHistories[etf.Symbol] = new PriceHistory(etf.Symbol, CandleInterval.OneMinute);
        }

        // Create commodity ETFs (GLD, SLV, USO) tracking economic indicators
        var commodityETFs = _etfEngine.CreateCommodityETFs(_economicEngine.Data);
        foreach (var cetf in commodityETFs)
        {
            MutableStocks.Add(cetf);
            StocksBySymbol[cetf.Symbol] = cetf;
            PriceHistories[cetf.Symbol] = new PriceHistory(cetf.Symbol, CandleInterval.OneMinute);
        }

        // Generate historical prices for all ETFs (Bug fix: ETFs had no chart data)
        var allETFs = etfs.Concat(commodityETFs).ToList();
        for (int i = 0; i < allETFs.Count; i++)
        {
            var historyGen = new HistoryGenerator(seed: seed + stocks.Count + i + 2000);
            var candles = historyGen.GenerateDaily(allETFs[i], GameTime, Phase);
            DailyHistory[allETFs[i].Symbol] = candles;
        }

        // Wire up sector lookup for achievements
        _achievementEngine.SetSectorLookup(sym =>
            StocksBySymbol.TryGetValue(sym, out var s) ? s.Sector : "Unknown");

        // Wire scenario rules to order engine
        OrderEngine.ActiveScenario = ActiveScenario;

        // History Mode: force-activate the corresponding Tier-4 arc at game start
        if (ActiveScenario?.ForceArcId != null)
        {
            _narrativeEngine.ForceActivateArc(ActiveScenario.ForceArcId, GameTime);
        }

        // Wire up cancellation tracking for SMA spoofing detection (Bible 9.3.3)
        OrderEngine.OnOrderCancelled += order =>
        {
            _smaEngine.RecordCancellation(order.Symbol, order.Quantity,
                order.LimitPrice ?? order.FillPrice ?? 0m,
                order.PlacedAt, GameTime);
        };

        // Wire up trade recording for journal + achievements + taxes
        OrderEngine.OnTradeCompleted += trade =>
        {
            // Fill in sector from stock data
            if (StocksBySymbol.TryGetValue(trade.Symbol, out var tradeStock))
                trade.Sector = tradeStock.Sector;
            _achievementEngine.RecordTrade(trade);

            // Record trade for SMA surveillance
            var side = trade.Side == "Long" ? OrderSide.Buy : OrderSide.Short;
            _smaEngine.RecordOrder(trade.Symbol, side, trade.Quantity, trade.EntryPrice, trade.EntryTime, true);
            var closeSide = trade.Side == "Long" ? OrderSide.Sell : OrderSide.Cover;
            _smaEngine.RecordOrder(trade.Symbol, closeSide, trade.Quantity, trade.ExitPrice, trade.ExitTime, true);

            // Calculate and deduct tax (with wash sale tracking)
            var tax = _taxEngine.CalculateTradeTax(trade.PnL, trade.HoldingDays, trade.Symbol, trade.ExitTime);
            if (tax > 0)
            {
                Portfolio.Cash -= tax;
            }
        };

        // Generate initial news so the feed isn't empty at game start
        _eventEngine.GenerateInitialNews(Stocks, GameTime);

        // Schedule initial dividends so players see them in the first 2-3 weeks
        _dividendEngine.ScheduleInitialDividends(Stocks, GameTime);

        // Generate option chains for eligible stocks
        _optionsEngine.GenerateChains(Stocks, GameTime);

        _log.Info("GameLoop initialized", new
        {
            seed,
            stockCount,
            phase = Phase.ToString(),
            sectors = stocks.Select(s => s.Sector).Distinct().Count(),
            gameTime = GameTime,
        });
    }

    public void SetSpeed(GameSpeed speed)
    {
        var oldSpeed = Speed;
        Speed = speed;
        _log.Info("Speed changed", new { from = oldSpeed.ToString(), to = speed.ToString() });
    }

    /// <summary>
    /// Execute one simulation tick. Called by the game loop timer.
    /// </summary>
    public void ExecuteTick()
    {
        if (IsPaused) return;

        // Clear all per-tick lists BEFORE any early returns (prevents stale data resending)
        _eventEngine.NewEventsThisTick.Clear();
        _eventEngine.MAndAEventsThisTick.Clear();
        _aiTraderEngine.NewsThisTick.Clear();
        _aiTraderEngine.MarginCascadeNewsThisTick.Clear();
        _earningsEngine.ReleasedThisTick.Clear();
        _earningsEngine.GuidanceThisTick.Clear();
        _economicEngine.ReleasedThisTick.Clear();
        _economicEngine.PolicyEventsThisTick.Clear();
        _dividendEngine.NewAnnouncementsThisTick.Clear();
        _dividendEngine.PaymentsThisTick.Clear();
        _ipoEngine.NewIPOsThisTick.Clear();
        _ipoEngine.DelistedThisTick.Clear();
        _ipoEngine.NewsThisTick.Clear();
        _smaEngine.NotificationsThisTick.Clear();
        _circuitBreaker.NewHaltsThisTick.Clear();
        _rumorEngine.NewRumorsThisTick.Clear();
        _rumorEngine.RumorEventsThisTick.Clear();
        _narrativeEngine.NewEventsThisTick.Clear();
        _etfEngine.RebalanceNewsThisTick.Clear();
        _optionsEngine.GexNewsThisTick.Clear();
        _memeStockEngine.NewsThisTick.Clear();
        _memeStockEngine.MemePressure.Clear();
        InsiderTradesThisTick.Clear();
        ShortSqueezeWarningsThisTick.Clear();
        SplitsThisTick.Clear();
        OrdersFilledThisTick.Clear();

        // 1. Advance game time by 1 minute
        GameTime = GameTime.AddMinutes(1);

        // Skip weekends: jump to Monday 9:00 (Bible 16.2)
        if (SkipWeekends && (GameTime.DayOfWeek == DayOfWeek.Saturday || GameTime.DayOfWeek == DayOfWeek.Sunday))
        {
            while (GameTime.DayOfWeek == DayOfWeek.Saturday || GameTime.DayOfWeek == DayOfWeek.Sunday)
                GameTime = GameTime.AddDays(1);
            GameTime = GameTime.Date.Add(new TimeSpan(9, 0, 0));
        }

        // Fast-forward overnight at all speeds: skip 20:00→9:00
        // Saves ~660 useless ticks per day (nothing happens overnight)
        if (Speed > GameSpeed.Paused && GameTime.DayOfWeek != DayOfWeek.Saturday && GameTime.DayOfWeek != DayOfWeek.Sunday)
        {
            var time = GameTime.TimeOfDay;
            if (time >= new TimeSpan(20, 0, 0) || time < new TimeSpan(9, 0, 0))
            {
                // Jump to 9:00 (next day if after 20:00, same day if before 9:00)
                if (time >= new TimeSpan(20, 0, 0))
                    GameTime = GameTime.Date.AddDays(1).Add(new TimeSpan(9, 0, 0));
                else
                    GameTime = GameTime.Date.Add(new TimeSpan(9, 0, 0));
            }
        }

        // Reset market-open flag before market opens (must be before early return)
        if (GameTime.TimeOfDay < new TimeSpan(9, 30, 0))
            _marketOpenProcessedToday = false;

        // Check bankruptcy every tick (even when market closed)
        // Bankrupt if: no positions/options and no cash, OR total equity <= 0
        if (TickCount > 0 && !IsBankrupt)
        {
            Func<string, decimal> getBankruptPrice = sym =>
                StocksBySymbol.TryGetValue(sym, out var s) ? s.CurrentPrice : 0m;
            var totalEquity = Portfolio.TotalEquity(getBankruptPrice);

            // Include options value in bankruptcy check
            var optionsValue = 0m;
            foreach (var op in _optionsEngine.Positions)
            {
                if (_optionsEngine.Chains.TryGetValue(op.UnderlyingSymbol, out var chain))
                {
                    var contract = chain.AllContracts.FirstOrDefault(c => c.Id == op.ContractId);
                    if (contract != null)
                        optionsValue += (contract.BidPrice + contract.AskPrice) / 2 * Models.OptionContract.Multiplier * op.Quantity;
                }
            }
            totalEquity += optionsValue;

            var hasNoAssets = Portfolio.Positions.Count == 0 && _optionsEngine.Positions.Count == 0;

            if ((Portfolio.Cash <= 0 && hasNoAssets) || totalEquity <= 0)
            {
                IsBankrupt = true;
                SetSpeed(GameSpeed.Paused);
            }
        }

        // 2. Check if market is open or in after-hours session
        if (!IsMarketOpen() && !IsAfterHours())
        {
            TickCount++;
            return;
        }

        // After-hours: reduced price movement only (no events, no daily processing)
        if (IsAfterHours() && !IsMarketOpen())
        {
            var ahTickDuration = TimeSpan.FromMinutes(1);
            _priceEngine.GenerateSectorShocks(Stocks.Select(s => s.Sector).Distinct());
            _priceEngine.CurrentDayTick = 390; // After regular hours
            foreach (var stock in Stocks)
            {
                // Reduced volatility during after-hours (30% of normal)
                var origVol = stock.BaseVolatility;
                stock.BaseVolatility *= 0.3m;
                _priceEngine.Tick(stock, ahTickDuration);
                stock.BaseVolatility = origVol;

                // Check limit orders only (market orders rejected in after-hours)
                OrdersFilledThisTick.AddRange(OrderEngine.CheckLimitOrders(stock, GameTime, isMarketOpen: true));
            }
            TickCount++;
            return;
        }

        // 3. At first market tick: apply gap, reset daily values, execute pending orders
        if (!_marketOpenProcessedToday && GameTime.TimeOfDay >= new TimeSpan(9, 31, 0))
        {
            foreach (var stock in Stocks)
            {
                // Daily mean reversion at open: pull price toward fair value
                // Prevents sustained compound drift (±3%/day × 17 days = ±40%)
                ApplyDailyMeanReversion(stock);
                // Set PreviousClose BEFORE gap so daily clamp references pre-gap price
                _priceEngine.ResetDailyValues(stock);
                // Gap Up/Down: overnight news causes price to jump at open (Bible 20.2)
                ApplyOpeningGap(stock);
                OrdersFilledThisTick.AddRange(OrderEngine.ExecutePendingOrders(stock, GameTime, isMarketOpen: true));
            }
            // Seasonality: calendar-based market effects
            _seasonalityEngine.TickDay(GameTime);
            if (_seasonalityEngine.SeasonalHeadline != null)
            {
                _eventEngine.InjectEvent(new Models.GameEvent
                {
                    Type = Models.EventType.Macro,
                    Severity = Models.EventSeverity.Minor,
                    Headline = _seasonalityEngine.SeasonalHeadline,
                    Summary = _seasonalityEngine.SeasonalHeadline,
                    Sentiment = 0f,
                    PriceEffect = 0f,
                    TriggeredAt = GameTime,
                    AffectedSymbols = new(),
                    AffectedSectors = new(),
                    Tags = new() { "seasonal", "calendar" },
                });
            }
            // Economic cycle: daily sector rotation (Bible 5.9)
            _economicCycle.TickDay(Stocks);
            // Macro economy: daily indicator drift + data releases
            _economicEngine.TickDay(GameTime);
            // Monetary policy evaluation + Dollar Index update
            _economicEngine.UpdateMonetaryPolicy();
            _economicEngine.UpdateDollarIndex();
            // Feed policy stress to AI traders for margin cascade
            _aiTraderEngine.PolicyStressFactor = _economicEngine.Data.PolicyStance switch
            {
                Models.MonetaryPolicyStance.Tightening => 0.4f,
                Models.MonetaryPolicyStance.Neutral => 0f,
                Models.MonetaryPolicyStance.Easing => -0.1f,
                Models.MonetaryPolicyStance.QE => -0.2f,
                _ => 0f,
            };
            // Update VIX (Market Volatility Index)
            _economicEngine.UpdateVolatilityIndex(Stocks, _eventEngine.ActiveEvents.Count, _aiTraderEngine.HedgeFundStress);
            // Stock splits: check for split candidates
            CheckStockSplits();
            // Insider trading activity
            CheckInsiderActivity();
            // Reset ETF daily values
            _etfEngine.ResetDailyValues();
            // Index rebalancing: quarterly constituent changes + flow effects
            _etfEngine.TickRebalancing(StocksBySymbol, (int)TickCount / 390);
            // Clear expired SSR restrictions (Bible 4.4.2)
            ClearExpiredSSR();
            // IPO/Delisting (Bible 8.2.8)
            _ipoEngine.TickDay(MutableStocks, Portfolio, GameTime);
            // Realism: recalculate FairValue from fundamentals daily
            RecalculateFairValues();
            // ONNX: generate daily price predictions (Session 23)
            _priceEngine.GenerateDailyOnnxPredictions(Stocks, DailyHistory);
            // Options: reprice chains, handle expirations
            _optionsEngine.RiskFreeRate = (double)_economicEngine.Data.TreasuryYield10Y / 100.0;
            _optionsEngine.TickDay(Stocks, GameTime);
            // Meme stock dynamics: scan for candidates + advance active events
            _memeStockEngine.TickDay(Stocks, GameTime, _aiTraderEngine.RetailSentiment);
            _marketOpenProcessedToday = true;

            // Auto-pause at market open (Bible 16.2)
            if (AutoPauseOnMarketOpen)
                SetSpeed(GameSpeed.Paused);
        }

        // 4. Update all stock prices and record candle data
        var tickDuration = TimeSpan.FromMinutes(1);
        var unixTime = new DateTimeOffset(GameTime).ToUnixTimeSeconds();

        // Generate sector-level correlation shocks (Bible 5.6)
        _priceEngine.GenerateSectorShocks(Stocks.Select(s => s.Sector).Distinct());

        // Track intraday tick for U-shaped volume curve
        var marketOpenTick = GameTime.TimeOfDay - new TimeSpan(9, 30, 0);
        _priceEngine.CurrentDayTick = Math.Max(0, (int)marketOpenTick.TotalMinutes);

        // Feed market stress from hedge fund stress (affects correlation + spreads)
        _priceEngine.MarketStress = _aiTraderEngine.HedgeFundStress;
        _priceEngine.Phase = Phase;

        // Feed runtime modifiers for ONNX hybrid blend
        _priceEngine.MarketSentiment = _economicEngine.GetMarketSentiment();
        _priceEngine._allStocks = Stocks;
        var sectorMults = _economicEngine.GetSectorMultipliers();
        var policyMults = _economicEngine.GetPolicyMultipliers();
        var dollarMults = _economicEngine.GetDollarMultipliers();
        _priceEngine.SectorMultipliers.Clear();
        foreach (var (sector, mult) in sectorMults)
        {
            var combined = mult
                * policyMults.GetValueOrDefault(sector, 1m)
                * dollarMults.GetValueOrDefault(sector, 1m)
                * _seasonalityEngine.SectorMultipliers.GetValueOrDefault(sector, 1m);
            _priceEngine.SectorMultipliers[sector] = combined;
        }

        // Build per-stock event sentiment + volatility + volume + max severity from active events
        _priceEngine.StockEventSentiment.Clear();
        _priceEngine.StockEventVolMultiplier.Clear();
        _priceEngine.StockEventVolumeMult.Clear();
        _priceEngine.StockMaxEventSeverity.Clear();
        foreach (var evt in _eventEngine.ActiveEvents)
        {
            foreach (var sym in evt.AffectedSymbols)
            {
                // Accumulate sentiment (clamped to -1..+1)
                _priceEngine.StockEventSentiment.TryGetValue(sym, out var curSent);
                _priceEngine.StockEventSentiment[sym] = Math.Clamp(curSent + evt.Sentiment * 0.3f, -1f, 1f);

                // Max volatility multiplier across active events
                _priceEngine.StockEventVolMultiplier.TryGetValue(sym, out var curVol);
                _priceEngine.StockEventVolMultiplier[sym] = Math.Max(curVol, evt.VolatilityMultiplier);

                // Max volume multiplier across active events
                _priceEngine.StockEventVolumeMult.TryGetValue(sym, out var curVolMult);
                _priceEngine.StockEventVolumeMult[sym] = Math.Max(curVolMult, evt.VolumeMultiplier);

                // Max severity across active events (widens daily clamp)
                _priceEngine.StockMaxEventSeverity.TryGetValue(sym, out var curSev);
                _priceEngine.StockMaxEventSeverity[sym] = Math.Max(curSev, (int)evt.Severity);
            }
            // Sector/Macro events: apply severity to all stocks in affected sectors
            if (evt.Type == EventType.Macro || evt.Type == EventType.Sector)
            {
                foreach (var stock in Stocks)
                {
                    if (evt.Type == EventType.Macro || evt.AffectedSectors.Contains(stock.Sector))
                    {
                        _priceEngine.StockMaxEventSeverity.TryGetValue(stock.Symbol, out var curSev2);
                        _priceEngine.StockMaxEventSeverity[stock.Symbol] = Math.Max(curSev2, (int)evt.Severity);
                    }
                }
            }
        }

        foreach (var stock in Stocks)
        {
            _priceEngine.Tick(stock, tickDuration);

            // Record candle data
            if (PriceHistories.TryGetValue(stock.Symbol, out var history))
            {
                history.UpdateTick(stock.CurrentPrice, unixTime, stock.DayVolume);
            }

            // 5. SSR check: activate if stock falls ≥10% from PreviousClose (Bible 4.4.2)
            CheckSSRActivation(stock);

            // 5b. Short squeeze detection (Bible 4.4.5)
            CheckShortSqueeze(stock);

            // 6. Check stop orders and limit orders against updated prices
            OrdersFilledThisTick.AddRange(OrderEngine.CheckStopOrders(stock, GameTime, isMarketOpen: true));
            OrdersFilledThisTick.AddRange(OrderEngine.CheckLimitOrders(stock, GameTime, isMarketOpen: true));
        }

        // 6. Process events (Bible 8.1)
        _eventEngine.Tick(Stocks, GameTime, isMarketOpen: true);

        // 7. Circuit breaker check (Bible 8.2.8)
        _circuitBreaker.Tick(Stocks, GameTime);

        // 8. Dividends: announcements, ex-date price drops, payments
        _dividendEngine.Tick(Stocks, Portfolio, GameTime);

        // Track dividend payments for achievements
        foreach (var payment in _dividendEngine.PaymentsThisTick)
        {
            if (payment.NetDividend > 0)
                _achievementEngine.RecordDividendReceived(payment.GrossDividend);
        }

        // 8. AI Traders: adjust spreads, volume, sentiment pressure (Bible 7)
        _aiTraderEngine.CurrentDayTick = _priceEngine.CurrentDayTick;
        _aiTraderEngine.EventAffectedSymbols.Clear();
        _aiTraderEngine.EventAffectedSectors.Clear();
        _aiTraderEngine.EventSeverityBySymbol.Clear();
        foreach (var evt in _eventEngine.ActiveEvents)
        {
            foreach (var sym in evt.AffectedSymbols) {
                _aiTraderEngine.EventAffectedSymbols.Add(sym);
                if (!_aiTraderEngine.EventSeverityBySymbol.TryGetValue(sym, out var cur) || evt.Severity > cur)
                    _aiTraderEngine.EventSeverityBySymbol[sym] = evt.Severity;
            }
            foreach (var sec in evt.AffectedSectors) _aiTraderEngine.EventAffectedSectors.Add(sec);
        }
        _aiTraderEngine.Tick(Stocks, _eventEngine.ActiveEvents, isMarketOpen: true);

        // 8b. Re-apply daily clamp after AI Trader (AI modifies prices directly)
        foreach (var stock in Stocks)
        {
            if (stock.PreviousClose > 0 && !stock.Traits.Contains("ETF"))
            {
                var maxPrice = stock.PreviousClose * 1.03m;
                var minPrice = stock.PreviousClose * 0.97m;
                stock.CurrentPrice = Math.Clamp(stock.CurrentPrice, minPrice, maxPrice);
            }
        }

        // 9. Update ETF prices based on constituent stocks
        _etfEngine.UpdatePrices(StocksBySymbol, _economicEngine.Data);
        _etfEngine.ApplyFlowPressure(StocksBySymbol);

        // Options GEX pressure: dealer hedging amplifies/dampens price moves
        foreach (var (sym, pressure) in _optionsEngine.GexPressure)
        {
            if (StocksBySymbol.TryGetValue(sym, out var gexStock))
            {
                var gexMove = gexStock.CurrentPrice * pressure;
                gexStock.CurrentPrice = Math.Max(0.01m, Math.Round(gexStock.CurrentPrice + gexMove, 2));
            }
        }

        // Meme stock pressure: spread daily pressure across 390 ticks
        foreach (var (sym, dailyPressure) in _memeStockEngine.MemePressure)
        {
            if (StocksBySymbol.TryGetValue(sym, out var memeStock))
            {
                var tickPressure = memeStock.CurrentPrice * dailyPressure / 390m;
                memeStock.CurrentPrice = Math.Max(0.01m, Math.Round(memeStock.CurrentPrice + tickPressure, 2));
            }
        }

        MarginCallThisTick = false;

        // 8. Expire day orders at market close + record equity + check achievements
        if (GameTime.TimeOfDay == new TimeSpan(16, 0, 0))
        {
            OrderEngine.ExpireDayOrders();

            // Update rolling returns for autocorrelation (momentum + mean reversion)
            foreach (var stock in MutableStocks)
            {
                if (stock.PreviousClose > 0)
                {
                    var dayReturn = (stock.CurrentPrice - stock.PreviousClose) / stock.PreviousClose;
                    // Exponential moving average of returns (5-day and 20-day approx)
                    stock.Return5Day = stock.Return5Day * 0.8m + dayReturn * 0.2m;   // ~5-day EMA
                    stock.Return20Day = stock.Return20Day * 0.95m + dayReturn * 0.05m; // ~20-day EMA
                }
            }

            // Append today's candle to DailyHistory (for ONNX model)
            var todayUnix = new DateTimeOffset(GameTime).ToUnixTimeSeconds();
            foreach (var stock in MutableStocks)
            {
                if (DailyHistory.TryGetValue(stock.Symbol, out var history))
                {
                    history.Add(new Candle(todayUnix, stock.PreviousClose, stock.DayHigh, stock.DayLow, stock.CurrentPrice, stock.DayVolume));
                    if (history.Count > 300) history.RemoveAt(0); // Keep rolling window
                }
            }

            // Daily charges: short borrow fees + margin interest
            ChargeDailyFees();

            // Shareholder Vote: if player holds >5% of any stock, occasional vote opportunities
            CheckShareholderVotes();

            // Gradual fundamental drift between quarterly earnings
            DriftFundamentals();

            // Player reputation: update influence and scrutiny daily
            {
                Func<string, decimal> repGetPrice = sym =>
                    StocksBySymbol.TryGetValue(sym, out var s) ? s.CurrentPrice : 0m;
                var portfolioVal = Portfolio.TotalEquity(repGetPrice);
                var tradesToday = OrdersFilledThisTick.Count;
                var hasSMAViolation = _smaEngine.State.Violations.Count > 0
                    && _smaEngine.State.Violations.Any(v => v.DetectedAt.Date == GameTime.Date);
                Reputation.UpdateDaily(portfolioVal, tradesToday, Portfolio.TradeCount, Portfolio.RealizedPnL, hasSMAViolation);
            }

            // Cleanup expired wash sale entries (>30 days old)
            _taxEngine.CleanupExpiredWashSales(GameTime);

            // Process earnings at market close
            _earningsEngine.TickDay(Stocks, GameTime);

            // CEO Performance Check: fire CEO after consecutive misses
            foreach (var report in _earningsEngine.ReleasedThisTick)
            {
                var stock = StocksBySymbol.GetValueOrDefault(report.Symbol);
                if (stock?.Personality == null) continue;

                // Track consecutive misses (simple: negative surprise = miss)
                if (report.ActualEPS < report.ExpectedEPS * 0.9m) // Miss by >10%
                {
                    stock.Personality.ConsecutiveMisses = (stock.Personality.ConsecutiveMisses ?? 0) + 1;
                }
                else
                {
                    stock.Personality.ConsecutiveMisses = 0;
                }

                // Fire CEO after 3 consecutive misses
                if ((stock.Personality.ConsecutiveMisses ?? 0) >= 3)
                {
                    var oldCEO = stock.Personality.CEOName;
                    var oldArchetype = stock.Personality.CEOArchetype;

                    // Generate new CEO
                    var rng = new Random(_seed + (int)TickCount + stock.Symbol.GetHashCode());
                    var archetypes = new[] { "Turnaround Artist", "Cost-Cutter", "Finance Veteran", "Industry Insider", "Steady Hand" };
                    stock.Personality.CEOArchetype = archetypes[rng.Next(archetypes.Length)];
                    stock.Personality.CEOName = $"New CEO"; // Will be properly named
                    stock.Personality.ConsecutiveMisses = 0;

                    // Generate news event
                    _eventEngine.InjectEvent(new Models.GameEvent
                    {
                        Type = Models.EventType.Company,
                        Severity = Models.EventSeverity.Major,
                        Sentiment = 0.1f,
                        Headline = $"BREAKING: {stock.Name} fires CEO {oldCEO} ({oldArchetype}) after 3 consecutive earnings misses. Board appoints {stock.Personality.CEOArchetype} as interim leadership.",
                        PriceEffect = 0.02f + (float)rng.NextDouble() * 0.03f,
                        VolatilityMultiplier = 1.8f, VolumeMultiplier = 3.0f,
                        DurationMinutes = 120, RemainingMinutes = 120,
                        AffectedSymbols = new() { stock.Symbol },
                        AffectedSectors = new() { stock.Sector },
                        TriggeredAt = GameTime,
                        Tags = new() { "ceo_fired", "management" },
                    });

                    _log.Info("CEO fired", new { symbol = stock.Symbol, oldCEO, oldArchetype, newArchetype = stock.Personality.CEOArchetype });
                }
            }

            // IV Crush: slash option IV after earnings release
            foreach (var report in _earningsEngine.ReleasedThisTick)
                _optionsEngine.ApplyIVCrush(report.Symbol, GameTime);

            // === REALISM BATCH 2: Process insolvency warnings from earnings ===
            foreach (var warning in _earningsEngine.InsolvencyWarnings)
            {
                // Schedule delisting in 10 trading days
                if (!_ipoEngine.PendingDelistings.Any(d => d.Symbol == warning.Symbol))
                {
                    var delistDate = GameTime.AddDays(14); // ~10 trading days
                    while (delistDate.DayOfWeek == DayOfWeek.Saturday || delistDate.DayOfWeek == DayOfWeek.Sunday)
                        delistDate = delistDate.AddDays(1);
                    _ipoEngine.PendingDelistings.Add(new PendingDelisting { Symbol = warning.Symbol, DelistDate = delistDate });
                    var stock = StocksBySymbol.GetValueOrDefault(warning.Symbol);
                    var headline = $"BANKRUPTCY: {stock?.Name ?? warning.Symbol} ({warning.Symbol}) files for Chapter 11. Trading suspended in 10 days.";
                    _ipoEngine.NewsThisTick.Add(headline);
                    _log.Warn("Company insolvency → delisting scheduled", new { symbol = warning.Symbol, delistDate = delistDate.ToString("yyyy-MM-dd"), reason = warning.Reason });
                }
            }

            // Rumors: generate hints and fire pending rumor events (Bible 4.8)
            _rumorEngine.TickDay(Stocks, GameTime);

            // Register rumor-triggered events with the event engine so they affect prices
            foreach (var rumorEvt in _rumorEngine.RumorEventsThisTick)
            {
                _eventEngine.InjectEvent(rumorEvt);
            }

            // M&A events (Bible 8.2.7): acquisition announcements, tender offers
            _eventEngine.TryGenerateMAndA(Stocks, GameTime);
            _eventEngine.TryGenerateGeopoliticalEvent(Stocks, GameTime);
            _eventEngine.TryGenerateSecondaryOffering(Stocks, GameTime);

            // Narrative arcs: advance multi-phase story events (Phase 1C)
            _narrativeEngine.TickDay(Stocks, GameTime, Phase);
            foreach (var arcEvt in _narrativeEngine.NewEventsThisTick)
                _eventEngine.InjectEvent(arcEvt);

            // AI Trader daily behaviors (window dressing, short reports, buybacks)
            _aiTraderEngine.TickDay(Stocks, _eventEngine.ActiveEvents, GameTime);
            foreach (var aiEvt in _aiTraderEngine.NewsThisTick)
            {
                aiEvt.TriggeredAt = GameTime;
                _eventEngine.InjectEvent(aiEvt);
            }

            // SMA regulatory check (Bible 9.2: daily surveillance)
            _smaEngine.TickDay(Portfolio, Stocks, StocksBySymbol, _eventEngine.ActiveEvents, GameTime);

            // Pause game if SMA demands it (investigation/penalty) — conditional (Bible 16.2)
            if (AutoPauseOnSMA && _smaEngine.NotificationsThisTick.Any(n => n.PauseGame))
                SetSpeed(GameSpeed.Paused);

            // Record daily equity snapshot for performance chart
            Func<string, decimal> getPrice = sym =>
                StocksBySymbol.TryGetValue(sym, out var s) ? s.CurrentPrice : 0m;
            var equity = Portfolio.TotalEquity(getPrice);
            var avgChange = Stocks.Average(s => (double)s.DayChangePercent);
            _achievementEngine.RecordEquitySnapshot(equity, Portfolio.Cash, (decimal)avgChange, GameTime);

            // Check achievements at end of each trading day
            _achievementEngine.CheckAchievements(equity, Portfolio, getPrice, GameTime, this);

            // Check margin call: if margin used > maintenance level, force liquidate
            // Bible 19.2: liquidate positions until margin is covered or all positions gone
            if (Portfolio.MarginEnabled && Portfolio.MarginBalance > 0)
            {
                var marginPct = Portfolio.MarginUsedPercent(getPrice);
                if (marginPct > 100) // Equity < margin balance = underwater
                {
                    // Force liquidate positions (largest first) until margin is manageable
                    var positions = Portfolio.Positions.Values
                        .OrderByDescending(p => Math.Abs(p.MarketValue(getPrice(p.Symbol))))
                        .ToList();

                    foreach (var pos in positions)
                    {
                        var stock = StocksBySymbol.GetValueOrDefault(pos.Symbol);
                        if (stock == null) continue;

                        var side = pos.Shares > 0 ? OrderSide.Sell : OrderSide.Cover;
                        OrderEngine.PlaceOrder(pos.Symbol, side, OrderType.Market,
                            Math.Abs(pos.Shares), stock, GameTime, true);
                        // Repay margin
                        var repay = Math.Min(Portfolio.MarginBalance, Math.Abs(pos.MarketValue(stock.CurrentPrice)));
                        Portfolio.MarginBalance -= repay;
                        MarginCallThisTick = true;

                        // Stop if margin is now manageable
                        if (Portfolio.MarginBalance <= 0 || Portfolio.MarginUsedPercent(getPrice) <= 100)
                            break;
                    }
                }
            }

            // Bankruptcy check moved to pre-market-open section (runs every tick)

            // Track flash crash for achievements
            if (_circuitBreaker.IsMarketHalted)
                _achievementEngine.Stats.SurvivedFlashCrash = true;

            // Check scenario win/lose conditions
            if (ActiveScenario != null && ActiveScenario.IsActive && !ActiveScenario.IsCompleted)
            {
                ActiveScenario.DaysElapsed++;
                CheckScenarioConditions(equity);
            }
        }

        // Auto-pause on breaking news: Major severity events (Bible 16.2)
        if (AutoPauseOnNews && _eventEngine.NewEventsThisTick.Any(e => e.Severity == EventSeverity.Major))
            SetSpeed(GameSpeed.Paused);

        // Auto-pause on margin call (Bible 16.2)
        if (AutoPauseOnMarginCall && MarginCallThisTick)
            SetSpeed(GameSpeed.Paused);

        // Track filled orders for achievements
        foreach (var filledOrder in OrdersFilledThisTick)
        {
            // Track short position opens
            if (filledOrder.Side == OrderSide.Short)
                _achievementEngine.RecordShortOpened();

            // Track limit order fills
            if (filledOrder.Type == OrderType.Limit || filledOrder.Type == OrderType.StopLimit)
                _achievementEngine.RecordLimitOrderFilled();

            // Track trades near Major news events (within 10 minutes = ~2 ticks at 5min/tick)
            if (_eventEngine.NewEventsThisTick.Any(e => e.Severity == EventSeverity.Major) ||
                _eventEngine.ActiveEvents.Any(e => e.Severity == EventSeverity.Major &&
                    (GameTime - e.TriggeredAt).TotalMinutes <= 10))
            {
                _achievementEngine.RecordTradeNearMajorEvent();
            }
        }

        // Auto-pause on order execution: limit/stop/pending fills (Bible 16.2)
        if (AutoPauseOnOrderExecution && OrdersFilledThisTick.Count > 0)
            SetSpeed(GameSpeed.Paused);

        TickCount++;

        if (TickCount % 100 == 0)
        {
            _log.Info("Tick milestone", new
            {
                tick = TickCount,
                gameTime = GameTime.ToString("yyyy-MM-dd HH:mm"),
                stocksSample = Stocks.Take(3).Select(s => new
                {
                    s.Symbol,
                    price = s.CurrentPrice,
                    change = s.DayChangePercent
                })
            });
        }
    }

    public bool IsMarketOpen()
    {
        var day = GameTime.DayOfWeek;
        if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
            return false;

        var time = GameTime.TimeOfDay;
        var open = new TimeSpan(9, 30, 0);
        var close = new TimeSpan(16, 0, 0);

        return time >= open && time < close;
    }

    /// <summary>
    /// After-hours session: 4:00 PM - 8:00 PM.
    /// Limit orders only, wider spreads (3x), lower volume (20%).
    /// </summary>
    public bool IsAfterHours()
    {
        var day = GameTime.DayOfWeek;
        if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
            return false;

        var time = GameTime.TimeOfDay;
        return time >= new TimeSpan(16, 0, 0) && time < new TimeSpan(20, 0, 0);
    }

    /// <summary>Whether trading is possible (regular hours OR after-hours).</summary>
    public bool IsTradingSession() => IsMarketOpen() || IsAfterHours();

    // IsPreMarket and second IsAfterHours removed — see canonical versions above IsMarketOpen

    private List<Stock> GenerateStocks(int seed, int count)
    {
        var rng = new Random(seed);
        var stocks = new List<Stock>();
        var usedSymbols = new HashSet<string>();

        var stocksPerSector = count / Sectors.Length;
        var remainder = count % Sectors.Length;

        foreach (var sector in Sectors)
        {
            var sectorCount = stocksPerSector + (remainder-- > 0 ? 1 : 0);

            for (int i = 0; i < sectorCount; i++)
            {
                var (name, symbol) = GenerateName(rng, sector, usedSymbols);
                usedSymbols.Add(symbol);

                var stock = new Stock(symbol, name, sector);

                // Assign subsector
                if (Subsectors.TryGetValue(sector, out var subs))
                    stock.Subsector = subs[rng.Next(subs.Length)];

                InitializeStockData(rng, stock);
                stocks.Add(stock);
            }
        }

        _log.Info("Stocks generated", new
        {
            total = stocks.Count,
            sectorDistribution = stocks.GroupBy(s => s.Sector)
                .Select(g => new { sector = g.Key, count = g.Count() })
        });

        return stocks;
    }

    private void InitializeStockData(Random rng, Stock stock)
    {
        // Market cap distribution: power law (few large, many small)
        // Bible 11.3.2
        var capRoll = rng.NextDouble();
        decimal marketCapBillions = capRoll switch
        {
            < 0.02 => (decimal)(rng.NextDouble() * 400 + 100),   // Mega Cap $100-500B
            < 0.10 => (decimal)(rng.NextDouble() * 90 + 10),     // Large Cap $10-100B
            < 0.30 => (decimal)(rng.NextDouble() * 8 + 2),       // Mid Cap $2-10B
            < 0.70 => (decimal)(rng.NextDouble() * 1.7 + 0.3),   // Small Cap $0.3-2B
            _ => (decimal)(rng.NextDouble() * 0.25 + 0.05),       // Micro Cap $0.05-0.3B
        };

        // Shares outstanding: scale with market cap for realistic price ranges ($5-$500)
        // Target price = MarketCap / Shares → choose shares to get reasonable price
        var targetPrice = (decimal)(rng.NextDouble() * 80 + 5); // Target $5-$85 for most stocks
        if (marketCapBillions > 50) targetPrice = (decimal)(rng.NextDouble() * 300 + 100); // Mega: $100-$400
        else if (marketCapBillions > 10) targetPrice = (decimal)(rng.NextDouble() * 150 + 50); // Large: $50-$200
        else if (marketCapBillions > 2) targetPrice = (decimal)(rng.NextDouble() * 80 + 20); // Mid: $20-$100
        stock.SharesOutstanding = (long)(marketCapBillions * 1_000_000_000m / targetPrice);
        stock.SharesOutstanding = Math.Max(stock.SharesOutstanding, 1_000_000); // Floor: 1M shares
        stock.CurrentPrice = Math.Round(marketCapBillions * 1_000_000_000m / stock.SharesOutstanding, 2);
        stock.CurrentPrice = Math.Max(stock.CurrentPrice, 1.00m);  // Floor $1 (no penny stocks at start)
        stock.CurrentPrice = Math.Min(stock.CurrentPrice, 5000m);

        stock.PreviousClose = stock.CurrentPrice;
        stock.DayHigh = stock.CurrentPrice;
        stock.DayLow = stock.CurrentPrice;
        stock.FairValue = stock.CurrentPrice;

        // Ownership structure (Bible 5.8)
        stock.InsiderOwnership = (decimal)(rng.NextDouble() * 0.25 + 0.05);
        stock.InstitutionalOwnership = (decimal)(rng.NextDouble() * 0.50 + 0.20);

        // Volatility (sector-dependent, Bible 11.2.2)
        // Sector daily volatility (annualized: multiply by √252)
        // Real-world SPY ~1% daily, individual stocks 1.5-3% daily
        var sectorVolBase = stock.Sector switch
        {
            "Technology" => 0.014,       // ~22% annualized
            "Energy" => 0.016,           // ~25% annualized
            "Healthcare" => 0.015,       // ~24% annualized
            "Financials" => 0.012,       // ~19% annualized
            "Consumer Goods" => 0.009,   // ~14% annualized
            "Industrials" => 0.010,      // ~16% annualized
            "Materials" => 0.013,        // ~21% annualized
            "Real Estate" => 0.012,      // ~19% annualized
            "Telecommunications" => 0.008, // ~13% annualized
            "Utilities" => 0.007,        // ~11% annualized
            "Luxury Goods" => 0.012,     // ~19% annualized
            "Transportation" => 0.011,   // ~17% annualized
            _ => 0.011,
        };
        stock.BaseVolatility = (decimal)(sectorVolBase * (0.7 + rng.NextDouble() * 0.6)); // 70%-130% of sector base

        // Liquidity score based on market cap
        stock.LiquidityScore = marketCapBillions switch
        {
            > 100m => 9 + (rng.Next(2)),    // 9-10
            > 10m => 7 + (rng.Next(2)),     // 7-8
            > 2m => 5 + (rng.Next(2)),      // 5-6
            > 0.3m => 3 + (rng.Next(2)),    // 3-4
            _ => 1 + (rng.Next(2)),          // 1-2
        };

        // Average daily volume (correlates with liquidity)
        stock.AverageVolume = stock.LiquidityScore switch
        {
            >= 9 => (long)(rng.NextDouble() * 40_000_000 + 10_000_000),
            >= 7 => (long)(rng.NextDouble() * 9_000_000 + 1_000_000),
            >= 5 => (long)(rng.NextDouble() * 900_000 + 100_000),
            >= 3 => (long)(rng.NextDouble() * 90_000 + 10_000),
            _ => (long)(rng.NextDouble() * 9_000 + 1_000),
        };

        // Sector-specific fundamentals (realistic ranges per sector)
        var (marginRange, growthRange, deRange, divChance, divRange) = stock.Sector switch
        {
            "Technology"        => ((0.10, 0.30), (0.05, 0.40), (0.0, 0.5),  0.15, (0.0, 0.015)),
            "Healthcare"        => ((0.08, 0.25), (0.03, 0.30), (0.2, 1.5),  0.25, (0.0, 0.025)),
            "Financials"        => ((0.15, 0.30), (0.02, 0.12), (2.0, 8.0),  0.50, (0.02, 0.05)),
            "Energy"            => ((0.05, 0.20), (-0.05, 0.15), (0.5, 2.5), 0.60, (0.02, 0.06)),
            "Consumer Goods"    => ((0.05, 0.15), (0.02, 0.12), (0.3, 1.5),  0.45, (0.015, 0.04)),
            "Industrials"       => ((0.06, 0.15), (0.02, 0.10), (0.5, 2.0),  0.40, (0.01, 0.03)),
            "Materials"         => ((0.05, 0.15), (-0.03, 0.10), (0.5, 2.0), 0.40, (0.015, 0.04)),
            "Real Estate"       => ((0.15, 0.35), (0.01, 0.08), (0.5, 1.8),  0.80, (0.03, 0.08)),
            "Telecommunications"=> ((0.08, 0.20), (0.01, 0.08), (0.8, 2.5),  0.65, (0.03, 0.06)),
            "Utilities"         => ((0.08, 0.15), (0.01, 0.05), (0.8, 2.0),  0.85, (0.03, 0.05)),
            "Luxury Goods"      => ((0.10, 0.25), (0.03, 0.20), (0.2, 1.0),  0.30, (0.01, 0.02)),
            "Transportation"    => ((0.05, 0.12), (0.02, 0.10), (0.5, 2.5),  0.35, (0.01, 0.03)),
            _ =>                   ((0.05, 0.20), (0.0, 0.15),  (0.3, 2.0),  0.40, (0.01, 0.04)),
        };

        var margin = (decimal)(rng.NextDouble() * (marginRange.Item2 - marginRange.Item1) + marginRange.Item1);
        stock.Revenue = marketCapBillions * (decimal)(rng.NextDouble() * 0.3 + 0.1) * 1_000_000_000m;
        stock.NetIncome = stock.Revenue * margin;
        stock.DividendYield = rng.NextDouble() < divChance ? (decimal)(rng.NextDouble() * (divRange.Item2 - divRange.Item1) + divRange.Item1) : 0m;
        stock.DebtToEquity = (decimal)(rng.NextDouble() * (deRange.Item2 - deRange.Item1) + deRange.Item1);
        stock.RevenueGrowth = (decimal)(rng.NextDouble() * (growthRange.Item2 - growthRange.Item1) + growthRange.Item1);
        stock.Employees = (int)(marketCapBillions * (decimal)(rng.NextDouble() * 500 + 100));
        stock.ShortBorrowAvailability = (decimal)(rng.NextDouble() * 0.5 + 0.5);

        // Assign 1-3 traits (Bible 11.3.4)
        AssignTraits(rng, stock, marketCapBillions);

        // Initial bid/ask
        var halfSpread = stock.CurrentPrice * 0.001m;
        stock.BidPrice = Math.Round(stock.CurrentPrice - halfSpread, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + halfSpread, 2);
    }

    /// <summary>
    /// Assign traits to a stock based on its fundamentals. Bible 11.3.4: 25 traits.
    /// Each trait affects gameplay via PriceEngine, AITraderEngine, EventEngine, etc.
    /// </summary>
    private void AssignTraits(Random rng, Stock stock, decimal marketCapB)
    {
        var possibleTraits = new List<string>();

        // --- Fundamentals-based traits ---
        if (marketCapB > 50) possibleTraits.Add("Blue Chip");
        if (stock.RevenueGrowth > 0.20m) possibleTraits.Add("Growth Stock");
        if (stock.RevenueGrowth > 0.30m) possibleTraits.Add("Fast Grower");
        if (stock.PERatio > 0 && stock.PERatio < 15) possibleTraits.Add("Value Stock");
        if (stock.DividendYield > 0.03m) possibleTraits.Add("Dividend Aristocrat");
        if (marketCapB < 0.3m) possibleTraits.Add("Speculative");
        if (stock.BaseVolatility > 0.035m) possibleTraits.Add("Volatile");
        if (stock.CurrentPrice < 5m) possibleTraits.Add("Penny Stock");
        if (stock.RevenueGrowth < 0.05m && stock.DividendYield > 0) possibleTraits.Add("Slow Grower");
        if (stock.BaseVolatility < 0.012m) possibleTraits.Add("Defensive");
        if (stock.DebtToEquity > 2.0m) possibleTraits.Add("Debt Heavy");

        // --- New traits (Bible 11.3.4) ---
        // Momentum Stock: mid-volatility stocks with positive growth
        if (stock.BaseVolatility > 0.02m && stock.BaseVolatility < 0.04m && stock.RevenueGrowth > 0.10m)
            possibleTraits.Add("Momentum Stock");

        // Cyclical: sectors sensitive to economic cycles
        if (stock.Sector is "Energy" or "Materials" or "Industrials" or "Financials" or "Real Estate" or "Luxury Goods")
            possibleTraits.Add("Cyclical");

        // Cash Cow: high income, moderate market cap
        if (stock.NetIncome > 0 && stock.Revenue > 0 && stock.NetIncome / stock.Revenue > 0.15m && stock.RevenueGrowth < 0.10m)
            possibleTraits.Add("Cash Cow");

        // Market Leader: largest in sector (assigned separately, but probability-based here)
        if (marketCapB > 30 && rng.NextDouble() < 0.15)
            possibleTraits.Add("Market Leader");

        // Acquisition Target: small/mid cap with value
        if (marketCapB < 5 && marketCapB > 0.5m && stock.PERatio > 0 && stock.PERatio < 20)
            possibleTraits.Add("Acquisition Target");

        // Serial Acquirer: large caps in growth sectors
        if (marketCapB > 20 && stock.Sector is "Technology" or "Healthcare" && rng.NextDouble() < 0.2)
            possibleTraits.Add("Serial Acquirer");

        // Insider Favorite: high insider ownership
        if (stock.InsiderOwnership > 0.20m)
            possibleTraits.Add("Insider Favorite");

        // Compounder: steady growth 10-20% p.a.
        if (stock.RevenueGrowth >= 0.10m && stock.RevenueGrowth <= 0.20m && stock.DebtToEquity < 1.0m)
            possibleTraits.Add("Compounder");

        // Short Target: high short interest
        if (stock.ShortInterest > stock.SharesOutstanding * 0.15m)
            possibleTraits.Add("Short Target");

        // Random-chance traits (add flavor)
        if (rng.NextDouble() < 0.08) possibleTraits.Add("Turnaround");
        if (rng.NextDouble() < 0.06) possibleTraits.Add("ESG Leader");
        if (rng.NextDouble() < 0.05) possibleTraits.Add("Controversy Magnet");
        if (rng.NextDouble() < 0.07) possibleTraits.Add("Seasonal");
        if (rng.NextDouble() < 0.04) possibleTraits.Add("IPO Fresh");

        // Default: at least one trait
        if (possibleTraits.Count == 0) possibleTraits.Add("Compounder");

        // Pick 1-4 traits (more variety)
        var traitCount = Math.Min(rng.Next(1, 5), possibleTraits.Count);
        var shuffled = possibleTraits.OrderBy(_ => rng.Next()).Take(traitCount);
        foreach (var trait in shuffled)
        {
            stock.Traits.Add(trait);
        }
    }

    private static readonly Dictionary<string, string[][]> NameParts = new()
    {
        ["Technology"] = new[] {
            new[] { "Vertex", "Nova", "Quantum", "Cyber", "Nexus", "Apex", "Synth", "Pixel", "Cloud", "Data", "Neural", "Helix", "Core", "Edge", "Smart", "Bolt", "Arc", "Zero", "Meta", "Flux", "Grid", "Stack", "Byte", "Logic", "Vector", "Pulse", "Nano", "Zeta", "Krypton", "Cipher" },
            new[] { "Dynamics", "Systems", "Technologies", "Labs", "Solutions", "Logic", "Networks", "Soft", "AI", "Tech", "Ware", "Digital", "Platform", "Cloud", "Works", "Forge", "Hub", "Link", "Code", "Base" }
        },
        ["Energy"] = new[] {
            new[] { "Petro", "Solar", "Volt", "Hydro", "Geo", "Wind", "Fuel", "Terra", "Ion", "Atom", "Green", "Flux", "Therm", "Eco", "Power", "Helios", "Ember", "Radiant", "Dynamo", "Charge", "Meridian", "Horizon", "Torque", "Blaze", "Arctic" },
            new[] { "Energy", "Power", "Resources", "Oil", "Gas", "Corp", "Renewables", "Fuels", "Grid", "Stream", "Force", "Solutions", "Dynamics", "Industries", "Partners", "Holdings" }
        },
        ["Financials"] = new[] {
            new[] { "Capital", "First", "Global", "Premier", "Trust", "Crown", "Sterling", "Pacific", "Atlantic", "Summit", "Eagle", "Sovereign", "Prime", "Harbor", "Meridian", "Keystone", "Granite", "Fortress", "Pinnacle", "Liberty", "Patriot", "Heritage", "Vanguard", "Alliance", "Continental" },
            new[] { "Bank", "Financial", "Holdings", "Capital", "Group", "Trust", "Securities", "Advisors", "Partners", "Corp", "Wealth", "Bancorp", "Investments", "Asset Management", "Credit" }
        },
        ["Healthcare"] = new[] {
            new[] { "Bio", "Nova", "Medi", "Vita", "Pulse", "Neura", "Cell", "Genome", "Helix", "Immuno", "Pharma", "Cardio", "Synapse", "Astra", "Vivo", "Onco", "Proto", "Zenith", "Apex", "Cura", "Seraph", "Lumina", "Catalyst", "Meridian", "Elixir" },
            new[] { "Pharma", "Therapeutics", "Sciences", "Biotech", "Labs", "Medical", "Diagnostics", "Health", "Genomics", "Cure", "Rx", "BioSciences", "Medicine", "Clinical", "Oncology" }
        },
        ["Consumer Goods"] = new[] {
            new[] { "Bright", "Prime", "Fresh", "Urban", "Ever", "Home", "Pure", "Daily", "Golden", "Nature", "Bloom", "Clear", "Swift", "True", "Harvest", "Maple", "Cedar", "Willow", "Olive", "Sage", "Ridge", "Harbor", "Meadow", "Valley", "Summit" },
            new[] { "Brands", "Products", "Foods", "Consumer", "Essentials", "Goods", "Co", "Corp", "Industries", "Market", "Direct", "Retail", "Provisions", "Staples", "Group" }
        },
        ["Industrials"] = new[] {
            new[] { "Iron", "Steel", "Forge", "Titan", "Atlas", "Apex", "Core", "Granite", "Bolt", "Arc", "Matrix", "Omega", "Vanguard", "Summit", "Anvil", "Rivet", "Piston", "Crane", "Condor", "Falcon", "Shield", "Armor", "Centurion", "Bastion", "Aegis" },
            new[] { "Industries", "Manufacturing", "Engineering", "Works", "Fabrication", "Machinery", "Industrial", "Solutions", "Dynamics", "Systems", "Defense", "Aerospace", "Corp", "Group" }
        },
        ["Materials"] = new[] {
            new[] { "Terra", "Geo", "Crystal", "Ore", "Mineral", "Carbon", "Alloy", "Stone", "Metal", "Prism", "Element", "Cobalt", "Quarry", "Onyx", "Copper", "Zinc", "Nickel", "Beryl", "Jade", "Amber", "Obsidian", "Granite", "Basalt", "Flint", "Mica" },
            new[] { "Materials", "Mining", "Resources", "Metals", "Minerals", "Chemical", "Composites", "Industries", "Extraction", "Corp", "Commodities", "Processing", "Refining" }
        },
        ["Real Estate"] = new[] {
            new[] { "Crown", "Harbor", "Summit", "Urban", "Metro", "Skyline", "Park", "Beacon", "Crest", "Haven", "Tower", "Pinnacle", "Grand", "Lakeside", "Riverside", "Coastal", "Highland", "Midtown", "Downtown", "Uptown", "Plaza", "Gateway", "Landmark", "Heritage", "Vista" },
            new[] { "Realty", "Properties", "Real Estate", "Holdings", "Development", "Estates", "Trust", "Capital", "REIT", "Property", "Homes", "Living", "Communities", "Residential" }
        },
        ["Telecommunications"] = new[] {
            new[] { "Signal", "Wave", "Link", "Net", "Tele", "Beam", "Fiber", "Pulse", "Echo", "Relay", "Orbit", "Spectrum", "Grid", "Omni", "Sonic", "Quantum", "Swift", "Rapid", "Global", "United", "Digital", "Stream", "Reach", "Pinnacle", "Horizon" },
            new[] { "Communications", "Telecom", "Networks", "Wireless", "Connect", "Broadband", "Media", "Signal", "Mobile", "Digital", "Streaming", "Interactive", "Entertainment" }
        },
        ["Utilities"] = new[] {
            new[] { "Power", "Grid", "Hydro", "Volt", "Amp", "Current", "Flow", "Source", "Green", "Clean", "Civic", "Metro", "National", "Central", "Pacific", "Mountain", "Prairie", "River", "Lake", "Coastal", "Valley", "Basin", "Delta", "Summit", "Northern" },
            new[] { "Utilities", "Power", "Electric", "Energy", "Water", "Gas", "Services", "Utility", "Corp", "Light", "Generation", "Distribution", "Authority" }
        },
        ["Luxury Goods"] = new[] {
            new[] { "Prestige", "Royal", "Maison", "Luxe", "Elite", "Noble", "Grand", "Imperial", "Regal", "Opulent", "Crown", "Sterling", "Artisan", "Bespoke", "Chateau", "Vivienne", "Laurent", "Bellini", "Montague", "Harrington", "Ashworth", "Beaumont", "Kensington", "Windsor", "Devereaux" },
            new[] { "Luxury", "Group", "Brands", "Collection", "Maison", "House", "Atelier", "Design", "International", "Premium", "Couture", "Jewelers", "Watches", "Spirits" }
        },
        ["Transportation"] = new[] {
            new[] { "Trans", "Global", "Swift", "Rapid", "Fleet", "Cargo", "Express", "Rail", "Aero", "Maritime", "Voyage", "Route", "Track", "Horizon", "Atlas", "Compass", "Navigate", "Convoy", "Passage", "Traverse", "Vector", "Zenith", "Orbit", "Trident", "Meridian" },
            new[] { "Transport", "Logistics", "Shipping", "Freight", "Airlines", "Rail", "Corp", "Transit", "Carriers", "Express", "Mobility", "Aviation", "Marine", "Trucking" }
        },
    };

    private (string name, string symbol) GenerateName(Random rng, string sector, HashSet<string> usedSymbols)
    {
        var parts = NameParts.GetValueOrDefault(sector, NameParts["Technology"])!;
        var prefixes = parts[0];
        var suffixes = parts[1];

        string name;
        string symbol;
        int attempts = 0;

        do
        {
            var prefix = prefixes[rng.Next(prefixes.Length)];
            var suffix = suffixes[rng.Next(suffixes.Length)];

            // Name patterns
            var pattern = rng.Next(3);
            name = pattern switch
            {
                0 => $"{prefix}{suffix}",
                1 => $"{prefix} {suffix}",
                _ => $"{prefix}{suffix} Inc.",
            };

            // Generate symbol (3-5 chars)
            symbol = GenerateSymbol(prefix, suffix, rng);
            attempts++;

            if (attempts > 100)
            {
                // Fallback: add number
                symbol = symbol[..3] + rng.Next(10).ToString();
                break;
            }
        } while (usedSymbols.Contains(symbol));

        return (name, symbol);
    }

    /// <summary>
    /// Apply a random opening gap to simulate overnight price movement.
    /// Bible 20.2: Gap Up/Down at market open.
    /// Most stocks gap small (±0.5%), some gap big on events.
    /// </summary>
    private void ApplyOpeningGap(Stock stock)
    {
        var rng = new Random(_seed + (int)TickCount + stock.Symbol.GetHashCode());

        // Check for overnight events affecting this stock → gap proportional to event magnitude
        var overnightEffect = 0f;
        foreach (var evt in _eventEngine.ActiveEvents)
        {
            if (evt.AffectedSymbols.Contains(stock.Symbol) || evt.AffectedSectors.Contains(stock.Sector))
                overnightEffect += evt.PriceEffect * 0.5f; // Dampened: half the event effect as gap
        }

        double gapPercent;
        if (Math.Abs(overnightEffect) > 0.005f)
        {
            // Event-driven gap: use overnight effect + small noise
            gapPercent = overnightEffect + (rng.NextDouble() - 0.5) * 0.005;
            gapPercent = Math.Clamp(gapPercent, -0.05, 0.05); // Cap at ±5%
        }
        else
        {
            // No significant overnight events: random gap
            var roll = rng.NextDouble();
            double maxGap = roll < 0.80 ? 0.003 : roll < 0.95 ? 0.015 : 0.03;
            maxGap *= (double)(1m + stock.BaseVolatility * 2m);
            maxGap = Math.Min(maxGap, 0.03);
            gapPercent = (rng.NextDouble() * 2 - 1) * maxGap;
        }
        var gapAmount = stock.CurrentPrice * (decimal)gapPercent;

        stock.CurrentPrice = Math.Max(0.01m, Math.Round(stock.CurrentPrice + gapAmount, 2));

        // Update bid/ask around new price
        var spread = stock.AskPrice - stock.BidPrice;
        if (spread <= 0) spread = stock.CurrentPrice * 0.002m;
        stock.BidPrice = Math.Round(stock.CurrentPrice - spread / 2, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + spread / 2, 2);
        stock.BidPrice = Math.Max(stock.BidPrice, 0.01m);
    }

    /// <summary>
    /// At market open, pull stocks toward fair value to prevent compound drift.
    /// If deviation > 10%, pulls 20% of the deviation back per day.
    /// Equilibrium: ~15% deviation (where pull ≈ daily drift).
    /// </summary>
    private void ApplyDailyMeanReversion(Stock stock)
    {
        if (stock.FairValue <= 0 || stock.Traits.Contains("ETF")) return;

        var deviation = (stock.CurrentPrice - stock.FairValue) / stock.FairValue;
        if (Math.Abs(deviation) < 0.10m) return;

        // Pull 20% of excess deviation (above 10%) back toward fair value
        var excessDeviation = deviation - Math.Sign(deviation) * 0.10m;
        var pullPercent = excessDeviation * 0.20m;
        var newPrice = stock.CurrentPrice * (1m - pullPercent);
        stock.CurrentPrice = Math.Max(0.01m, Math.Round(newPrice, 2));

        // Update bid/ask
        var spread = stock.AskPrice - stock.BidPrice;
        if (spread <= 0) spread = stock.CurrentPrice * 0.002m;
        stock.BidPrice = Math.Round(stock.CurrentPrice - spread / 2, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + spread / 2, 2);
    }

    private void GenerateHistoricalPrices(int seed, List<Stock> stocks)
    {
        // Each stock gets its own seeded generator for reproducibility
        for (int i = 0; i < stocks.Count; i++)
        {
            var stock = stocks[i];
            var historyGen = new HistoryGenerator(seed: seed + i + 1000);
            var candles = historyGen.GenerateDaily(stock, GameTime, Phase);
            DailyHistory[stock.Symbol] = candles;

            // Initialize YearHigh/YearLow from historical data (252 trading days = 1 year)
            if (candles.Count > 0)
            {
                stock.YearHigh = candles.Max(c => c.High);
                stock.YearLow = candles.Min(c => c.Low);
                if (stock.CurrentPrice > stock.YearHigh) stock.YearHigh = stock.CurrentPrice;
                if (stock.CurrentPrice < stock.YearLow) stock.YearLow = stock.CurrentPrice;
                // Set PreviousClose to second-to-last candle so sectors show initial change
                if (candles.Count >= 2)
                    stock.PreviousClose = candles[^2].Close;
            }
            else
            {
                stock.YearHigh = stock.CurrentPrice;
                stock.YearLow = stock.CurrentPrice;
            }
        }

        _log.Info("Historical prices generated", new
        {
            stocks = stocks.Count,
            candlesPerStock = 252,
            phase = Phase.ToString(),
        });
    }

    /// <summary>
    /// Recalculate FairValue for all stocks based on current fundamentals.
    /// Uses a PE-based model: FairValue = EPS * sector-appropriate PE multiple.
    /// Called daily at market open.
    /// </summary>
    private void RecalculateFairValues()
    {
        foreach (var stock in Stocks)
        {
            if (stock.Traits.Contains("ETF")) continue;

            if (stock.NetIncome > 0 && stock.SharesOutstanding > 0)
            {
                var eps = stock.NetIncome / stock.SharesOutstanding;
                // Sector-based PE multiples (rough approximation)
                var sectorPE = stock.Sector switch
                {
                    "Technology" => 25m,
                    "Healthcare" => 22m,
                    "Financials" => 14m,
                    "Energy" => 12m,
                    "Utilities" => 16m,
                    "Real Estate" => 18m,
                    "Consumer Goods" => 20m,
                    "Industrials" => 17m,
                    "Materials" => 15m,
                    "Telecommunications" => 16m,
                    "Transportation" => 15m,
                    "Luxury Goods" => 22m,
                    _ => 18m,
                };
                // Blend PE-derived value but anchor to initial FairValue to prevent death spirals
                // Initial FairValue = starting price (set at game creation)
                var peFairValue = eps * sectorPE;
                // Clamp PE-derived value to ±30% of current FairValue (prevents sudden jumps)
                var clampedPE = Math.Clamp(peFairValue, stock.FairValue * 0.7m, stock.FairValue * 1.3m);
                // Slow drift: 5% toward clamped PE value per day
                stock.FairValue = Math.Max(0.50m, Math.Round(stock.FairValue * 0.95m + clampedPE * 0.05m, 2));
            }
            // Unprofitable companies keep their original FairValue (stable anchor)
        }
    }

    private void CheckInsiderActivity()
    {
        InsiderTradesThisTick.Clear();
        ShortSqueezeWarningsThisTick.Clear();
        var rng = new Random(_seed + (int)TickCount + 77777);

        // ~0.4% chance per stock per day = ~1 insider trade per day
        foreach (var stock in MutableStocks.Where(s => !s.Traits.Contains("ETF")))
        {
            if (rng.NextDouble() > 0.004) continue;

            var isBuy = rng.NextDouble() > 0.4; // 60% buys, 40% sells
            var titles = new[] { "CEO", "CFO", "COO", "Director", "VP", "Board Member" };
            var title = titles[rng.Next(titles.Length)];
            var shares = (int)(rng.NextDouble() * 80_000 + 5_000);
            shares = shares / 1000 * 1000; // Round to thousands
            var value = shares * stock.CurrentPrice;

            InsiderTradesThisTick.Add(new InsiderTradeEvent
            {
                Symbol = stock.Symbol,
                Title = title,
                IsBuy = isBuy,
                Shares = shares,
                Value = value,
                Price = stock.CurrentPrice,
            });
        }
    }

    private void CheckStockSplits()
    {
        SplitsThisTick.Clear();
        var rng = new Random(_seed + (int)TickCount + 99999);

        // Forward splits: stocks over $500 have a small daily chance
        foreach (var stock in MutableStocks.Where(s => !s.Traits.Contains("ETF")))
        {
            if (stock.CurrentPrice > 500m && rng.NextDouble() < 0.005) // ~0.5% daily = ~1 per year for expensive stocks
            {
                var ratio = stock.CurrentPrice > 1000m ? 5 : (stock.CurrentPrice > 700m ? 3 : 2);
                ApplySplit(stock, ratio, rng);
            }
            // Reverse splits: penny stocks under $0.50 (with 60-day cooldown to prevent loops)
            else if (stock.CurrentPrice < 0.50m && rng.NextDouble() < 0.01)
            {
                if (!_lastReverseSplit.TryGetValue(stock.Symbol, out var lastSplit) || (GameTime - lastSplit).TotalDays >= 60)
                {
                    ApplyReverseSplit(stock, 10, rng); // 1:10 reverse
                    _lastReverseSplit[stock.Symbol] = GameTime;
                }
            }
        }
    }

    private void ApplySplit(Stock stock, int ratio, Random rng)
    {
        var oldPrice = stock.CurrentPrice;
        stock.CurrentPrice = Math.Round(stock.CurrentPrice / ratio, 2);
        stock.PreviousClose = Math.Round(stock.PreviousClose / ratio, 2);
        stock.FairValue = Math.Round(stock.FairValue / ratio, 2);
        stock.DayHigh = Math.Round(stock.DayHigh / ratio, 2);
        stock.DayLow = Math.Round(stock.DayLow / ratio, 2);
        stock.YearHigh = Math.Round(stock.YearHigh / ratio, 2);
        stock.YearLow = Math.Round(stock.YearLow / ratio, 2);
        stock.BidPrice = Math.Round(stock.BidPrice / ratio, 2);
        stock.AskPrice = Math.Round(stock.AskPrice / ratio, 2);
        stock.SharesOutstanding *= ratio;

        // Adjust player position if held
        if (Portfolio.Positions.TryGetValue(stock.Symbol, out var pos))
        {
            pos.Shares *= ratio;
            pos.AverageCost = Math.Round(pos.AverageCost / ratio, 2);
        }

        SplitsThisTick.Add(new StockSplitEvent
        {
            Symbol = stock.Symbol, Ratio = $"{ratio}:1", OldPrice = oldPrice, NewPrice = stock.CurrentPrice,
        });
        _log.Info("Stock split", new { symbol = stock.Symbol, ratio = $"{ratio}:1", oldPrice, newPrice = stock.CurrentPrice });
    }

    private void ApplyReverseSplit(Stock stock, int ratio, Random rng)
    {
        var oldPrice = stock.CurrentPrice;
        stock.CurrentPrice = Math.Round(stock.CurrentPrice * ratio, 2);
        stock.PreviousClose = Math.Round(stock.PreviousClose * ratio, 2);
        stock.FairValue = Math.Round(stock.FairValue * ratio, 2);
        stock.DayHigh = Math.Round(stock.DayHigh * ratio, 2);
        stock.DayLow = Math.Round(stock.DayLow * ratio, 2);
        stock.YearHigh = Math.Round(stock.YearHigh * ratio, 2);
        stock.YearLow = Math.Round(stock.YearLow * ratio, 2);
        stock.BidPrice = Math.Round(stock.BidPrice * ratio, 2);
        stock.AskPrice = Math.Round(stock.AskPrice * ratio, 2);
        stock.SharesOutstanding /= ratio;

        // Adjust player position
        if (Portfolio.Positions.TryGetValue(stock.Symbol, out var pos))
        {
            var newShares = Math.Max(1, pos.Shares / ratio);
            pos.Shares = newShares;
            pos.AverageCost = Math.Round(pos.AverageCost * ratio, 2);
        }

        SplitsThisTick.Add(new StockSplitEvent
        {
            Symbol = stock.Symbol, Ratio = $"1:{ratio}", OldPrice = oldPrice, NewPrice = stock.CurrentPrice,
        });
        _log.Info("Reverse stock split", new { symbol = stock.Symbol, ratio = $"1:{ratio}", oldPrice, newPrice = stock.CurrentPrice });
    }

    /// <summary>
    /// Daily charges: short borrow fees (Bible 4.4.1) + margin interest.
    /// Short borrow fee: 0.5-15% APY based on short interest level.
    /// Margin interest: (base rate + 4%) APY on outstanding margin balance.
    /// </summary>
    private void ChargeDailyFees()
    {
        Func<string, decimal> getPrice = sym =>
            StocksBySymbol.TryGetValue(sym, out var s) ? s.CurrentPrice : 0m;

        // Short borrow fees (Bible 4.4.1)
        foreach (var (symbol, pos) in Portfolio.Positions)
        {
            if (!pos.IsShort) continue;
            if (!StocksBySymbol.TryGetValue(symbol, out var stock)) continue;

            // Borrow rate based on short interest level
            var siPercent = stock.SharesOutstanding > 0
                ? stock.ShortInterest / stock.SharesOutstanding
                : 0m;
            var annualRate = siPercent switch
            {
                < 0.10m => 0.005m,   // 0.5% APY (easy to borrow)
                < 0.20m => 0.02m,    // 2% APY
                < 0.40m => 0.08m,    // 8% APY (hard to borrow)
                _ => 0.20m,          // 20% APY (very hard to borrow)
            };

            var positionValue = Math.Abs(pos.Shares) * stock.CurrentPrice;
            var dailyFee = positionValue * annualRate / 252m; // 252 trading days
            Portfolio.Cash -= Math.Round(dailyFee, 2);

            if (dailyFee > 10)
                _log.Debug("Short borrow fee", new { symbol, dailyFee = Math.Round(dailyFee, 2), annualRate });
        }

        // Margin interest (rate + 4% APY on margin balance)
        if (Portfolio.MarginEnabled && Portfolio.MarginBalance > 0)
        {
            var baseRate = _economicEngine?.Data.InterestRate ?? 3m;
            var marginRate = (baseRate + 4m) / 100m; // e.g. 3% + 4% = 7% APY
            var dailyInterest = Portfolio.MarginBalance * marginRate / 252m;
            Portfolio.Cash -= Math.Round(dailyInterest, 2);
        }
    }

    /// <summary>
    /// Apply slow fundamental drift between quarterly earnings.
    /// Revenue, NetIncome, and Employees change gradually each day,
    /// biased by the current sector cycle multiplier.
    /// Called once per trading day at market close.
    /// </summary>
    /// <summary>Shareholder vote events this tick (for frontend modal). Cleared each tick.</summary>
    public List<ShareholderVote> ShareholderVotesThisTick { get; } = new();

    private void CheckShareholderVotes()
    {
        ShareholderVotesThisTick.Clear();
        var rng = new Random(_seed + (int)TickCount + 99);

        // Only check once per ~30 trading days
        if (rng.NextDouble() > 0.033) return; // ~3.3% daily = ~1 per month

        foreach (var (sym, pos) in Portfolio.Positions)
        {
            if (!StocksBySymbol.TryGetValue(sym, out var stock)) continue;
            if (stock.Traits.Contains("ETF")) continue;
            if (stock.SharesOutstanding <= 0) continue;

            var ownershipPct = (decimal)Math.Abs(pos.Shares) / stock.SharesOutstanding * 100;
            if (ownershipPct < 5m) continue; // Must own >5%

            // Generate a vote proposal
            var proposals = new[]
            {
                ($"Approve $500M share buyback program for {stock.Name}", "buyback", 0.02f),
                ($"Approve strategic acquisition target for {stock.Name}", "acquisition", -0.01f),
                ($"Approve 20% dividend increase for {stock.Name}", "dividend_increase", 0.01f),
                ($"Replace current board member at {stock.Name}", "board_change", 0f),
                ($"Approve executive compensation package at {stock.Name}", "exec_comp", -0.005f),
            };

            var (proposal, voteType, priceImpact) = proposals[rng.Next(proposals.Length)];

            ShareholderVotesThisTick.Add(new ShareholderVote
            {
                Symbol = sym,
                CompanyName = stock.Name,
                Proposal = proposal,
                VoteType = voteType,
                OwnershipPercent = ownershipPct,
                PriceImpactIfApproved = priceImpact,
            });

            break; // Max 1 vote per day
        }
    }

    private void DriftFundamentals()
    {
        var rng = new Random(_seed + (int)TickCount);
        var sectorMults = _economicEngine.GetSectorMultipliers();

        foreach (var stock in MutableStocks)
        {
            if (stock.Traits.Contains("ETF")) continue;

            // Small daily revenue drift (±0.1% per day, biased by sector cycle)
            var sectorMult = sectorMults.GetValueOrDefault(stock.Sector, 1.0m);
            var drift = (decimal)(rng.NextDouble() * 0.002 - 0.001) * sectorMult;

            // CEO Archetype influences fundamental trajectory
            if (stock.Personality != null)
            {
                drift += stock.Personality.CEOArchetype switch
                {
                    "Visionary" => 0.0005m,          // Revenue grows faster
                    "Empire Builder" => 0.0004m,     // Acquisition-driven growth
                    "Sales Machine" => 0.0003m,      // Revenue-focused
                    "Disruptor" => 0.0003m,          // High growth, volatile
                    "Founder-CEO" => 0.0002m,        // Passionate growth
                    "Engineer-CEO" => 0.0001m,       // Steady improvement
                    "Industry Insider" => 0.0001m,   // Knows the market
                    "Dealmaker" => 0.0002m,          // M&A growth
                    "Finance Veteran" => 0m,          // Capital allocation, not growth
                    "Steady Hand" => 0m,              // Stable, no surprise
                    "Turnaround Artist" => stock.NetIncome < 0 ? 0.001m : 0m, // Accelerated recovery if in trouble
                    "Cost-Cutter" => -0.0001m,       // Revenue stagnates under cost-cutting
                    _ => 0m,
                };
            }

            // Supply chain impact: stressed suppliers hurt this stock's revenue
            if (stock.Personality?.Suppliers.Count > 0)
            {
                foreach (var supplierSym in stock.Personality.Suppliers)
                {
                    if (StocksBySymbol.TryGetValue(supplierSym, out var supplier) && supplier.PreviousClose > 0)
                    {
                        var supplierReturn = (supplier.CurrentPrice - supplier.PreviousClose) / supplier.PreviousClose;
                        if (Math.Abs(supplierReturn) > 0.03m)
                            drift += supplierReturn * 0.1m; // 10% of large supplier moves flow through
                    }
                }
            }

            stock.Revenue = Math.Max(1m, stock.Revenue * (1m + drift));

            // Cost-Cutter CEO improves margins
            var margin = stock.Revenue != 0 ? stock.NetIncome / stock.Revenue : 0m;
            if (stock.Personality?.CEOArchetype == "Cost-Cutter" && margin < 0.25m)
                margin += 0.0002m; // Margins slowly improve under cost-cutting
            stock.NetIncome = Math.Round(stock.Revenue * margin, 0);

            // Employee count drifts with revenue (grows when revenue grows)
            if (rng.NextDouble() < 0.05) // 5% chance per day
            {
                var empDrift = drift > 0 ? rng.Next(1, 5) : -rng.Next(0, 3);
                // Empire Builder grows staff faster
                if (stock.Personality?.CEOArchetype == "Empire Builder") empDrift += rng.Next(1, 3);
                // Cost-Cutter shrinks staff
                if (stock.Personality?.CEOArchetype == "Cost-Cutter") empDrift -= rng.Next(0, 2);
                stock.Employees = Math.Max(10, stock.Employees + empDrift);
            }

            // Dynamic Credit Rating: D/E > 3 for extended periods → downgrade risk
            if (stock.Personality != null && rng.NextDouble() < 0.005) // 0.5% daily check
            {
                var de = stock.DebtToEquity;
                var rating = stock.Personality.CreditRating;
                if (de > 3m && rating != "B" && rating != "BB")
                    stock.Personality.CreditRating = rating switch { "AAA" or "AA" => "A", "A" => "BBB", "BBB" => "BB", _ => rating };
                else if (stock.NetIncome > 0 && de < 1m && rating is "BB" or "B")
                    stock.Personality.CreditRating = rating == "B" ? "BB" : "BBB";
            }
        }
    }

    /// <summary>
    /// Bible 4.4.5: Short Squeeze detection.
    /// Trigger: Short Interest >30% AND price up >10% in last 60 ticks (1 hour).
    /// </summary>
    private void CheckShortSqueeze(Stock stock)
    {
        // Track rolling price history (60 ticks = 1 hour)
        if (!_priceHistory60.TryGetValue(stock.Symbol, out var queue))
        {
            queue = new Queue<decimal>();
            _priceHistory60[stock.Symbol] = queue;
        }
        queue.Enqueue(stock.CurrentPrice);
        if (queue.Count > 60) queue.Dequeue();
        if (queue.Count < 60) return; // Need full hour of data

        var price60Ago = queue.Peek();
        if (price60Ago <= 0) return;
        var hourlyChange = (stock.CurrentPrice - price60Ago) / price60Ago;

        // Bible 4.4.5: Short Interest >30% AND price >10% up in last hour
        var shortInterestPct = stock.SharesOutstanding > 0
            ? stock.ShortInterest / stock.SharesOutstanding
            : 0m;

        if (shortInterestPct > 0.30m && hourlyChange > 0.10m)
        {
            // Only warn once per stock per day (avoid spam)
            if (ShortSqueezeWarningsThisTick.Any(w => w.Symbol == stock.Symbol)) return;

            var playerHasShort = Portfolio.Positions.TryGetValue(stock.Symbol, out var pos) && pos.IsShort;

            ShortSqueezeWarningsThisTick.Add(new ShortSqueezeWarning
            {
                Symbol = stock.Symbol,
                CompanyName = stock.Name,
                PriceChangePercent = Math.Round(hourlyChange * 100, 1),
                ShortInterestPercent = Math.Round(shortInterestPct * 100, 1),
                PlayerHasShortPosition = playerHasShort,
            });

            _log.Warn("SHORT SQUEEZE WARNING", new
            {
                symbol = stock.Symbol,
                hourlyChange = $"{hourlyChange:P1}",
                shortInterest = $"{shortInterestPct:P1}",
                playerShort = playerHasShort,
            });
        }
    }

    /// <summary>
    /// Bible 4.4.2: Activate SSR if stock falls ≥10% from PreviousClose.
    /// Lasts rest of day + next trading day.
    /// </summary>
    private void CheckSSRActivation(Stock stock)
    {
        if (stock.IsSSR) return; // Already under SSR
        if (stock.PreviousClose <= 0) return;

        var dropPercent = (stock.PreviousClose - stock.CurrentPrice) / stock.PreviousClose;
        if (dropPercent >= 0.10m)
        {
            stock.IsSSR = true;
            // SSR lasts rest of today + next trading day
            var today = GameTime.Date;
            var nextTradingDay = GetNextTradingDay(today);
            stock.SSRUntilDate = nextTradingDay.AddHours(16); // End of next trading day

            _log.Warn("SSR activated", new
            {
                symbol = stock.Symbol,
                drop = $"{dropPercent:P1}",
                previousClose = stock.PreviousClose,
                currentPrice = stock.CurrentPrice,
                until = stock.SSRUntilDate?.ToString("yyyy-MM-dd"),
            });
        }
    }

    /// <summary>
    /// Clear SSR flags on stocks whose restriction has expired.
    /// Called at market open each day.
    /// </summary>
    private void ClearExpiredSSR()
    {
        var today = GameTime.Date;
        foreach (var stock in MutableStocks)
        {
            if (stock.IsSSR && stock.SSRUntilDate.HasValue && today > stock.SSRUntilDate.Value.Date)
            {
                stock.IsSSR = false;
                stock.SSRUntilDate = null;
                _log.Info("SSR cleared", new { symbol = stock.Symbol });
            }
        }
    }

    /// <summary>Get next trading day (skips weekends).</summary>
    private static DateTime GetNextTradingDay(DateTime date)
    {
        var next = date.AddDays(1);
        while (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
            next = next.AddDays(1);
        return next;
    }

    /// <summary>List of stocks currently under SSR (for WebSocket).</summary>
    public List<string> SSRSymbols => MutableStocks.Where(s => s.IsSSR).Select(s => s.Symbol).ToList();

    private void CheckScenarioConditions(decimal currentEquity)
    {
        var s = ActiveScenario!;

        // Check WIN conditions
        bool won = false;
        if (s.TargetPortfolioValue.HasValue && currentEquity >= s.TargetPortfolioValue.Value)
            won = true;

        // Dividend King scenario: check quarterly dividend income
        if (s.TargetDividendIncome.HasValue)
        {
            // Approximate quarterly income: total dividends / quarters elapsed
            var quartersElapsed = Math.Max(1, s.DaysElapsed / 63m);
            var quarterlyIncome = _dividendEngine.TotalDividendsReceived / quartersElapsed;
            if (quarterlyIncome >= s.TargetDividendIncome.Value)
                won = true;
        }

        // Check time limit: survival scenarios win if time runs out with conditions met
        if (s.TimeLimitDays.HasValue && s.DaysElapsed >= s.TimeLimitDays.Value)
        {
            if (s.SurvivalMode && currentEquity > 0)
                won = true;
            else if (s.MaxLossPercent.HasValue)
            {
                var lossPct = (s.StartingCash - currentEquity) / s.StartingCash * 100;
                won = lossPct <= s.MaxLossPercent.Value;
            }
            else if (s.TargetPortfolioValue.HasValue && currentEquity >= s.TargetPortfolioValue.Value)
                won = true;
            else if (!s.SurvivalMode && !s.TargetPortfolioValue.HasValue)
                won = true; // No specific target, just survive to end

            // If time ran out and didn't win, it's a loss
            if (!won)
            {
                CompleteScenario(false, currentEquity,
                    s.TargetPortfolioValue.HasValue ? $"Did not reach ${s.TargetPortfolioValue:N0} (${currentEquity:N0})" :
                    s.MaxLossPercent.HasValue ? $"Lost more than {s.MaxLossPercent}% of starting capital" :
                    "Time expired");
                return;
            }
        }

        // Check LOSE conditions
        if (currentEquity <= 0)
        {
            CompleteScenario(false, currentEquity, "Bankrupt");
            return;
        }

        if (s.MaxLossPercent.HasValue)
        {
            var lossPct = (s.StartingCash - currentEquity) / s.StartingCash * 100;
            if (lossPct > s.MaxLossPercent.Value)
            {
                CompleteScenario(false, currentEquity, $"Exceeded maximum loss of {s.MaxLossPercent}%");
                return;
            }
        }

        if (won)
        {
            CompleteScenario(true, currentEquity, "");
        }
    }

    private void CompleteScenario(bool won, decimal finalEquity, string failReason)
    {
        var s = ActiveScenario!;
        s.IsCompleted = true;
        s.IsWon = won;

        var totalTrades = _achievementEngine.Stats.WinningTradeCount + _achievementEngine.Stats.LosingTradeCount;
        var winRate = totalTrades > 0
            ? Math.Round((decimal)_achievementEngine.Stats.WinningTradeCount / totalTrades * 100, 1)
            : 0m;

        ScenarioResult = new ScenarioResult
        {
            ScenarioId = s.Id,
            ScenarioName = s.Name,
            Won = won,
            DaysElapsed = s.DaysElapsed,
            FinalPortfolioValue = finalEquity,
            TotalReturn = finalEquity - s.StartingCash,
            TotalReturnPercent = s.StartingCash > 0 ? Math.Round((finalEquity - s.StartingCash) / s.StartingCash * 100, 2) : 0,
            TotalTrades = totalTrades,
            WinRate = winRate,
            FailReason = failReason,
        };

        // Pause game when scenario ends
        SetSpeed(GameSpeed.Paused);
        _log.Info("Scenario completed", new { id = s.Id, won, finalEquity, days = s.DaysElapsed });
    }

    private string GenerateSymbol(string prefix, string suffix, Random rng)
    {
        // Take consonants from prefix + first letter of suffix
        var consonants = new string(prefix.Where(c => !"aeiouAEIOU ".Contains(c)).Take(3).ToArray()).ToUpper();
        var suffixStart = suffix[..1].ToUpper();

        var symbol = consonants.Length >= 2
            ? consonants + suffixStart
            : (prefix[..2] + suffixStart).ToUpper();

        // Ensure 3-5 chars
        if (symbol.Length < 3) symbol += "X";
        if (symbol.Length > 5) symbol = symbol[..5];

        return symbol;
    }
}

public class ShareholderVote
{
    public string Symbol { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Proposal { get; set; } = "";
    public string VoteType { get; set; } = "";
    public decimal OwnershipPercent { get; set; }
    public float PriceImpactIfApproved { get; set; }
}
