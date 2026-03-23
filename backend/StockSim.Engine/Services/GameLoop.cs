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
    private readonly Logger _log = new("GameLoop");
    private readonly int _seed;

    public IReadOnlyList<Stock> Stocks { get; }
    public Dictionary<string, PriceHistory> PriceHistories { get; } = new();
    public Dictionary<string, List<Candle>> DailyHistory { get; } = new();
    public Portfolio Portfolio { get; }
    public OrderEngine OrderEngine { get; }
    public EventEngine EventEngine => _eventEngine;
    public MarketPhase Phase { get; }
    public DateTime GameTime { get; private set; }
    public GameSpeed Speed { get; private set; } = GameSpeed.Paused;
    public bool IsPaused => Speed == GameSpeed.Paused;
    public long TickCount { get; private set; }

    private static readonly string[] Sectors = new[]
    {
        "Technology", "Energy", "Financials", "Healthcare",
        "Consumer Goods", "Industrials", "Materials", "Real Estate",
        "Telecommunications", "Utilities", "Luxury Goods", "Transportation"
    };

    public GameLoop(int seed, int stockCount = 250, decimal startingCash = 50_000m)
    {
        _seed = seed;
        _priceEngine = new PriceEngine(seed);
        _eventEngine = new EventEngine(seed + 5000);
        _aiTraderEngine = new AITraderEngine(seed + 7000);

        // Start on a Monday at market pre-open
        GameTime = new DateTime(2027, 1, 4, 9, 0, 0); // Mon, Jan 4 2027

        // Initialize portfolio and order engine (Bible 4.1)
        Portfolio = new Portfolio(startingCash);
        OrderEngine = new OrderEngine(Portfolio);

        // Determine market phase (Bible 11.4: Bull 40%, Neutral 40%, Bear 20%)
        Phase = HistoryGenerator.DeterminePhase(seed);

        var stocks = GenerateStocks(seed, stockCount);
        Stocks = stocks.AsReadOnly();

        // Initialize price history for each stock (1-minute candles)
        foreach (var stock in stocks)
        {
            PriceHistories[stock.Symbol] = new PriceHistory(stock.Symbol, CandleInterval.OneMinute);
        }

        // Generate 252 trading days of historical daily candles (Bible 11.4)
        GenerateHistoricalPrices(seed, stocks);

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

        // 1. Advance game time by 1 minute
        GameTime = GameTime.AddMinutes(1);

        // 2. Check if market is open (9:30 AM - 4:00 PM, weekdays)
        if (!IsMarketOpen())
        {
            TickCount++;
            return;
        }

        // 3. At market open (9:31): reset daily values and execute pending orders
        if (GameTime.TimeOfDay == new TimeSpan(9, 31, 0))
        {
            foreach (var stock in Stocks)
            {
                _priceEngine.ResetDailyValues(stock);
                OrderEngine.ExecutePendingOrders(stock, GameTime, isMarketOpen: true);
            }
        }

        // 4. Update all stock prices and record candle data
        var tickDuration = TimeSpan.FromMinutes(1);
        var unixTime = new DateTimeOffset(GameTime).ToUnixTimeSeconds();
        foreach (var stock in Stocks)
        {
            _priceEngine.Tick(stock, tickDuration);

            // Record candle data
            if (PriceHistories.TryGetValue(stock.Symbol, out var history))
            {
                history.UpdateTick(stock.CurrentPrice, unixTime, stock.DayVolume);
            }

            // 5. Check stop orders and limit orders against updated prices
            OrderEngine.CheckStopOrders(stock, GameTime, isMarketOpen: true);
            OrderEngine.CheckLimitOrders(stock, GameTime, isMarketOpen: true);
        }

        // 6. Process events (Bible 8.1)
        _eventEngine.Tick(Stocks, GameTime, isMarketOpen: true);

        // 7. AI Traders: adjust spreads, volume, sentiment pressure (Bible 7)
        _aiTraderEngine.Tick(Stocks, _eventEngine.ActiveEvents, isMarketOpen: true);

        // 8. Expire day orders at market close
        if (GameTime.TimeOfDay == new TimeSpan(16, 0, 0))
        {
            OrderEngine.ExpireDayOrders();
        }

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

    public bool IsPreMarket()
    {
        var day = GameTime.DayOfWeek;
        if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
            return false;

        var time = GameTime.TimeOfDay;
        return time >= new TimeSpan(7, 0, 0) && time < new TimeSpan(9, 30, 0);
    }

    public bool IsAfterHours()
    {
        var day = GameTime.DayOfWeek;
        if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
            return false;

        var time = GameTime.TimeOfDay;
        return time >= new TimeSpan(16, 0, 0) && time < new TimeSpan(20, 0, 0);
    }

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

        // Shares outstanding (derive price from market cap)
        stock.SharesOutstanding = (long)(rng.NextDouble() * 900_000_000 + 100_000_000);
        stock.CurrentPrice = Math.Round(marketCapBillions * 1_000_000_000m / stock.SharesOutstanding, 2);
        stock.CurrentPrice = Math.Max(stock.CurrentPrice, 0.50m);
        stock.CurrentPrice = Math.Min(stock.CurrentPrice, 5000m);

        stock.PreviousClose = stock.CurrentPrice;
        stock.DayHigh = stock.CurrentPrice;
        stock.DayLow = stock.CurrentPrice;
        stock.FairValue = stock.CurrentPrice;

        // Ownership structure (Bible 5.8)
        stock.InsiderOwnership = (decimal)(rng.NextDouble() * 0.25 + 0.05);
        stock.InstitutionalOwnership = (decimal)(rng.NextDouble() * 0.50 + 0.20);

        // Volatility (sector-dependent, Bible 11.2.2)
        var sectorVolBase = stock.Sector switch
        {
            "Technology" => 0.025,
            "Energy" => 0.030,
            "Healthcare" => 0.028,
            "Financials" => 0.020,
            "Consumer Goods" => 0.015,
            "Industrials" => 0.018,
            "Materials" => 0.025,
            "Real Estate" => 0.020,
            "Telecommunications" => 0.012,
            "Utilities" => 0.010,
            "Luxury Goods" => 0.022,
            "Transportation" => 0.020,
            _ => 0.020,
        };
        stock.BaseVolatility = (decimal)(sectorVolBase * (0.5 + rng.NextDouble()));

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

        // Fundamentals
        stock.Revenue = marketCapBillions * (decimal)(rng.NextDouble() * 0.3 + 0.1) * 1_000_000_000m;
        stock.NetIncome = stock.Revenue * (decimal)(rng.NextDouble() * 0.2 - 0.02);
        stock.DividendYield = rng.NextDouble() < 0.4 ? (decimal)(rng.NextDouble() * 0.06) : 0m;
        stock.DebtToEquity = (decimal)(rng.NextDouble() * 2.0);
        stock.RevenueGrowth = (decimal)(rng.NextDouble() * 0.4 - 0.1);
        stock.Employees = (int)(marketCapBillions * (decimal)(rng.NextDouble() * 500 + 100));
        stock.ShortBorrowAvailability = (decimal)(rng.NextDouble() * 0.5 + 0.5);

        // Assign 1-3 traits (Bible 11.3.4)
        AssignTraits(rng, stock, marketCapBillions);

        // Initial bid/ask
        var halfSpread = stock.CurrentPrice * 0.001m;
        stock.BidPrice = Math.Round(stock.CurrentPrice - halfSpread, 2);
        stock.AskPrice = Math.Round(stock.CurrentPrice + halfSpread, 2);
    }

    private void AssignTraits(Random rng, Stock stock, decimal marketCapB)
    {
        var possibleTraits = new List<string>();

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

        // Default: at least one trait
        if (possibleTraits.Count == 0) possibleTraits.Add("Compounder");

        // Pick 1-3 traits
        var traitCount = Math.Min(rng.Next(1, 4), possibleTraits.Count);
        var shuffled = possibleTraits.OrderBy(_ => rng.Next()).Take(traitCount);
        foreach (var trait in shuffled)
        {
            stock.Traits.Add(trait);
        }
    }

    private static readonly Dictionary<string, string[][]> NameParts = new()
    {
        ["Technology"] = new[] {
            new[] { "Vertex", "Nova", "Quantum", "Cyber", "Nexus", "Apex", "Synth", "Pixel", "Cloud", "Data", "Neural", "Helix", "Core", "Edge", "Smart" },
            new[] { "Dynamics", "Systems", "Technologies", "Labs", "Solutions", "Logic", "Networks", "Soft", "AI", "Tech", "Ware", "Digital", "Platform" }
        },
        ["Energy"] = new[] {
            new[] { "Petro", "Solar", "Volt", "Hydro", "Geo", "Wind", "Fuel", "Terra", "Ion", "Atom", "Green", "Flux", "Therm", "Eco", "Power" },
            new[] { "Energy", "Power", "Resources", "Oil", "Gas", "Corp", "Renewables", "Fuels", "Grid", "Stream", "Force", "Solutions" }
        },
        ["Financials"] = new[] {
            new[] { "Capital", "First", "Global", "Premier", "Trust", "Crown", "Sterling", "Pacific", "Atlantic", "Summit", "Eagle", "Sovereign", "Prime" },
            new[] { "Bank", "Financial", "Holdings", "Capital", "Group", "Trust", "Securities", "Advisors", "Partners", "Corp", "Wealth" }
        },
        ["Healthcare"] = new[] {
            new[] { "Bio", "Nova", "Medi", "Vita", "Pulse", "Neura", "Cell", "Genome", "Helix", "Immuno", "Pharma", "Cardio", "Synapse", "Astra" },
            new[] { "Pharma", "Therapeutics", "Sciences", "Biotech", "Labs", "Medical", "Diagnostics", "Health", "Genomics", "Cure", "Rx" }
        },
        ["Consumer Goods"] = new[] {
            new[] { "Bright", "Prime", "Fresh", "Urban", "Ever", "Home", "Pure", "Daily", "Golden", "Nature", "Bloom", "Clear", "Swift", "True" },
            new[] { "Brands", "Products", "Foods", "Consumer", "Essentials", "Goods", "Co", "Corp", "Industries", "Market", "Direct" }
        },
        ["Industrials"] = new[] {
            new[] { "Iron", "Steel", "Forge", "Titan", "Atlas", "Apex", "Core", "Granite", "Bolt", "Arc", "Matrix", "Omega", "Vanguard", "Summit" },
            new[] { "Industries", "Manufacturing", "Engineering", "Works", "Fabrication", "Machinery", "Industrial", "Solutions", "Dynamics" }
        },
        ["Materials"] = new[] {
            new[] { "Terra", "Geo", "Crystal", "Ore", "Mineral", "Carbon", "Alloy", "Stone", "Metal", "Prism", "Element", "Cobalt", "Quarry", "Onyx" },
            new[] { "Materials", "Mining", "Resources", "Metals", "Minerals", "Chemical", "Composites", "Industries", "Extraction", "Corp" }
        },
        ["Real Estate"] = new[] {
            new[] { "Crown", "Harbor", "Summit", "Urban", "Metro", "Skyline", "Park", "Beacon", "Crest", "Haven", "Tower", "Pinnacle", "Grand" },
            new[] { "Realty", "Properties", "Real Estate", "Holdings", "Development", "Estates", "Trust", "Capital", "REIT", "Property" }
        },
        ["Telecommunications"] = new[] {
            new[] { "Signal", "Wave", "Link", "Net", "Tele", "Beam", "Fiber", "Pulse", "Echo", "Relay", "Orbit", "Spectrum", "Grid", "Omni" },
            new[] { "Communications", "Telecom", "Networks", "Wireless", "Connect", "Broadband", "Media", "Signal", "Mobile", "Digital" }
        },
        ["Utilities"] = new[] {
            new[] { "Power", "Grid", "Hydro", "Volt", "Amp", "Current", "Flow", "Source", "Green", "Clean", "Civic", "Metro", "National", "Central" },
            new[] { "Utilities", "Power", "Electric", "Energy", "Water", "Gas", "Services", "Utility", "Corp", "Light", "Generation" }
        },
        ["Luxury Goods"] = new[] {
            new[] { "Prestige", "Royal", "Maison", "Luxe", "Elite", "Noble", "Grand", "Imperial", "Regal", "Opulent", "Crown", "Sterling" },
            new[] { "Luxury", "Group", "Brands", "Collection", "Maison", "House", "Atelier", "Design", "International", "Premium" }
        },
        ["Transportation"] = new[] {
            new[] { "Trans", "Global", "Swift", "Rapid", "Fleet", "Cargo", "Express", "Rail", "Aero", "Maritime", "Voyage", "Route", "Track" },
            new[] { "Transport", "Logistics", "Shipping", "Freight", "Airlines", "Rail", "Corp", "Transit", "Carriers", "Express", "Mobility" }
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

    private void GenerateHistoricalPrices(int seed, List<Stock> stocks)
    {
        // Each stock gets its own seeded generator for reproducibility
        for (int i = 0; i < stocks.Count; i++)
        {
            var stock = stocks[i];
            var historyGen = new HistoryGenerator(seed: seed + i + 1000);
            var candles = historyGen.GenerateDaily(stock, GameTime, Phase);
            DailyHistory[stock.Symbol] = candles;
        }

        _log.Info("Historical prices generated", new
        {
            stocks = stocks.Count,
            candlesPerStock = 252,
            phase = Phase.ToString(),
        });
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
