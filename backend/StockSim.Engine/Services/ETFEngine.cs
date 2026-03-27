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

    public IReadOnlyList<Stock> ETFs => _etfs;

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

        var halfSpread = etf.CurrentPrice * 0.0005m;
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
    public void UpdatePrices(Dictionary<string, Stock> stocksBySymbol)
    {
        foreach (var etf in _etfs)
        {
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
            var halfSpread = newEtfPrice * 0.0005m;
            etf.BidPrice = Math.Round(newEtfPrice - halfSpread, 2);
            etf.AskPrice = Math.Round(newEtfPrice + halfSpread, 2);

            // Update volume (proportional to constituent volume)
            var avgConstituentVol = def.ConstituentSymbols
                .Select(s => stocksBySymbol.TryGetValue(s, out var st) ? st.DayVolume : 0L)
                .Average();
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
