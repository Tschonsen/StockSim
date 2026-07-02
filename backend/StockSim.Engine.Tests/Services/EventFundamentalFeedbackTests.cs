using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 1: events leave a lasting mark on company fundamentals (not just price),
/// and a stock's growth trajectory feeds back into its daily drift so price moves
/// don't fully revert.
/// </summary>
public class EventFundamentalFeedbackTests : IDisposable
{
    private readonly EventEngine _engine;
    private readonly DateTime _time = new(2027, 1, 5, 10, 0, 0);

    public EventFundamentalFeedbackTests()
    {
        GameEvent.ResetIdCounter();
        _engine = new EventEngine(seed: 42);
    }

    public void Dispose() => GameEvent.ResetIdCounter();

    // ---- 1a: events -> fundamentals ----

    [Fact]
    public void MajorPositiveEarnings_RaisesNetIncome()
    {
        var stock = CreateStock("AAPL", "Apple", "Technology");
        var before = stock.NetIncome;
        Fire(stock, priceEffect: 0.08f, EventSeverity.Major, "earnings", "beat");
        Assert.True(stock.NetIncome > before, $"NetIncome {stock.NetIncome} should exceed {before}");
    }

    [Fact]
    public void MajorNegativeEarnings_LowersNetIncome()
    {
        var stock = CreateStock("AAPL", "Apple", "Technology");
        var before = stock.NetIncome;
        Fire(stock, priceEffect: -0.08f, EventSeverity.Major, "earnings", "miss");
        Assert.True(stock.NetIncome < before, $"NetIncome {stock.NetIncome} should be below {before}");
    }

    [Fact]
    public void EqualMagnitudeEvents_NegativeImpactsFundamentalsLessThanPositive()
    {
        // Negative events are already more violent in price terms (template design); Point 1 should
        // not over-amplify that into fundamentals, so a -X% event cuts NetIncome less than a +X% event lifts it.
        var beat = CreateStock("AAA", "Acme", "Technology");
        var miss = CreateStock("BBB", "Beta", "Technology");
        Fire(beat, 0.08f, EventSeverity.Major, "earnings", "beat");
        Fire(miss, -0.08f, EventSeverity.Major, "earnings", "miss");

        var gain = beat.NetIncome - 100m;
        var loss = 100m - miss.NetIncome;
        Assert.True(gain > loss, $"gain {gain} should exceed loss {loss}");
    }

    [Fact]
    public void EarningsBeat_RaisesRevenueGrowth_MissLowersIt()
    {
        var beat = CreateStock("AAA", "Acme", "Technology");
        var miss = CreateStock("BBB", "Beta", "Technology");
        var baseline = beat.RevenueGrowth;
        Fire(beat, 0.08f, EventSeverity.Major, "earnings", "beat");
        Fire(miss, -0.08f, EventSeverity.Major, "earnings", "miss");
        Assert.True(beat.RevenueGrowth > baseline, "beat should lift trajectory");
        Assert.True(miss.RevenueGrowth < baseline, "miss should cut trajectory");
    }

    [Fact]
    public void ProductBreakthrough_RaisesRevenue()
    {
        var stock = CreateStock("AAPL", "Apple", "Technology");
        var before = stock.Revenue;
        Fire(stock, 0.06f, EventSeverity.Major, "product", "breakthrough");
        Assert.True(stock.Revenue > before, $"Revenue {stock.Revenue} should exceed {before}");
    }

    [Fact]
    public void MajorFraud_DowngradesCreditRating()
    {
        var stock = CreateStock("AAPL", "Apple", "Technology");
        stock.Personality!.CreditRating = "AA";
        Fire(stock, -0.10f, EventSeverity.Major, "fraud", "scandal");
        Assert.NotEqual("AA", stock.Personality.CreditRating);
    }

    [Fact]
    public void MinorEvent_LeavesFundamentalsUnchanged()
    {
        var stock = CreateStock("AAPL", "Apple", "Technology");
        var ni = stock.NetIncome;
        var rev = stock.Revenue;
        var growth = stock.RevenueGrowth;
        Fire(stock, 0.08f, EventSeverity.Minor, "earnings", "beat");
        Assert.Equal(ni, stock.NetIncome);
        Assert.Equal(rev, stock.Revenue);
        Assert.Equal(growth, stock.RevenueGrowth);
    }

    [Fact]
    public void FundamentalImpact_AppliedOncePerEvent_NotEveryTick()
    {
        var stock = CreateStock("AAPL", "Apple", "Technology");
        var stocks = new[] { stock };
        _engine.InjectEvent(MakeEvent(stock, 0.08f, EventSeverity.Major, "earnings", "beat"));

        _engine.Tick(stocks, _time, isMarketOpen: true);
        var afterFirstTick = stock.NetIncome;

        _engine.Tick(stocks, _time.AddMinutes(1), isMarketOpen: true);
        Assert.Equal(afterFirstTick, stock.NetIncome);
    }

    // ---- 1b: growth trajectory -> daily drift (pure) ----

    [Fact]
    public void PositiveGrowth_BiasesDriftUp_NegativeDown()
    {
        Assert.True(FundamentalDynamics.GrowthTrajectory(0.10m, 0m).DriftBias > 0);
        Assert.True(FundamentalDynamics.GrowthTrajectory(-0.10m, 0m).DriftBias < 0);
    }

    [Fact]
    public void Growth_MeanRevertsTowardBaseline_ButNotInstantly()
    {
        var (_, newGrowth) = FundamentalDynamics.GrowthTrajectory(0.10m, 0m);
        Assert.True(newGrowth < 0.10m, "growth should decay toward baseline");
        Assert.True(newGrowth > 0m, "growth should not collapse to baseline in one step");
    }

    // ---- helpers ----

    private void Fire(Stock stock, float priceEffect, EventSeverity severity, params string[] tags)
    {
        _engine.InjectEvent(MakeEvent(stock, priceEffect, severity, tags));
        _engine.Tick(new[] { stock }, _time, isMarketOpen: true);
    }

    private static GameEvent MakeEvent(Stock stock, float priceEffect, EventSeverity severity, params string[] tags) => new()
    {
        Type = EventType.Company,
        Severity = severity,
        PriceEffect = priceEffect,
        Sentiment = priceEffect > 0 ? 0.7f : -0.7f,
        Headline = "test event",
        AffectedSymbols = { stock.Symbol },
        AffectedSectors = { stock.Sector },
        Tags = tags.ToList(),
        DurationMinutes = 30,
        RemainingMinutes = 30,
    };

    private static Stock CreateStock(string symbol, string name, string sector) => new(symbol, name, sector)
    {
        CurrentPrice = 150m,
        PreviousClose = 150m,
        BidPrice = 149.90m,
        AskPrice = 150.10m,
        DayHigh = 150m,
        DayLow = 150m,
        FairValue = 150m,
        AverageVolume = 1_000_000,
        SharesOutstanding = 100_000_000,
        Revenue = 1000m,
        NetIncome = 100m,
        RevenueGrowth = 0.05m,
        DebtToEquity = 1.0m,
        Personality = new CompanyPersonality
        {
            CEOName = "Test CEO",
            CEOArchetype = "Steady Hand",
            CreditRating = "A",
        },
    };
}
