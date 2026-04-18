using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages IPO (new stocks) and Delisting (stock removal) events.
/// Spec 8.2.8: IPO every 30-60 days, Delisting rare.
/// </summary>
public class IPOEngine
{
    private readonly Random _rng;
    private readonly Logger _log = new("IPOEngine");
    private int _daysSinceLastIPO;
    private int _nextIPOInterval;

    /// <summary>Pending IPO announcements (name, sector, expected price, listing date).</summary>
    public List<PendingIPO> PendingIPOs { get; } = new();

    /// <summary>Pending delistings.</summary>
    public List<PendingDelisting> PendingDelistings { get; } = new();

    /// <summary>New IPO stocks added this tick.</summary>
    public List<Stock> NewIPOsThisTick { get; } = new();

    /// <summary>Delistings executed this tick.</summary>
    public List<string> DelistedThisTick { get; } = new();

    /// <summary>News headlines generated this tick.</summary>
    public List<string> NewsThisTick { get; } = new();

    private static readonly string[][] IPONames = new[]
    {
        new[] { "Nova", "Apex", "Zenith", "Prism", "Quantum", "Nexus", "Helix", "Spark", "Flux", "Orbit" },
        new[] { "AI", "Robotics", "Bio", "Space", "Crypto", "Cloud", "Green", "Smart", "Cyber", "Meta" },
        new[] { "Labs", "Tech", "Systems", "Dynamics", "Solutions", "Corp", "Inc.", "Group", "Health", "Energy" },
    };

    public IPOEngine(int seed)
    {
        _rng = new Random(seed);
        _nextIPOInterval = 30 + _rng.Next(30); // 30-60 days
    }

    /// <summary>Called once per trading day at market open.</summary>
    public void TickDay(List<Stock> stocks, Portfolio portfolio, DateTime gameTime)
    {
        NewIPOsThisTick.Clear();
        DelistedThisTick.Clear();
        NewsThisTick.Clear();

        _daysSinceLastIPO++;

        // Process pending IPOs
        foreach (var ipo in PendingIPOs.Where(i => i.ListingDate.Date <= gameTime.Date && !i.Listed).ToList())
        {
            var stock = CreateIPOStock(ipo);
            stocks.Add(stock);
            ipo.Listed = true;
            NewIPOsThisTick.Add(stock);
            NewsThisTick.Add($"IPO: {stock.Name} ({stock.Symbol}) begins trading at ${stock.CurrentPrice:F2}");
            _log.Info("IPO listed", new { symbol = stock.Symbol, name = stock.Name, price = stock.CurrentPrice });
        }
        PendingIPOs.RemoveAll(i => i.Listed);

        // Process pending delistings
        foreach (var dl in PendingDelistings.Where(d => d.DelistDate.Date <= gameTime.Date && !d.Executed).ToList())
        {
            // Force-sell player position
            if (portfolio.Positions.TryGetValue(dl.Symbol, out var pos))
            {
                var stock = stocks.FirstOrDefault(s => s.Symbol == dl.Symbol);
                var sellPrice = stock?.CurrentPrice ?? 0.01m;
                var proceeds = Math.Abs(pos.Shares) * sellPrice;
                portfolio.Cash += proceeds;
                portfolio.Positions.Remove(dl.Symbol);
                NewsThisTick.Add($"Position in {dl.Symbol} liquidated at ${sellPrice:F2} due to delisting");
            }

            stocks.RemoveAll(s => s.Symbol == dl.Symbol);
            dl.Executed = true;
            DelistedThisTick.Add(dl.Symbol);
            NewsThisTick.Add($"{dl.Symbol} has been delisted from the exchange");
            _log.Info("Stock delisted", new { symbol = dl.Symbol });
        }
        PendingDelistings.RemoveAll(d => d.Executed);

        // Generate new IPO announcement
        if (_daysSinceLastIPO >= _nextIPOInterval)
        {
            _daysSinceLastIPO = 0;
            _nextIPOInterval = 30 + _rng.Next(30);
            AnnounceIPO(stocks, gameTime);
        }

        // IPO Lock-up expiry: insider selling flood after 180 days
        foreach (var stock in stocks)
        {
            if (stock.LockUpExpiry.HasValue && !stock.LockUpExpired && gameTime.Date >= stock.LockUpExpiry.Value.Date)
            {
                stock.LockUpExpired = true;
                stock.Traits.Remove("IPO Fresh");
                // Insider selling pressure: -5 to -15% over next few days
                var sellPressure = 0.05m + (decimal)(_rng.NextDouble() * 0.10);
                stock.CurrentPrice = Math.Max(0.01m, Math.Round(stock.CurrentPrice * (1m - sellPressure), 2));
                stock.InsiderOwnership *= 0.7m; // Insiders sell ~30% of their holdings
                NewsThisTick.Add($"LOCK-UP EXPIRY: {stock.Name} ({stock.Symbol}) insiders now free to sell. Stock drops {sellPressure:P0} on heavy insider selling volume.");
                _log.Info("Lock-up expired", new { symbol = stock.Symbol, sellPressure = $"{sellPressure:P1}" });
            }
        }

        // Rare delisting check (stocks trading < $0.50 for extended periods)
        if (_rng.NextDouble() < 0.002) // ~1 every 500 days
        {
            var cheapStocks = stocks.Where(s => s.CurrentPrice < 0.50m && !PendingDelistings.Any(d => d.Symbol == s.Symbol)).ToList();
            if (cheapStocks.Count > 0)
            {
                var target = cheapStocks[_rng.Next(cheapStocks.Count)];
                var delistDate = GetNextTradingDay(gameTime, 10);
                PendingDelistings.Add(new PendingDelisting { Symbol = target.Symbol, DelistDate = delistDate });
                NewsThisTick.Add($"WARNING: {target.Name} ({target.Symbol}) faces delisting on {delistDate:MMM dd}. Trading will be suspended.");
                _log.Info("Delisting announced", new { symbol = target.Symbol, date = delistDate.ToString("yyyy-MM-dd") });
            }
        }
    }

    private void AnnounceIPO(List<Stock> stocks, DateTime gameTime)
    {
        var name = GenerateIPOName(stocks);
        var symbol = GenerateSymbol(name, stocks);
        var sectors = new[] { "Technology", "Healthcare", "Consumer Goods", "Energy", "Financials" };
        var sector = sectors[_rng.Next(sectors.Length)];
        var price = (decimal)(10 + _rng.NextDouble() * 90); // $10-$100
        var listingDate = GetNextTradingDay(gameTime, 5);

        PendingIPOs.Add(new PendingIPO
        {
            Name = name,
            Symbol = symbol,
            Sector = sector,
            ExpectedPrice = Math.Round(price, 2),
            ListingDate = listingDate,
        });

        NewsThisTick.Add($"{name} ({symbol}) files for IPO in {sector}, expected to list at ${price:F0}-${price * 1.2m:F0} on {listingDate:MMM dd}");
        _log.Info("IPO announced", new { name, symbol, sector, price, listingDate = listingDate.ToString("yyyy-MM-dd") });
    }

    private Stock CreateIPOStock(PendingIPO ipo)
    {
        // IPO day spike: +10-50% from expected price
        var spike = 1.0m + (decimal)(_rng.NextDouble() * 0.4 + 0.1);
        var openPrice = Math.Round(ipo.ExpectedPrice * spike, 2);

        var stock = new Stock(ipo.Symbol, ipo.Name, ipo.Sector)
        {
            CurrentPrice = openPrice,
            PreviousClose = ipo.ExpectedPrice,
            BidPrice = openPrice - 0.10m,
            AskPrice = openPrice + 0.10m,
            DayHigh = openPrice,
            DayLow = openPrice,
            FairValue = ipo.ExpectedPrice,
            BaseVolatility = 0.03m + (decimal)(_rng.NextDouble() * 0.02),
            LiquidityScore = 4 + _rng.Next(3),
            AverageVolume = 100_000 + _rng.Next(900_000),
            SharesOutstanding = 10_000_000 + _rng.Next(90_000_000),
            DividendYield = 0,
            IPODate = ipo.ListingDate,
            LockUpExpiry = ipo.ListingDate.AddDays(180), // 180-day insider lock-up
        };
        stock.Traits.Add("IPO Fresh");

        return stock;
    }

    private string GenerateIPOName(List<Stock> stocks)
    {
        var p1 = IPONames[0][_rng.Next(IPONames[0].Length)];
        var p2 = IPONames[1][_rng.Next(IPONames[1].Length)];
        var p3 = IPONames[2][_rng.Next(IPONames[2].Length)];
        return $"{p1}{p2} {p3}";
    }

    private string GenerateSymbol(string name, List<Stock> stocks)
    {
        var existing = new HashSet<string>(stocks.Select(s => s.Symbol));
        var consonants = new string(name.Where(c => char.IsLetter(c) && !"aeiouAEIOU ".Contains(c)).Take(4).ToArray()).ToUpper();
        if (consonants.Length < 3) consonants += "X";
        var sym = consonants[..Math.Min(4, consonants.Length)];
        while (existing.Contains(sym))
            sym = sym[..3] + _rng.Next(10).ToString();
        return sym;
    }

    private static DateTime GetNextTradingDay(DateTime from, int days)
    {
        var date = from.Date;
        int counted = 0;
        while (counted < days)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                counted++;
        }
        return date;
    }
}

public class PendingIPO
{
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Sector { get; set; } = "";
    public decimal ExpectedPrice { get; set; }
    public DateTime ListingDate { get; set; }
    public bool Listed { get; set; }
}

public class PendingDelisting
{
    public string Symbol { get; set; } = "";
    public DateTime DelistDate { get; set; }
    public bool Executed { get; set; }
}
