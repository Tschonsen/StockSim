using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class CompanyPersonalityTests
{
    private static readonly string[] AllSectors =
    {
        "Technology", "Energy", "Financials", "Healthcare",
        "Consumer Goods", "Industrials", "Materials", "Real Estate",
        "Telecommunications", "Utilities", "Luxury Goods", "Transportation"
    };

    private List<Stock> CreateStocks(int perSector = 10)
    {
        var stocks = new List<Stock>();
        foreach (var sector in AllSectors)
        {
            for (int i = 0; i < perSector; i++)
            {
                var s = new Stock($"{sector[..2].ToUpper()}{i}", $"{sector} Co {i}", sector);
                s.Subsector = sector switch
                {
                    "Technology" => "Software",
                    "Energy" => "Oil & Gas",
                    "Healthcare" => "Biotechnology",
                    _ => ""
                };
                stocks.Add(s);
            }
        }
        return stocks;
    }

    [Fact]
    public void GenerateAll_AssignsPersonalityToEveryStock()
    {
        var stocks = CreateStocks();
        CompanyPersonalityGenerator.GenerateAll(stocks, seed: 42);

        foreach (var stock in stocks)
        {
            Assert.NotNull(stock.Personality);
            Assert.False(string.IsNullOrEmpty(stock.Personality!.CEOName), $"{stock.Symbol} missing CEO");
            Assert.False(string.IsNullOrEmpty(stock.Personality.CEOArchetype), $"{stock.Symbol} missing archetype");
            Assert.False(string.IsNullOrEmpty(stock.Personality.Headquarters), $"{stock.Symbol} missing HQ");
            Assert.False(string.IsNullOrEmpty(stock.Personality.Description), $"{stock.Symbol} missing description");
            Assert.False(string.IsNullOrEmpty(stock.Personality.FlagshipProduct), $"{stock.Symbol} missing flagship");
            Assert.False(string.IsNullOrEmpty(stock.Personality.FoundingStory), $"{stock.Symbol} missing story");
            Assert.InRange(stock.Personality.FoundedYear, 1920, 2025);
        }
    }

    [Fact]
    public void GenerateAll_IsDeterministic_SameSeedSameResults()
    {
        var stocks1 = CreateStocks(5);
        var stocks2 = CreateStocks(5);

        CompanyPersonalityGenerator.GenerateAll(stocks1, seed: 123);
        CompanyPersonalityGenerator.GenerateAll(stocks2, seed: 123);

        for (int i = 0; i < stocks1.Count; i++)
        {
            Assert.Equal(stocks1[i].Personality!.CEOName, stocks2[i].Personality!.CEOName);
            Assert.Equal(stocks1[i].Personality!.FlagshipProduct, stocks2[i].Personality!.FlagshipProduct);
            Assert.Equal(stocks1[i].Personality!.FoundedYear, stocks2[i].Personality!.FoundedYear);
            Assert.Equal(stocks1[i].Personality!.RivalSymbol, stocks2[i].Personality!.RivalSymbol);
        }
    }

    [Fact]
    public void GenerateAll_DifferentSeedsDifferentResults()
    {
        var stocks1 = CreateStocks(5);
        var stocks2 = CreateStocks(5);

        CompanyPersonalityGenerator.GenerateAll(stocks1, seed: 100);
        CompanyPersonalityGenerator.GenerateAll(stocks2, seed: 200);

        // At least some should differ
        var differences = stocks1.Zip(stocks2)
            .Count(pair => pair.First.Personality!.CEOName != pair.Second.Personality!.CEOName);
        Assert.True(differences > 0, "Different seeds should produce different personalities");
    }

    [Fact]
    public void GenerateAll_AssignsRivalries()
    {
        var stocks = CreateStocks(20); // More stocks = more rivalry pairs
        CompanyPersonalityGenerator.GenerateAll(stocks, seed: 42);

        var withRivals = stocks.Count(s => !string.IsNullOrEmpty(s.Personality?.RivalSymbol));
        Assert.True(withRivals > 0, "Should assign at least some rivalries");

        // Verify rival symbols actually exist
        foreach (var stock in stocks.Where(s => !string.IsNullOrEmpty(s.Personality?.RivalSymbol)))
        {
            var rivalExists = stocks.Any(s => s.Symbol == stock.Personality!.RivalSymbol);
            Assert.True(rivalExists, $"{stock.Symbol}'s rival {stock.Personality!.RivalSymbol} doesn't exist");
        }
    }

    [Fact]
    public void GenerateAll_AllSectorsGetProducts()
    {
        var stocks = CreateStocks(3);
        CompanyPersonalityGenerator.GenerateAll(stocks, seed: 42);

        foreach (var sector in AllSectors)
        {
            var sectorStocks = stocks.Where(s => s.Sector == sector).ToList();
            Assert.All(sectorStocks, s =>
                Assert.False(string.IsNullOrEmpty(s.Personality?.FlagshipProduct),
                    $"Sector {sector} stock {s.Symbol} missing flagship product"));
        }
    }

    [Fact]
    public void GenerateAll_CEONameHasTwoParts()
    {
        var stocks = CreateStocks(5);
        CompanyPersonalityGenerator.GenerateAll(stocks, seed: 42);

        foreach (var stock in stocks)
        {
            var parts = stock.Personality!.CEOName.Split(' ');
            Assert.True(parts.Length >= 2, $"CEO name '{stock.Personality.CEOName}' should have first and last name");
        }
    }
}

public class SectorEventTemplateTests : IDisposable
{
    private readonly EventEngine _engine;
    private readonly List<Stock> _stocks;
    private readonly DateTime _marketTime = new(2027, 1, 5, 10, 0, 0);

    private static readonly string[] AllSectors =
    {
        "Technology", "Energy", "Financials", "Healthcare",
        "Consumer Goods", "Industrials", "Materials", "Real Estate",
        "Telecommunications", "Utilities", "Luxury Goods", "Transportation"
    };

    public SectorEventTemplateTests()
    {
        GameEvent.ResetIdCounter();
        _engine = new EventEngine(seed: 42);

        // Create stocks for every sector
        _stocks = new List<Stock>();
        foreach (var sector in AllSectors)
        {
            var s = new Stock($"{sector[..2].ToUpper()}1", $"{sector} Corp", sector);
            s.CurrentPrice = 100m;
            s.PreviousClose = 100m;
            s.BidPrice = 99.95m;
            s.AskPrice = 100.05m;
            s.BaseVolatility = 0.02m;
            _stocks.Add(s);
        }
    }

    public void Dispose() => GameEvent.ResetIdCounter();

    [Fact]
    public void SectorEvents_ShouldEventuallyFire_ForAllSectors()
    {
        // Run many ticks to collect sector events
        var hitSectors = new HashSet<string>();

        for (int i = 0; i < 10000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), true);

            foreach (var evt in _engine.NewEventsThisTick)
            {
                if (evt.Type == EventType.Sector)
                {
                    foreach (var s in evt.AffectedSectors)
                        hitSectors.Add(s);
                }
            }
        }

        // We should hit most sectors in 10k ticks
        Assert.True(hitSectors.Count >= 6, $"Expected at least 6 sectors hit, got {hitSectors.Count}: {string.Join(", ", hitSectors)}");
    }

    [Fact]
    public void SectorEvents_HaveSectorSpecificHeadlines()
    {
        // Run ticks and verify that events have sector-specific content (not generic)
        var sectorHeadlines = new Dictionary<string, List<string>>();

        for (int i = 0; i < 5000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), true);

            foreach (var evt in _engine.NewEventsThisTick)
            {
                if (evt.Type == EventType.Sector && evt.AffectedSectors.Count > 0)
                {
                    var sector = evt.AffectedSectors[0];
                    if (!sectorHeadlines.ContainsKey(sector))
                        sectorHeadlines[sector] = new List<string>();
                    sectorHeadlines[sector].Add(evt.Headline);
                }
            }
        }

        // Check that some headlines contain sector-specific keywords
        if (sectorHeadlines.ContainsKey("Technology"))
            Assert.Contains(sectorHeadlines["Technology"], h => h.Contains("tech", StringComparison.OrdinalIgnoreCase) || h.Contains("chip", StringComparison.OrdinalIgnoreCase) || h.Contains("AI", StringComparison.OrdinalIgnoreCase) || h.Contains("privacy", StringComparison.OrdinalIgnoreCase) || h.Contains("Technology"));

        if (sectorHeadlines.ContainsKey("Energy"))
            Assert.Contains(sectorHeadlines["Energy"], h => h.Contains("oil", StringComparison.OrdinalIgnoreCase) || h.Contains("OPEC", StringComparison.OrdinalIgnoreCase) || h.Contains("renewable", StringComparison.OrdinalIgnoreCase) || h.Contains("Energy"));
    }

    [Fact]
    public void SectorEvents_PriceEffectsInReasonableRange()
    {
        for (int i = 0; i < 5000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), true);

            foreach (var evt in _engine.NewEventsThisTick)
            {
                if (evt.Type == EventType.Sector)
                {
                    // Spec says sector events should be roughly ±1-10%
                    Assert.InRange(Math.Abs(evt.PriceEffect), 0.005f, 0.10f);
                }
            }
        }
    }
}
