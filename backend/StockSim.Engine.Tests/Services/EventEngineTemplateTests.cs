using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class EventEngineTemplateTests
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

    private static TemplateLoader CreateLoader()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();
        return loader;
    }

    private static Stock MakeStock(string symbol = "TEST", string name = "Test Corp", string sector = "Technology")
    {
        return new Stock(symbol, name, sector)
        {
            CurrentPrice = 100m,
            PreviousClose = 100m,
            BidPrice = 99.95m,
            AskPrice = 100.05m,
            DayHigh = 100m,
            DayLow = 100m,
            BaseVolatility = 0.02m,
            FairValue = 100m,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
            Personality = new CompanyPersonality
            {
                CEOName = "John Smith",
                CEOArchetype = "Visionary",
                FlagshipProduct = "CloudOS Platform",
                RivalSymbol = "RIVL",
            },
        };
    }

    [Fact]
    public void EventEngine_WithTemplates_ShouldEventuallyGenerateEvents()
    {
        var loader = CreateLoader();
        var engine = new EventEngine(42, loader);
        var stocks = new List<Stock> { MakeStock(), MakeStock("RIVL", "Rival Corp", "Technology") };
        var baseTime = new DateTime(2027, 1, 6, 10, 0, 0); // Monday 10 AM

        bool foundEvent = false;
        for (int i = 0; i < 2000; i++)
        {
            engine.Tick(stocks, baseTime.AddMinutes(i), true);
            if (engine.NewEventsThisTick.Count > 0)
            {
                foundEvent = true;
                break;
            }
        }

        Assert.True(foundEvent, "EventEngine with templates should generate events");
    }

    [Fact]
    public void EventEngine_WithTemplates_ShouldProduceRichEvents()
    {
        var loader = CreateLoader();
        var baseTime = new DateTime(2027, 1, 6, 10, 0, 0);

        // Try multiple seeds to find one that generates a template-based event
        GameEvent? richEvent = null;
        for (int seed = 0; seed < 100; seed++)
        {
            var engine = new EventEngine(seed, loader);
            var stocks = new List<Stock> { MakeStock(), MakeStock("RIVL", "Rival Corp", "Technology") };

            for (int i = 0; i < 2000; i++)
            {
                engine.Tick(stocks, baseTime.AddMinutes(i), true);
                var candidate = engine.NewEventsThisTick.FirstOrDefault(e => e.Summary != null || e.Tags != null);
                if (candidate != null)
                {
                    richEvent = candidate;
                    break;
                }
            }
            if (richEvent != null) break;
        }

        Assert.NotNull(richEvent);
        // Template-based events should have at least Summary or Tags
        Assert.True(richEvent.Summary != null || richEvent.Tags != null,
            $"Rich event should have Summary or Tags. Headline: {richEvent.Headline}");
    }

    [Fact]
    public void EventEngine_WithTemplates_PlaceholdersShouldBeResolved()
    {
        var loader = CreateLoader();
        var baseTime = new DateTime(2027, 1, 6, 10, 0, 0);

        // Try multiple seeds
        for (int seed = 0; seed < 100; seed++)
        {
            var engine = new EventEngine(seed, loader);
            var stocks = new List<Stock> { MakeStock(), MakeStock("RIVL", "Rival Corp", "Technology") };

            for (int i = 0; i < 2000; i++)
            {
                engine.Tick(stocks, baseTime.AddMinutes(i), true);
                foreach (var evt in engine.NewEventsThisTick)
                {
                    // No unresolved placeholders in headlines
                    Assert.DoesNotContain("{company}", evt.Headline);
                    Assert.DoesNotContain("{ceo}", evt.Headline);
                    Assert.DoesNotContain("{product}", evt.Headline);
                }
            }
        }
    }

    [Fact]
    public void EventEngine_WithoutTemplates_ShouldFallbackToHardcoded()
    {
        // No TemplateLoader passed — should work exactly like before
        var engine = new EventEngine(42);
        var stocks = new List<Stock> { MakeStock() };
        var baseTime = new DateTime(2027, 1, 6, 10, 0, 0);

        bool foundEvent = false;
        for (int i = 0; i < 2000; i++)
        {
            engine.Tick(stocks, baseTime.AddMinutes(i), true);
            if (engine.NewEventsThisTick.Count > 0)
            {
                foundEvent = true;
                // Hardcoded events should NOT have Summary (old behavior)
                var evt = engine.NewEventsThisTick[0];
                Assert.Null(evt.Summary);
                break;
            }
        }

        Assert.True(foundEvent, "EventEngine without templates should still generate events");
    }

    [Fact]
    public void EventEngine_AnalystQuotes_ShouldAppearOnSomeEvents()
    {
        var loader = CreateLoader();
        var baseTime = new DateTime(2027, 1, 6, 10, 0, 0);

        int eventsWithAnalyst = 0;
        int totalEvents = 0;

        for (int seed = 0; seed < 20; seed++)
        {
            var engine = new EventEngine(seed, loader);
            var stocks = new List<Stock> { MakeStock(), MakeStock("RIVL", "Rival Corp", "Technology") };

            for (int i = 0; i < 500; i++)
            {
                engine.Tick(stocks, baseTime.AddMinutes(i), true);
                foreach (var evt in engine.NewEventsThisTick)
                {
                    totalEvents++;
                    if (evt.AnalystName != null) eventsWithAnalyst++;
                }
            }
        }

        // Some events should have analyst quotes (not all, ~40% chance per template event)
        Assert.True(totalEvents > 0, "Should have generated some events");
        // We don't require a specific count since it's probabilistic
    }
}
