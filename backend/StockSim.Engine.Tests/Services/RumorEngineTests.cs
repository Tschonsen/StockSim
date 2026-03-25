using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class RumorEngineTests
{
    private readonly DateTime _baseTime = new(2027, 1, 4, 16, 0, 0); // Market close, Day 3

    private static List<Stock> MakeStocks(int count = 10)
    {
        var stocks = new List<Stock>();
        for (var i = 0; i < count; i++)
        {
            stocks.Add(new Stock($"TST{i}", $"Test Corp {i}", "Technology")
            {
                CurrentPrice = 50m + i * 10,
                PreviousClose = 50m + i * 10,
                AverageVolume = 100_000,
            });
        }
        return stocks;
    }

    // ========================
    // Basic Generation
    // ========================

    [Fact]
    public void NewEngine_ShouldHaveNoRumors()
    {
        var engine = new RumorEngine(42);
        Assert.Empty(engine.ActiveRumors);
        Assert.Empty(engine.RumorHistory);
        Assert.Empty(engine.NewRumorsThisTick);
    }

    [Fact]
    public void TickDay_ShouldNotGenerateRumorBeforeInterval()
    {
        var engine = new RumorEngine(42);
        var stocks = MakeStocks();

        // Tick only a few days — should NOT generate a rumor (interval is 20-40 days)
        for (var day = 0; day < 15; day++)
        {
            engine.TickDay(stocks, _baseTime.AddDays(day));
        }

        Assert.Empty(engine.RumorHistory);
    }

    [Fact]
    public void TickDay_ShouldGenerateRumorAfterInterval()
    {
        var engine = new RumorEngine(42);
        var stocks = MakeStocks();

        // Tick enough days to guarantee at least one rumor (max interval = 40 days)
        for (var day = 0; day < 50; day++)
        {
            engine.TickDay(stocks, _baseTime.AddDays(day));
        }

        Assert.NotEmpty(engine.RumorHistory);
    }

    [Fact]
    public void GeneratedRumor_ShouldHaveValidFields()
    {
        var engine = new RumorEngine(42);
        var stocks = MakeStocks();

        // Force rumor by setting interval to 0
        engine.NextRumorInDays = 0;
        engine.TickDay(stocks, _baseTime);

        Assert.Single(engine.NewRumorsThisTick);
        var rumor = engine.NewRumorsThisTick[0];

        Assert.False(string.IsNullOrEmpty(rumor.Symbol));
        Assert.False(string.IsNullOrEmpty(rumor.Headline));
        Assert.False(string.IsNullOrEmpty(rumor.CompanyName));
        Assert.Contains("💬", rumor.Headline);
        Assert.Contains("Market Rumor", rumor.Headline);
        Assert.Equal(_baseTime, rumor.CreatedAt);
        Assert.True(rumor.EventExpectedAt > rumor.CreatedAt);
        Assert.True((rumor.EventExpectedAt - rumor.CreatedAt).TotalDays >= 1);
        Assert.True((rumor.EventExpectedAt - rumor.CreatedAt).TotalDays <= 5);
        Assert.False(rumor.EventFired);
    }

    // ========================
    // True/False Ratio
    // ========================

    [Fact]
    public void RumorTruthRatio_ShouldBeApproximately80Percent()
    {
        // Generate many rumors with different seeds and check the ratio
        var trueCount = 0;
        var totalCount = 0;

        for (var seed = 0; seed < 200; seed++)
        {
            var engine = new RumorEngine(seed);
            var stocks = MakeStocks();
            engine.NextRumorInDays = 0;
            engine.TickDay(stocks, _baseTime);

            foreach (var r in engine.NewRumorsThisTick)
            {
                totalCount++;
                if (r.IsTrue) trueCount++;
            }
        }

        Assert.True(totalCount > 100, $"Should have generated enough rumors, got {totalCount}");
        var trueRatio = (double)trueCount / totalCount;
        Assert.InRange(trueRatio, 0.65, 0.95); // Allow some variance from 80%
    }

    // ========================
    // Event Firing
    // ========================

    [Fact]
    public void TrueRumor_ShouldFireEventOnExpectedDate()
    {
        // Find a seed that produces a true rumor
        RumorEngine? engine = null;
        Rumor? trueRumor = null;
        var stocks = MakeStocks();

        for (var seed = 0; seed < 100; seed++)
        {
            engine = new RumorEngine(seed);
            engine.NextRumorInDays = 0;
            engine.TickDay(stocks, _baseTime);

            trueRumor = engine.NewRumorsThisTick.FirstOrDefault(r => r.IsTrue);
            if (trueRumor != null) break;
        }

        Assert.NotNull(engine);
        Assert.NotNull(trueRumor);

        // Advance to the expected event date
        engine.TickDay(stocks, trueRumor.EventExpectedAt);

        Assert.NotEmpty(engine.RumorEventsThisTick);
        var evt = engine.RumorEventsThisTick[0];
        Assert.Equal(EventType.Company, evt.Type);
        Assert.Contains(trueRumor.Symbol, evt.AffectedSymbols);
        Assert.NotEmpty(evt.Headline);
        Assert.NotEqual(0f, evt.PriceEffect);
        Assert.True(trueRumor.EventFired);
    }

    [Fact]
    public void FalseRumor_ShouldNotFireEvent()
    {
        RumorEngine? engine = null;
        Rumor? falseRumor = null;
        var stocks = MakeStocks();

        for (var seed = 0; seed < 200; seed++)
        {
            engine = new RumorEngine(seed);
            engine.NextRumorInDays = 0;
            engine.TickDay(stocks, _baseTime);

            falseRumor = engine.NewRumorsThisTick.FirstOrDefault(r => !r.IsTrue);
            if (falseRumor != null) break;
        }

        Assert.NotNull(engine);
        Assert.NotNull(falseRumor);

        // Advance to the expected event date
        engine.TickDay(stocks, falseRumor.EventExpectedAt);

        // Should have no events (only true rumors fire events)
        Assert.Empty(engine.RumorEventsThisTick);
        Assert.True(falseRumor.EventFired); // Marked as resolved
    }

    // ========================
    // Frequency Multiplier
    // ========================

    [Fact]
    public void FrequencyMultiplier_ShouldAffectInterval()
    {
        var engine = new RumorEngine(42);
        var stocks = MakeStocks();
        engine.FrequencyMultiplier = 3.0; // "The Insider" scenario

        var rumorCount = 0;
        for (var day = 0; day < 100; day++)
        {
            engine.TickDay(stocks, _baseTime.AddDays(day));
            rumorCount += engine.NewRumorsThisTick.Count;
        }

        // With 3x multiplier, should generate significantly more rumors
        Assert.True(rumorCount >= 5, $"Expected >=5 rumors with 3x multiplier over 100 days, got {rumorCount}");
    }

    // ========================
    // Active Rumor Lookup (for SMA)
    // ========================

    [Fact]
    public void GetActiveRumorForSymbol_ShouldReturnActiveRumor()
    {
        var engine = new RumorEngine(42);
        var stocks = MakeStocks();
        engine.NextRumorInDays = 0;
        engine.TickDay(stocks, _baseTime);

        Assert.NotEmpty(engine.NewRumorsThisTick);
        var rumor = engine.NewRumorsThisTick[0];

        var found = engine.GetActiveRumorForSymbol(rumor.Symbol);
        Assert.NotNull(found);
        Assert.Equal(rumor.Id, found.Id);
    }

    [Fact]
    public void GetActiveRumorForSymbol_ShouldReturnNullForUnknownSymbol()
    {
        var engine = new RumorEngine(42);
        Assert.Null(engine.GetActiveRumorForSymbol("NONEXISTENT"));
    }

    // ========================
    // Save/Load Round-Trip
    // ========================

    [Fact]
    public void LoadState_ShouldRestoreRumorsCorrectly()
    {
        var engine = new RumorEngine(42);
        var stocks = MakeStocks();
        engine.NextRumorInDays = 0;
        engine.TickDay(stocks, _baseTime);

        var rumors = engine.RumorHistory.ToList();
        var lastDay = engine.LastRumorDay;
        var nextIn = engine.NextRumorInDays;

        // Create a new engine and load state
        var engine2 = new RumorEngine(99);
        engine2.LoadState(rumors, lastDay, nextIn);

        Assert.Equal(rumors.Count, engine2.RumorHistory.Count);
        Assert.Equal(lastDay, engine2.LastRumorDay);
        Assert.Equal(nextIn, engine2.NextRumorInDays);

        // Active rumors should be restored
        var activeCount = rumors.Count(r => !r.EventFired);
        Assert.Equal(activeCount, engine2.ActiveRumors.Count);
    }

    // ========================
    // ETF Exclusion
    // ========================

    [Fact]
    public void Rumor_ShouldNotTargetETFs()
    {
        var stocks = new List<Stock>
        {
            new("ETF_MKT", "Market ETF", "ETF") { CurrentPrice = 100m },
            new("ETF_TEC", "Tech ETF", "ETF") { CurrentPrice = 50m },
            new("REAL", "Real Corp", "Technology") { CurrentPrice = 75m },
        };

        // Generate many rumors
        for (var seed = 0; seed < 50; seed++)
        {
            var engine = new RumorEngine(seed);
            engine.NextRumorInDays = 0;
            engine.TickDay(stocks, _baseTime);

            foreach (var r in engine.NewRumorsThisTick)
            {
                Assert.DoesNotContain("ETF_", r.Symbol);
            }
        }
    }

    // ========================
    // Template Coverage
    // ========================

    [Fact]
    public void AllTemplates_ShouldProduceBothPositiveAndNegativeHints()
    {
        var stocks = MakeStocks();
        var seenPositive = false;
        var seenNegative = false;

        for (var seed = 0; seed < 200; seed++)
        {
            var engine = new RumorEngine(seed);
            engine.NextRumorInDays = 0;
            engine.TickDay(stocks, _baseTime);

            foreach (var r in engine.NewRumorsThisTick)
            {
                if (r.IsPositive) seenPositive = true;
                else seenNegative = true;
            }

            if (seenPositive && seenNegative) break;
        }

        Assert.True(seenPositive, "Should generate at least one positive rumor");
        Assert.True(seenNegative, "Should generate at least one negative rumor");
    }

    [Fact]
    public void NewRumorsThisTick_ShouldClearEachTick()
    {
        var engine = new RumorEngine(42);
        var stocks = MakeStocks();
        engine.NextRumorInDays = 0;

        engine.TickDay(stocks, _baseTime);
        Assert.NotEmpty(engine.NewRumorsThisTick);

        // Next tick without generating a new rumor
        engine.TickDay(stocks, _baseTime.AddDays(1));
        Assert.Empty(engine.NewRumorsThisTick);
    }
}
