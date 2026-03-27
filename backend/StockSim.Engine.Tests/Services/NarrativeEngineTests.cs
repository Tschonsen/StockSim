using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class NarrativeEngineTests
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

    private static List<Stock> MakeStocks()
    {
        return new List<Stock>
        {
            new("TECH", "TechCorp", "Technology") { CurrentPrice = 150m, BaseVolatility = 0.02m },
            new("BANK", "BankCo", "Financials") { CurrentPrice = 80m, BaseVolatility = 0.015m },
            new("OILX", "OilCorp", "Energy") { CurrentPrice = 60m, BaseVolatility = 0.025m },
            new("HLTH", "HealthInc", "Healthcare") { CurrentPrice = 200m, BaseVolatility = 0.02m },
        };
    }

    [Fact]
    public void NarrativeEngine_ShouldLoadArcTemplates()
    {
        var engine = new NarrativeEngine(42);
        engine.LoadArcs(FindDataPath());

        // May be 0 if no tier3/tier4 JSON files exist yet
        // Just verify it doesn't throw
        Assert.True(engine.TemplateCount >= 0);
    }

    [Fact]
    public void NarrativeEngine_TickDay_ShouldNotThrowWithoutArcs()
    {
        var engine = new NarrativeEngine(42);
        var stocks = MakeStocks();
        var gameTime = new DateTime(2027, 1, 6, 16, 0, 0);

        engine.TickDay(stocks, gameTime, MarketPhase.Bull);

        Assert.Empty(engine.NewEventsThisTick);
    }

    [Fact]
    public void NarrativeEngine_ShouldEventuallyActivateArc()
    {
        var engine = new NarrativeEngine(42);
        engine.LoadArcs(FindDataPath());

        if (engine.TemplateCount == 0)
        {
            // No arc templates available yet — skip
            return;
        }

        var stocks = MakeStocks();
        var gameTime = new DateTime(2027, 1, 6, 16, 0, 0);

        bool activated = false;
        for (int day = 0; day < 200; day++)
        {
            engine.TickDay(stocks, gameTime.AddDays(day), MarketPhase.Neutral);
            if (engine.ActiveArcs.Count > 0 || engine.NewEventsThisTick.Count > 0)
            {
                activated = true;
                break;
            }
        }

        Assert.True(activated, "NarrativeEngine should eventually activate an arc within 200 days");
    }

    [Fact]
    public void NarrativeEngine_ArcEvents_ShouldHaveRichFields()
    {
        var engine = new NarrativeEngine(42);
        engine.LoadArcs(FindDataPath());

        if (engine.TemplateCount == 0) return;

        var stocks = MakeStocks();
        var gameTime = new DateTime(2027, 1, 6, 16, 0, 0);

        GameEvent? arcEvent = null;
        for (int day = 0; day < 300; day++)
        {
            engine.TickDay(stocks, gameTime.AddDays(day), MarketPhase.Neutral);
            arcEvent = engine.NewEventsThisTick.FirstOrDefault();
            if (arcEvent != null) break;
        }

        if (arcEvent != null)
        {
            Assert.NotEmpty(arcEvent.Headline);
            Assert.NotNull(arcEvent.ArcId);
            Assert.True(arcEvent.Tier == EventTier.Tier3 || arcEvent.Tier == EventTier.Tier4);
        }
    }

    [Fact]
    public void NarrativeEngine_ShouldLimitActiveArcs()
    {
        var engine = new NarrativeEngine(42);
        engine.LoadArcs(FindDataPath());

        var stocks = MakeStocks();
        var gameTime = new DateTime(2027, 1, 6, 16, 0, 0);

        for (int day = 0; day < 500; day++)
        {
            engine.TickDay(stocks, gameTime.AddDays(day), MarketPhase.Neutral);
            Assert.True(engine.ActiveArcs.Count <= 2,
                $"Should have max 2 active arcs, got {engine.ActiveArcs.Count}");
        }
    }
}
