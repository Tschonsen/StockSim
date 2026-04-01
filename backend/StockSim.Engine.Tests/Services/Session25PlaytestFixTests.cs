using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Tests for Session 25 playtest findings fixes:
/// 1. Sector drift clamp (±8%)
/// 2. Weekly price clamp (±15%)
/// 3. Event frequency increase
/// 4. Initial dividend scheduling
/// </summary>
public class Session25PlaytestFixTests
{
    // === FIX 1: Sector Multiplier Clamp ===

    [Fact]
    public void SectorMultipliers_ShouldBeClampedTo8Percent()
    {
        var engine = new EconomicEngine(42);
        // Push oil to extreme value to force Energy multiplier to extremes
        engine.Data.OilPrice = 20m; // Very low oil → Energy should drop
        engine.Data.InterestRate = 12m; // Very high rates → Tech should drop

        var multipliers = engine.GetSectorMultipliers();

        foreach (var (sector, mult) in multipliers)
        {
            Assert.InRange(mult, 0.97m, 1.03m);
        }
    }

    [Fact]
    public void SectorMultipliers_NeutralConditions_ShouldBeNearOne()
    {
        var engine = new EconomicEngine(42);
        engine.Data.OilPrice = 75m;
        engine.Data.InterestRate = 3m;
        engine.Data.ConsumerConfidence = 90m;
        engine.Data.ManufacturingPMI = 50m;
        engine.Data.InflationRate = 2m;
        engine.Data.UnemploymentRate = 4m;

        var multipliers = engine.GetSectorMultipliers();

        foreach (var (sector, mult) in multipliers)
        {
            Assert.InRange(mult, 0.95m, 1.05m);
        }
    }

    // === FIX 2+5: Weekly Price Clamp ===

    [Fact]
    public void DailyClamp_ShouldPreventExtremeDailyMoves()
    {
        var engine = new PriceEngine(42);
        var stock = CreateTestStock("TEST", 100m);
        stock.PreviousClose = 100m; // Day open reference

        // Simulate full trading day with extreme volatility
        stock.BaseVolatility = 0.10m; // Extremely high vol
        for (int i = 0; i < 390; i++)
        {
            engine.GenerateSectorShocks(new[] { stock.Sector });
            engine.Tick(stock, TimeSpan.FromMinutes(1));
        }

        // Price should be within ±3% of previous close ($100)
        Assert.InRange(stock.CurrentPrice, 97m, 103m);
    }

    [Fact]
    public void DailyClamp_MultiDay_ShouldLimitCumulativeDrift()
    {
        var engine = new PriceEngine(42);
        var stock = CreateTestStock("DRIFT", 100m);
        stock.PreviousClose = 100m;
        stock.BaseVolatility = 0.05m;

        // Simulate 10 trading days (3900 ticks)
        for (int day = 0; day < 10; day++)
        {
            engine.GenerateSectorShocks(new[] { stock.Sector });
            for (int tick = 0; tick < 390; tick++)
            {
                engine.Tick(stock, TimeSpan.FromMinutes(1));
            }
            // Reset for next day (like GameLoop does at market open)
            stock.PreviousClose = stock.CurrentPrice;
        }

        // With ±5%/day clamp over 10 days, max theoretical drift is ~63% (compound)
        // but mean reversion + random walk should keep it much closer
        // Ensure no extreme moves (>40% from start)
        var totalChange = Math.Abs((double)(stock.CurrentPrice - 100m) / 100);
        Assert.True(totalChange < 0.40, $"10-day drift {totalChange:P1} exceeds 40%");
    }

    // === FIX 3: Event Frequency ===

    [Fact]
    public void EventFrequency_ShouldGenerate3to12EventsPerDay()
    {
        var engine = new EventEngine(42);
        var stocks = Enumerable.Range(0, 30).Select(i => CreateTestStock($"S{i:D3}", 50m + i * 5)).ToList();
        var gameTime = new DateTime(2027, 1, 6, 9, 30, 0); // Monday

        int totalEvents = 0;
        // Simulate one trading day (390 ticks)
        for (int tick = 0; tick < 390; tick++)
        {
            gameTime = gameTime.AddMinutes(1);
            engine.Tick(stocks, gameTime, isMarketOpen: true);
            totalEvents += engine.NewEventsThisTick.Count;
        }

        // Should generate many events per day (target: 30-50, player filters noise from signal)
        Assert.True(totalEvents >= 10, $"Expected >=10 events/day, got {totalEvents}");
        Assert.True(totalEvents <= 60, $"Expected <=60 events/day, got {totalEvents}");
    }

    [Fact]
    public void EventFrequency_Over5Days_ShouldAverageAtLeast3PerDay()
    {
        var engine = new EventEngine(123);
        var stocks = Enumerable.Range(0, 30).Select(i => CreateTestStock($"T{i:D3}", 50m + i * 5)).ToList();
        var gameTime = new DateTime(2027, 1, 6, 9, 30, 0);

        int totalEvents = 0;
        int tradingDays = 0;
        // Simulate 5 trading days
        for (int day = 0; day < 7; day++) // 7 calendar days ≈ 5 trading days
        {
            if (gameTime.DayOfWeek == DayOfWeek.Saturday || gameTime.DayOfWeek == DayOfWeek.Sunday)
            {
                gameTime = gameTime.AddDays(1);
                continue;
            }
            tradingDays++;
            for (int tick = 0; tick < 390; tick++)
            {
                gameTime = gameTime.AddMinutes(1);
                engine.Tick(stocks, gameTime, isMarketOpen: true);
                totalEvents += engine.NewEventsThisTick.Count;
            }
            // Jump to next day
            gameTime = gameTime.Date.AddDays(1).AddHours(9).AddMinutes(30);
        }

        var avgPerDay = (double)totalEvents / tradingDays;
        Assert.True(avgPerDay >= 3.0, $"Expected avg >=3 events/day, got {avgPerDay:F1} ({totalEvents} total in {tradingDays} days)");
    }

    // === FIX 4: Initial Dividends ===

    [Fact]
    public void ScheduleInitialDividends_ShouldCreatePendingPayments()
    {
        var engine = new DividendEngine();
        var stocks = new List<Stock>();
        for (int i = 0; i < 20; i++)
        {
            var stock = CreateTestStock($"DIV{i}", 100m);
            stock.DividendYield = i < 12 ? 0.03m : 0m; // 12 out of 20 pay dividends
            stocks.Add(stock);
        }

        engine.ScheduleInitialDividends(stocks, new DateTime(2027, 3, 15, 9, 30, 0));

        // Should schedule at least 3 dividends (30% of 12 eligible = ~4)
        Assert.True(engine.PendingPayments.Count >= 3,
            $"Expected >=3 initial dividends, got {engine.PendingPayments.Count}");
    }

    [Fact]
    public void ScheduleInitialDividends_ShouldWorkInNonQuarterMonth()
    {
        var engine = new DividendEngine();
        var stocks = Enumerable.Range(0, 10).Select(i =>
        {
            var s = CreateTestStock($"Q{i}", 80m);
            s.DividendYield = 0.025m;
            return s;
        }).ToList();

        // February is NOT a quarter-start month, but initial dividends should still schedule
        engine.ScheduleInitialDividends(stocks, new DateTime(2027, 2, 10, 9, 30, 0));

        Assert.True(engine.PendingPayments.Count >= 3,
            $"Expected dividends even in February, got {engine.PendingPayments.Count}");
    }

    // === HELPER ===

    private static Stock CreateTestStock(string symbol, decimal price) => new(symbol, $"{symbol} Inc.", "Technology")
    {
        CurrentPrice = price,
        FairValue = price,
        PreviousClose = price,
        DayHigh = price,
        DayLow = price,
        BidPrice = price - 0.05m,
        AskPrice = price + 0.05m,
        BaseVolatility = 0.015m,
        AverageVolume = 500_000,
        LiquidityScore = 7,
        SpreadMultiplier = 1.0m,
        SharesOutstanding = 1_000_000,
    };
}
