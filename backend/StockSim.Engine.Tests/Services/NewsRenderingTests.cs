using System.Globalization;
using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 8: rendered news must be clean even under a comma-decimal locale — no "$0,15",
/// no "$the companyM", no "the the", no "$298MB" double-suffix.
/// </summary>
public class NewsRenderingTests
{
    private static string FindDataPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "StockSim.Engine", "data");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory, "data");
    }

    [Fact]
    public void RenderedNews_IsClean_EvenUnderGermanCulture()
    {
        var prev = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        GameEvent.ResetIdCounter();
        try
        {
            var loader = new TemplateLoader(FindDataPath());
            loader.LoadAll();
            var engine = new EventEngine(seed: 7, loader);
            var stocks = MakeStocks();
            var start = new DateTime(2027, 1, 5, 10, 0, 0);

            for (int i = 0; i < 12000; i++)
                engine.Tick(stocks, start.AddMinutes(i), isMarketOpen: true);

            var texts = engine.EventHistory
                .Select(e => string.Join(" ",
                    new[] { e.Headline, e.Summary, e.AnalystQuote }.Where(s => !string.IsNullOrEmpty(s))))
                .ToList();

            Assert.NotEmpty(texts);
            // Sanity: we actually rendered numeric/$ content (so the checks below aren't vacuous).
            Assert.Contains(texts, s => System.Text.RegularExpressions.Regex.IsMatch(s, @"\$\d"));

            foreach (var s in texts)
            {
                Assert.DoesNotMatch(@"\d,\d{1,2}(?!\d)", s);   // comma-decimal (e.g. "0,15")
                Assert.DoesNotContain("$the company", s);       // number slot got "the company"
                Assert.DoesNotContain("the the", s);            // double article
                Assert.DoesNotMatch(@"\dMB", s);                // "$298MB" double-suffix
                Assert.DoesNotMatch(@"\dBB", s);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = prev;
            GameEvent.ResetIdCounter();
        }
    }

    private static List<Stock> MakeStocks()
    {
        var defs = new[]
        {
            ("VRTX", "Vertex Systems", "Technology", "CloudSync Platform"),
            ("ASTM", "Astra Medical", "Healthcare", "gene therapy"),
            ("CRWB", "Crown Bank", "Financials", "digital banking app"),
            ("PETC", "PetroCorp", "Energy", "offshore wind project"),
            ("FRSH", "Fresh Provisions", "Consumer Goods", "flagship brand"),
            ("RVDY", "Rivet Dynamics", "Industrials", "automation system"),
        };
        var list = new List<Stock>();
        foreach (var (sym, name, sector, product) in defs)
        {
            list.Add(new Stock(sym, name, sector)
            {
                CurrentPrice = 120m, PreviousClose = 120m, BidPrice = 119.9m, AskPrice = 120.1m,
                DayHigh = 121m, DayLow = 119m, FairValue = 120m,
                AverageVolume = 1_000_000, SharesOutstanding = 80_000_000,
                Revenue = 4_200_000_000m, NetIncome = 520_000_000m, RevenueGrowth = 0.12m,
                DebtToEquity = 1.1m, TargetPrice = 135m,
                Personality = new CompanyPersonality
                {
                    CEOName = "Jordan Avery", CEOArchetype = "Visionary", CreditRating = "A",
                    FlagshipProduct = product, SecondaryProduct = "support services",
                    Headquarters = "Boston", FoundedYear = 1998, RivalSymbol = "RVDY",
                },
            });
        }
        return list;
    }
}
