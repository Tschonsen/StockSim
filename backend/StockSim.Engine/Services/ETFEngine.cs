using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages Exchange-Traded Funds that track sector averages and the overall market.
/// ETFs are tradeable like stocks but their prices are derived from their constituent stocks.
/// </summary>
public class ETFEngine
{
    private readonly Logger _log = new("ETFEngine");
    private readonly List<Stock> _etfs = new();
    private readonly Dictionary<string, ETFDefinition> _definitions = new();

    private const decimal ETFHalfSpreadRatio = 0.0005m;
    private readonly Dictionary<string, CommodityETFDef> _commodityDefs = new();

    public IReadOnlyList<Stock> ETFs => _etfs;

    /// <summary>Get constituent symbols for an ETF. Returns empty if not found.</summary>
    public List<string> GetConstituents(string etfSymbol)
    {
        return _definitions.TryGetValue(etfSymbol, out var def) ? def.ConstituentSymbols : new();
    }

    /// <summary>
    /// Create ETFs for all sectors plus a market-wide index ETF.
    /// Call this after stocks are generated.
    /// </summary>
    public List<Stock> CreateETFs(IReadOnlyList<Stock> stocks)
    {
        _etfs.Clear();
        _definitions.Clear();

        // Market-wide index ETF
        CreateETF("SIMX", "StockSim Total Market ETF", "ETF", stocks.ToList(), isIndex: true);

        // Sector ETFs
        var sectorMap = new Dictionary<string, (string symbol, string name)>
        {
            ["Technology"] = ("STEC", "SimTech Sector ETF"),
            ["Energy"] = ("SENG", "SimEnergy Sector ETF"),
            ["Financials"] = ("SFIN", "SimFinancial Sector ETF"),
            ["Healthcare"] = ("SHLT", "SimHealth Sector ETF"),
            ["Consumer Goods"] = ("SCON", "SimConsumer Sector ETF"),
            ["Industrials"] = ("SIND", "SimIndustrial Sector ETF"),
            ["Materials"] = ("SMAT", "SimMaterials Sector ETF"),
            ["Real Estate"] = ("SREL", "SimReal Estate ETF"),
            ["Telecommunications"] = ("STEL", "SimTelecom Sector ETF"),
            ["Utilities"] = ("SUTL", "SimUtilities Sector ETF"),
            ["Luxury Goods"] = ("SLUX", "SimLuxury Sector ETF"),
            ["Transportation"] = ("STRN", "SimTransport Sector ETF"),
        };

        foreach (var (sector, info) in sectorMap)
        {
            var sectorStocks = stocks.Where(s => s.Sector == sector).ToList();
            if (sectorStocks.Count > 0)
            {
                CreateETF(info.symbol, info.name, sector, sectorStocks, isIndex: false);
            }
        }

        _log.Info("ETFs created", new { count = _etfs.Count, totalMarketETF = "SIMX" });
        return _etfs;
    }

    /// <summary>
    /// Create commodity ETFs that track economic indicators (Gold, Silver, Oil).
    /// Call after CreateETFs. Returns the created stocks.
    /// </summary>
    public List<Stock> CreateCommodityETFs(Models.EconomicData economicData)
    {
        var commodityETFs = new List<Stock>();

        var commodities = new[]
        {
            (symbol: "GLD", name: "SPDR Gold Shares", price: economicData.GoldPrice / 10m, baseVol: 0.013m, underlying: "Gold"),
            (symbol: "SLV", name: "iShares Silver Trust", price: economicData.GoldPrice * 0.035m / 3m, baseVol: 0.018m, underlying: "Silver"),
            (symbol: "USO", name: "United States Oil Fund", price: economicData.OilPrice * 0.7m, baseVol: 0.020m, underlying: "Oil"),
        };

        foreach (var (symbol, name, price, baseVol, underlying) in commodities)
        {
            var etfPrice = Math.Round(Math.Max(5m, price), 2);
            var etf = new Stock(symbol, name, "Commodities");
            etf.Subsector = underlying;
            etf.CurrentPrice = etfPrice;
            etf.PreviousClose = etfPrice;
            etf.FairValue = etfPrice;
            etf.DayHigh = etfPrice;
            etf.DayLow = etfPrice;
            etf.SharesOutstanding = 100_000_000;
            etf.AverageVolume = 8_000_000;
            etf.LiquidityScore = 9;
            etf.BaseVolatility = baseVol;
            etf.InsiderOwnership = 0;
            etf.InstitutionalOwnership = 0.70m;
            etf.ShortInterest = 0.05m;
            etf.ShortBorrowAvailability = 0.90m;
            etf.Revenue = 0;
            etf.NetIncome = 0;
            etf.DividendYield = 0;
            etf.DebtToEquity = 0;
            etf.RevenueGrowth = 0;
            etf.Employees = 0;
            etf.Traits.Add("ETF");
            etf.Traits.Add("Commodity ETF");

            var halfSpread = etfPrice * ETFHalfSpreadRatio * 1.5m; // Slightly wider spread for commodities
            etf.BidPrice = Math.Round(etfPrice - halfSpread, 2);
            etf.AskPrice = Math.Round(etfPrice + halfSpread, 2);

            _etfs.Add(etf);
            _commodityDefs[symbol] = new CommodityETFDef
            {
                Symbol = symbol,
                Underlying = underlying,
                InitialCommodityPrice = underlying switch
                {
                    "Gold" => economicData.GoldPrice,
                    "Silver" => economicData.GoldPrice * 0.035m,
                    "Oil" => economicData.OilPrice,
                    _ => 100m,
                },
                InitialETFPrice = etfPrice,
            };
            commodityETFs.Add(etf);
        }

        _log.Info("Commodity ETFs created", new { count = commodityETFs.Count, symbols = "GLD, SLV, USO" });
        return commodityETFs;
    }

    private void CreateETF(string symbol, string name, string sector, List<Stock> constituents, bool isIndex)
    {
        // Calculate initial ETF price as weighted average of constituents
        var totalMcap = constituents.Sum(s => s.MarketCap);
        var weightedPrice = totalMcap > 0
            ? constituents.Sum(s => s.CurrentPrice * (s.MarketCap / totalMcap))
            : constituents.Average(s => s.CurrentPrice);

        // Scale to a reasonable ETF price ($50-$200 range)
        var scaleFactor = 100m / Math.Max(weightedPrice, 0.01m);
        var etfPrice = Math.Round(weightedPrice * scaleFactor, 2);
        etfPrice = Math.Max(10m, Math.Min(etfPrice, 500m));

        var etf = new Stock(symbol, name, sector);
        etf.CurrentPrice = etfPrice;
        etf.PreviousClose = etfPrice;
        etf.FairValue = etfPrice;
        etf.DayHigh = etfPrice;
        etf.DayLow = etfPrice;
        etf.SharesOutstanding = 50_000_000;
        etf.AverageVolume = 5_000_000;
        etf.LiquidityScore = 10;
        etf.BaseVolatility = isIndex ? 0.008m : 0.012m;
        etf.InsiderOwnership = 0;
        etf.InstitutionalOwnership = 0.85m;
        etf.ShortBorrowAvailability = 0.95m;
        etf.Revenue = 0;
        etf.NetIncome = 0;
        etf.DividendYield = constituents.Average(s => s.DividendYield);
        etf.DebtToEquity = 0;
        etf.RevenueGrowth = 0;
        etf.Employees = 0;
        etf.Traits.Add("ETF");
        if (isIndex) etf.Traits.Add("Index Fund");
        else etf.Traits.Add("Sector Fund");

        var halfSpread = etf.CurrentPrice * ETFHalfSpreadRatio;
        etf.BidPrice = Math.Round(etf.CurrentPrice - halfSpread, 2);
        etf.AskPrice = Math.Round(etf.CurrentPrice + halfSpread, 2);

        _etfs.Add(etf);
        _definitions[symbol] = new ETFDefinition
        {
            Symbol = symbol,
            ConstituentSymbols = constituents.Select(s => s.Symbol).ToList(),
            ScaleFactor = scaleFactor,
            InitialPrice = etfPrice,
            InitialTotalMarketCap = totalMcap,
            IsIndex = isIndex,
        };
    }

    /// <summary>
    /// Update ETF prices based on their constituent stocks.
    /// Called each tick from GameLoop.
    /// </summary>
    public void UpdatePrices(Dictionary<string, Stock> stocksBySymbol, Models.EconomicData? economicData = null)
    {
        // Update commodity ETFs from economic indicators
        if (economicData != null)
        {
            foreach (var (symbol, cDef) in _commodityDefs)
            {
                var etf = _etfs.FirstOrDefault(e => e.Symbol == symbol);
                if (etf == null) continue;

                var currentCommodityPrice = cDef.Underlying switch
                {
                    "Gold" => economicData.GoldPrice,
                    "Silver" => economicData.GoldPrice * 0.035m,
                    "Oil" => economicData.OilPrice,
                    _ => cDef.InitialCommodityPrice,
                };

                // Price tracks commodity ratio × initial ETF price + small noise
                var ratio = cDef.InitialCommodityPrice > 0 ? currentCommodityPrice / cDef.InitialCommodityPrice : 1m;
                var noise = 1m + ((decimal)_rng.NextDouble() - 0.5m) * 0.002m; // ±0.1% tick noise
                var newPrice = Math.Round(cDef.InitialETFPrice * ratio * noise, 2);
                newPrice = Math.Max(0.50m, newPrice);

                etf.CurrentPrice = newPrice;
                etf.DayHigh = Math.Max(etf.DayHigh, newPrice);
                etf.DayLow = Math.Min(etf.DayLow, newPrice);

                var halfSpread = newPrice * ETFHalfSpreadRatio * 1.5m;
                etf.BidPrice = Math.Round(newPrice - halfSpread, 2);
                etf.AskPrice = Math.Round(newPrice + halfSpread, 2);

                // Commodity ETFs get high volume
                etf.DayVolume += _rng.Next(5000, 20000);
            }
        }

        // Update sector/index ETFs from constituents
        foreach (var etf in _etfs)
        {
            if (_commodityDefs.ContainsKey(etf.Symbol)) continue; // Skip commodity ETFs
            if (!_definitions.TryGetValue(etf.Symbol, out var def)) continue;

            // Calculate new weighted price
            decimal totalMcap = 0;
            decimal weightedSum = 0;
            int activeCount = 0;

            foreach (var sym in def.ConstituentSymbols)
            {
                if (stocksBySymbol.TryGetValue(sym, out var stock))
                {
                    var mcap = stock.MarketCap;
                    totalMcap += mcap;
                    weightedSum += stock.CurrentPrice * mcap;
                    activeCount++;
                }
            }

            if (activeCount == 0 || totalMcap == 0) continue;

            // Market-cap-weighted index: ETF tracks total market cap change, not weighted price
            // This avoids the Price^2 feedback loop that caused +141,000% spikes
            var newEtfPrice = def.InitialTotalMarketCap > 0
                ? Math.Round(def.InitialPrice * (totalMcap / def.InitialTotalMarketCap), 2)
                : Math.Round(weightedSum / totalMcap * def.ScaleFactor, 2);
            newEtfPrice = Math.Max(0.01m, newEtfPrice);

            etf.CurrentPrice = newEtfPrice;
            etf.DayHigh = Math.Max(etf.DayHigh, newEtfPrice);
            etf.DayLow = Math.Min(etf.DayLow, newEtfPrice);

            // Update bid/ask
            var halfSpread = newEtfPrice * ETFHalfSpreadRatio;
            etf.BidPrice = Math.Round(newEtfPrice - halfSpread, 2);
            etf.AskPrice = Math.Round(newEtfPrice + halfSpread, 2);

            // Update volume (proportional to constituent volume)
            var constituentVols = def.ConstituentSymbols
                .Select(s => stocksBySymbol.TryGetValue(s, out var st) ? st.DayVolume : 0L)
                .ToList();
            var avgConstituentVol = constituentVols.Count > 0 ? constituentVols.Average() : 0.0;
            etf.DayVolume = (long)(avgConstituentVol * 0.3);
        }
    }

    /// <summary>
    /// Reset daily values for ETFs at market open.
    /// </summary>
    public void ResetDailyValues()
    {
        foreach (var etf in _etfs)
        {
            etf.PreviousClose = etf.CurrentPrice;
            etf.DayHigh = etf.CurrentPrice;
            etf.DayLow = etf.CurrentPrice;
            etf.DayVolume = 0;
        }
    }

    // === INDEX REBALANCING + ETF FLOW EFFECTS ===

    /// <summary>News events from rebalancing (for frontend).</summary>
    public List<string> RebalanceNewsThisTick { get; } = new();

    /// <summary>Per-stock flow pressure from ETF rebalancing. Key: symbol, Value: -1 to +1 multiplier.</summary>
    public Dictionary<string, decimal> FlowPressure { get; } = new();

    private int _daysSinceRebalance;
    private readonly Random _rng = new();

    /// <summary>
    /// Quarterly index rebalancing: re-evaluate constituents, generate flows.
    /// Called daily from GameLoop. Active rebalancing happens every ~63 trading days.
    /// </summary>
    public void TickRebalancing(Dictionary<string, Stock> stocksBySymbol, int tradingDay)
    {
        RebalanceNewsThisTick.Clear();
        FlowPressure.Clear();
        _daysSinceRebalance++;

        // Quarterly rebalancing (~63 trading days)
        if (_daysSinceRebalance < 63) return;
        _daysSinceRebalance = 0;

        _log.Info("Index rebalancing triggered", new { tradingDay });

        foreach (var (etfSymbol, def) in _definitions)
        {
            if (def.IsIndex) continue; // Market index tracks everything, no rebalancing

            var sectorStocks = stocksBySymbol.Values
                .Where(s => !s.Traits.Contains("ETF") && s.Sector == _etfs.FirstOrDefault(e => e.Symbol == etfSymbol)?.Sector)
                .OrderByDescending(s => s.MarketCap)
                .ToList();

            var currentConstituents = new HashSet<string>(def.ConstituentSymbols);
            var newConstituents = sectorStocks.Select(s => s.Symbol).ToHashSet();

            // Find additions and removals
            var additions = newConstituents.Except(currentConstituents).ToList();
            var removals = currentConstituents.Except(newConstituents).ToList();

            if (additions.Count == 0 && removals.Count == 0) continue;

            // Update constituents
            def.ConstituentSymbols = sectorStocks.Select(s => s.Symbol).ToList();
            def.InitialTotalMarketCap = sectorStocks.Sum(s => s.MarketCap);

            // Generate flow pressure: additions get buying pressure, removals get selling
            foreach (var sym in additions)
            {
                FlowPressure[sym] = 0.005m + (decimal)(_rng.NextDouble() * 0.01); // +0.5% to +1.5%
                if (stocksBySymbol.TryGetValue(sym, out var stock))
                    RebalanceNewsThisTick.Add($"{stock.Name} ({sym}) added to {etfSymbol} — passive fund buying expected");
            }
            foreach (var sym in removals)
            {
                FlowPressure[sym] = -(0.005m + (decimal)(_rng.NextDouble() * 0.01)); // -0.5% to -1.5%
                if (stocksBySymbol.TryGetValue(sym, out var stock))
                    RebalanceNewsThisTick.Add($"{stock.Name} ({sym}) removed from {etfSymbol} — index fund selling expected");
            }

            _log.Info("Sector ETF rebalanced", new { etf = etfSymbol, additions = additions.Count, removals = removals.Count });
        }

        // Month-end rebalancing flow: all constituents get slight volume spike
        foreach (var def in _definitions.Values)
        {
            foreach (var sym in def.ConstituentSymbols)
            {
                if (!FlowPressure.ContainsKey(sym))
                    FlowPressure[sym] = (decimal)(_rng.NextDouble() - 0.5) * 0.002m; // ±0.1% noise
            }
        }

        if (RebalanceNewsThisTick.Count > 0)
            RebalanceNewsThisTick.Insert(0, $"QUARTERLY INDEX REBALANCING: {RebalanceNewsThisTick.Count} membership changes across sector ETFs");
    }

    /// <summary>
    /// Apply flow pressure to stock prices. Called each tick during rebalancing window (5 trading days).
    /// The pressure decays linearly over the window.
    /// </summary>
    public void ApplyFlowPressure(Dictionary<string, Stock> stocksBySymbol)
    {
        if (FlowPressure.Count == 0) return;

        foreach (var (sym, pressure) in FlowPressure)
        {
            if (stocksBySymbol.TryGetValue(sym, out var stock))
            {
                // Apply flow as daily price drift spread over 5 days
                var dailyPressure = stock.CurrentPrice * pressure / 5m;
                stock.CurrentPrice = Math.Max(0.01m, Math.Round(stock.CurrentPrice + dailyPressure, 2));

                // Flow also increases volume
                stock.DayVolume += (long)(stock.AverageVolume * Math.Abs((double)pressure) * 2);
            }
        }

        // Decay: reduce flow pressure each day
        var keys = FlowPressure.Keys.ToList();
        foreach (var key in keys)
        {
            FlowPressure[key] *= 0.7m;
            if (Math.Abs(FlowPressure[key]) < 0.0001m)
                FlowPressure.Remove(key);
        }
    }
}

/// <summary>
/// Internal ETF definition tracking constituents and pricing.
/// </summary>
internal class ETFDefinition
{
    public string Symbol { get; set; } = "";
    public List<string> ConstituentSymbols { get; set; } = new();
    public decimal ScaleFactor { get; set; }
    public decimal InitialPrice { get; set; }
    public decimal InitialTotalMarketCap { get; set; }
    public bool IsIndex { get; set; }
}

internal class CommodityETFDef
{
    public string Symbol { get; set; } = "";
    public string Underlying { get; set; } = "";
    public decimal InitialCommodityPrice { get; set; }
    public decimal InitialETFPrice { get; set; }
}
