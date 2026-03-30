using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class EconomicEngineExtendedTests
{
    [Fact]
    public void TickDay_ShouldKeepInterestRateInRange()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));

        for (int i = 0; i < 1000; i++)
            engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

        Assert.InRange(engine.Data.InterestRate, 0m, 20m);
    }

    [Fact]
    public void TickDay_ShouldKeepInflationInRange()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));

        for (int i = 0; i < 1000; i++)
            engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

        Assert.InRange(engine.Data.InflationRate, -1m, 15m);
    }

    [Fact]
    public void TickDay_ShouldKeepGDPGrowthInRange()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));

        for (int i = 0; i < 1000; i++)
            engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

        Assert.InRange(engine.Data.GDPGrowth, -5m, 10m);
    }

    [Fact]
    public void TickDay_ShouldKeepUnemploymentInRange()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));

        for (int i = 0; i < 1000; i++)
            engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

        Assert.InRange(engine.Data.UnemploymentRate, 2m, 15m);
    }

    [Fact]
    public void GetSectorMultipliers_ShouldReturnAllSectors()
    {
        var engine = new EconomicEngine(42);
        var multipliers = engine.GetSectorMultipliers();

        var expectedSectors = new[]
        {
            "Technology", "Financials", "Real Estate", "Utilities", "Energy",
            "Healthcare", "Consumer Goods", "Industrials", "Materials",
            "Telecommunications", "Luxury Goods", "Transportation", "ETF"
        };

        foreach (var sector in expectedSectors)
        {
            Assert.True(multipliers.ContainsKey(sector), $"Missing sector: {sector}");
        }
    }

    [Fact]
    public void GetSectorMultipliers_ShouldBeClampedWithinThreePercent()
    {
        // Test with multiple seeds to cover different economic conditions
        for (int seed = 0; seed < 20; seed++)
        {
            var engine = new EconomicEngine(seed);
            // Tick many days to push indicators to extremes
            engine.ScheduleEvents(new DateTime(2027, 1, 5));
            for (int i = 0; i < 500; i++)
                engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

            var multipliers = engine.GetSectorMultipliers();
            foreach (var (sector, value) in multipliers)
            {
                Assert.True(value >= 0.97m, $"Seed {seed}, sector {sector}: multiplier {value} below 0.97");
                Assert.True(value <= 1.03m, $"Seed {seed}, sector {sector}: multiplier {value} above 1.03");
            }
        }
    }

    [Fact]
    public void UpdateVolatilityIndex_ShouldProduceValuesInRange()
    {
        var engine = new EconomicEngine(42);
        var stocks = Enumerable.Range(0, 20).Select(i =>
        {
            var s = new Stock($"S{i:D3}", $"Stock {i}", "Technology")
            {
                CurrentPrice = 50m + i,
                SharesOutstanding = 1_000_000,
                BaseVolatility = 0.02m,
            };
            return s;
        }).ToList();

        // Test with various event counts and stress levels
        foreach (var events in new[] { 0, 5, 15 })
        {
            foreach (var stress in new[] { 0.0, 0.5, 1.0 })
            {
                engine.UpdateVolatilityIndex(stocks, events, stress);
                Assert.InRange(engine.MarketVolatilityIndex, 0, 80);
            }
        }
    }

    [Fact]
    public void UpdateVolatilityIndex_ShouldIncreaseWithMoreEvents()
    {
        var engine = new EconomicEngine(42);
        var stocks = Enumerable.Range(0, 10).Select(i =>
            new Stock($"S{i}", $"Stock {i}", "Technology")
            {
                CurrentPrice = 100m,
                SharesOutstanding = 1_000_000,
                BaseVolatility = 0.02m,
            }).ToList();

        // Settle the VIX with no stress
        for (int i = 0; i < 20; i++)
            engine.UpdateVolatilityIndex(stocks, 0, 0.0);
        var lowVix = engine.MarketVolatilityIndex;

        // Now increase events and stress
        for (int i = 0; i < 20; i++)
            engine.UpdateVolatilityIndex(stocks, 10, 0.8);
        var highVix = engine.MarketVolatilityIndex;

        Assert.True(highVix > lowVix, $"VIX with stress ({highVix}) should exceed calm VIX ({lowVix})");
    }

    [Fact]
    public void GetMarketSentiment_ShouldReturnBetweenMinusOneAndOne()
    {
        // Test across many seeds to cover different initial conditions
        for (int seed = 0; seed < 30; seed++)
        {
            var engine = new EconomicEngine(seed);
            var sentiment = engine.GetMarketSentiment();
            Assert.InRange(sentiment, -1m, 1m);
        }
    }

    [Fact]
    public void GetMarketSentiment_ShouldRemainInRangeAfterExtremeDrift()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));

        // Push indicators to extremes over many ticks
        for (int i = 0; i < 1000; i++)
            engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

        var sentiment = engine.GetMarketSentiment();
        Assert.InRange(sentiment, -1m, 1m);
    }
}
