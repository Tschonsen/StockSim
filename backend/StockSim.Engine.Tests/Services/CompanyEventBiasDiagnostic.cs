using StockSim.Engine.Models;
using StockSim.Engine.Services;
using Xunit.Abstractions;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Diagnostic (not a hard gate): measure whether company-event generation skews bearish across
/// seeds — the suspected doom-loop from Point 2 (distressed companies over-selected) + Point 1
/// (their fundamentals get permanently cut). EventEngine.Tick already runs that loop.
/// </summary>
public class CompanyEventBiasDiagnostic
{
    private readonly ITestOutputHelper _out;
    public CompanyEventBiasDiagnostic(ITestOutputHelper output) => _out = output;

    private static string FindDataPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var c = Path.Combine(dir.FullName, "StockSim.Engine", "data");
            if (Directory.Exists(c)) return c;
            dir = dir.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory, "data");
    }

    [Fact]
    public void MeasureCompanyEventSentimentAcrossSeeds()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();
        var start = new DateTime(2027, 1, 5, 10, 0, 0);
        int totalPos = 0, totalNeg = 0;
        double totalEffect = 0;

        foreach (var seed in new[] { 1, 7, 42, 123, 999, 2027 })
        {
            GameEvent.ResetIdCounter();
            var engine = new EventEngine(seed, loader);
            var stocks = MakeMixedMarket();
            for (int i = 0; i < 6000; i++)
                engine.Tick(stocks, start.AddMinutes(i), isMarketOpen: true);

            var company = engine.EventHistory.Where(e => e.Type == EventType.Company).ToList();
            int pos = company.Count(e => e.PriceEffect > 0);
            int neg = company.Count(e => e.PriceEffect < 0);
            double sum = company.Sum(e => (double)e.PriceEffect);
            totalPos += pos; totalNeg += neg; totalEffect += sum;
            _out.WriteLine($"seed {seed,4}: company events={company.Count,4} pos={pos,4} neg={neg,4} " +
                           $"net%={sum * 100:F1} avg%={(company.Count > 0 ? sum / company.Count * 100 : 0):F3}");
        }

        var totEvents = totalPos + totalNeg;
        _out.WriteLine($"\nTOTAL: events={totEvents} pos={totalPos} neg={totalNeg} " +
                       $"pos/neg ratio={(double)totalPos / Math.Max(1, totalNeg):F2} " +
                       $"netEffect%={totalEffect * 100:F1}");

        Assert.True(totEvents > 0);
    }

    private static List<Stock> MakeMixedMarket()
    {
        var list = new List<Stock>();
        // 3 healthy, 3 distressed, 4 neutral — exercises health-weighted selection (Point 2).
        void Add(string sym, string sector, decimal rev, decimal ni, decimal de, string rating, decimal growth)
        {
            list.Add(new Stock(sym, sym + " Corp", sector)
            {
                CurrentPrice = 100m, PreviousClose = 100m, BidPrice = 99.9m, AskPrice = 100.1m,
                DayHigh = 101m, DayLow = 99m, FairValue = 100m, AverageVolume = 1_000_000,
                SharesOutstanding = 50_000_000, Revenue = rev, NetIncome = ni, DebtToEquity = de,
                RevenueGrowth = growth, TargetPrice = 110m,
                Personality = new CompanyPersonality
                {
                    CEOName = "Test CEO", CEOArchetype = "Steady Hand", CreditRating = rating,
                    FlagshipProduct = "product", Headquarters = "Boston", FoundedYear = 2000,
                },
            });
        }
        Add("HLTA", "Technology", 5_000_000_000m, 900_000_000m, 0.3m, "AA", 0.18m);
        Add("HLTB", "Healthcare", 4_000_000_000m, 700_000_000m, 0.4m, "AA", 0.15m);
        Add("HLTC", "Financials", 6_000_000_000m, 800_000_000m, 0.5m, "A", 0.12m);
        Add("DSTA", "Energy", 2_000_000_000m, -150_000_000m, 4.0m, "B", -0.18m);
        Add("DSTB", "Materials", 1_500_000_000m, -100_000_000m, 3.5m, "B", -0.15m);
        Add("DSTC", "Industrials", 1_800_000_000m, -80_000_000m, 3.2m, "BB", -0.10m);
        Add("NEUA", "Consumer Goods", 3_000_000_000m, 200_000_000m, 1.2m, "BBB", 0.03m);
        Add("NEUB", "Utilities", 3_200_000_000m, 250_000_000m, 1.0m, "BBB", 0.02m);
        Add("NEUC", "Telecommunications", 2_800_000_000m, 180_000_000m, 1.4m, "BBB", 0.01m);
        Add("NEUD", "Real Estate", 2_500_000_000m, 150_000_000m, 1.5m, "BBB", 0.00m);
        return list;
    }
}
