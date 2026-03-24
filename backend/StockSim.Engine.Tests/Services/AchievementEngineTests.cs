using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class AchievementEngineTests
{
    private AchievementEngine CreateEngine()
    {
        var engine = new AchievementEngine();
        engine.SetSectorLookup(_ => "Technology");
        return engine;
    }

    [Fact]
    public void ShouldHave30Achievements()
    {
        var engine = CreateEngine();
        Assert.Equal(31, engine.Achievements.Count);
    }

    [Fact]
    public void ShouldStartWithNoUnlocks()
    {
        var engine = CreateEngine();
        Assert.All(engine.Achievements, a => Assert.False(a.Unlocked));
    }

    [Fact]
    public void ShouldUnlockFirstStepsAt60k()
    {
        var engine = CreateEngine();
        var portfolio = new Portfolio(50_000);
        Func<string, decimal> getPrice = _ => 100m;
        var time = new DateTime(2027, 1, 10);

        engine.CheckAchievements(59_000, portfolio, getPrice, time);
        Assert.Empty(engine.NewUnlocksThisTick);

        engine.CheckAchievements(60_000, portfolio, getPrice, time);
        Assert.Contains(engine.NewUnlocksThisTick, a => a.Id == "first_steps");
    }

    [Fact]
    public void ShouldUnlockMillionaire()
    {
        var engine = CreateEngine();
        var portfolio = new Portfolio(50_000);
        Func<string, decimal> getPrice = _ => 100m;
        var time = new DateTime(2027, 6, 1);

        engine.CheckAchievements(1_000_000, portfolio, getPrice, time);
        Assert.Contains(engine.NewUnlocksThisTick, a => a.Id == "millionaire");
    }

    [Fact]
    public void ShouldNotUnlockSameAchievementTwice()
    {
        var engine = CreateEngine();
        var portfolio = new Portfolio(50_000);
        Func<string, decimal> getPrice = _ => 100m;
        var time = new DateTime(2027, 1, 10);

        engine.CheckAchievements(60_000, portfolio, getPrice, time);
        Assert.Single(engine.NewUnlocksThisTick.Where(a => a.Id == "first_steps"));

        engine.CheckAchievements(60_000, portfolio, getPrice, time);
        Assert.DoesNotContain(engine.NewUnlocksThisTick, a => a.Id == "first_steps");
    }

    [Fact]
    public void RecordTrade_ShouldTrackWins()
    {
        var engine = CreateEngine();
        engine.RecordTrade(new TradeRecord { Symbol = "TEST", PnL = 500, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });
        engine.RecordTrade(new TradeRecord { Symbol = "TEST", PnL = 300, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });

        Assert.Equal(2, engine.Stats.WinningTradeCount);
        Assert.Equal(0, engine.Stats.LosingTradeCount);
        Assert.Equal(2, engine.Stats.ConsecutiveWins);
        Assert.Equal(500, engine.Stats.LargestSingleGain);
    }

    [Fact]
    public void RecordTrade_ShouldTrackLosses()
    {
        var engine = CreateEngine();
        engine.RecordTrade(new TradeRecord { Symbol = "TEST", PnL = -200, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });

        Assert.Equal(0, engine.Stats.WinningTradeCount);
        Assert.Equal(1, engine.Stats.LosingTradeCount);
        Assert.Equal(200, engine.Stats.LargestSingleLoss);
    }

    [Fact]
    public void RecordTrade_ShouldResetConsecutiveOnSwitch()
    {
        var engine = CreateEngine();
        engine.RecordTrade(new TradeRecord { Symbol = "A", PnL = 100, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });
        engine.RecordTrade(new TradeRecord { Symbol = "B", PnL = 200, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });
        engine.RecordTrade(new TradeRecord { Symbol = "C", PnL = -50, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });

        Assert.Equal(0, engine.Stats.ConsecutiveWins);
        Assert.Equal(1, engine.Stats.ConsecutiveLosses);
        Assert.Equal(2, engine.Stats.MaxConsecutiveWins);
    }

    [Fact]
    public void RecordTrade_ShouldTrackSectorPnL()
    {
        var engine = CreateEngine();
        engine.RecordTrade(new TradeRecord { Symbol = "A", PnL = 500, Sector = "Tech", Side = "Long", ExitTime = DateTime.Now });
        engine.RecordTrade(new TradeRecord { Symbol = "B", PnL = -200, Sector = "Tech", Side = "Long", ExitTime = DateTime.Now });
        engine.RecordTrade(new TradeRecord { Symbol = "C", PnL = 300, Sector = "Energy", Side = "Long", ExitTime = DateTime.Now });

        Assert.Equal(300, engine.Stats.SectorPnL["Tech"]);
        Assert.Equal(300, engine.Stats.SectorPnL["Energy"]);
    }

    [Fact]
    public void RecordEquitySnapshot_ShouldTrackDrawdown()
    {
        var engine = CreateEngine();
        var time = new DateTime(2027, 1, 5, 16, 0, 0);

        engine.RecordEquitySnapshot(50_000, 50_000, 0, time);
        engine.RecordEquitySnapshot(55_000, 45_000, 1.0m, time.AddDays(1));
        engine.RecordEquitySnapshot(44_000, 40_000, -2.0m, time.AddDays(2)); // 20% drawdown from peak

        Assert.Equal(55_000, engine.Stats.MaxPortfolioValue);
        Assert.True(engine.Stats.MaxDrawdownPercent > 19 && engine.Stats.MaxDrawdownPercent < 21);
        Assert.Equal(3, engine.Stats.EquityHistory.Count);
    }

    [Fact]
    public void ShouldUnlockFirstBloodOnFirstWin()
    {
        var engine = CreateEngine();
        var portfolio = new Portfolio(50_000);
        Func<string, decimal> getPrice = _ => 100m;

        engine.RecordTrade(new TradeRecord { Symbol = "A", PnL = 100, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });
        engine.CheckAchievements(50_000, portfolio, getPrice, DateTime.Now);

        Assert.Contains(engine.NewUnlocksThisTick, a => a.Id == "first_blood");
    }

    [Fact]
    public void ShouldUnlockSharpshooterAfter10ConsecutiveWins()
    {
        var engine = CreateEngine();
        var portfolio = new Portfolio(50_000);
        Func<string, decimal> getPrice = _ => 100m;

        for (int i = 0; i < 10; i++)
            engine.RecordTrade(new TradeRecord { Symbol = "A", PnL = 100, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });

        engine.CheckAchievements(50_000, portfolio, getPrice, DateTime.Now);
        Assert.Contains(engine.NewUnlocksThisTick, a => a.Id == "sharpshooter");
    }

    [Fact]
    public void ShouldUnlockHundredTradesAt100()
    {
        var engine = CreateEngine();
        var portfolio = new Portfolio(50_000);
        Func<string, decimal> getPrice = _ => 100m;

        for (int i = 0; i < 100; i++)
            engine.RecordTrade(new TradeRecord { Symbol = "A", PnL = i % 2 == 0 ? 50 : -30, Side = "Long", Sector = "Tech", ExitTime = DateTime.Now });

        engine.CheckAchievements(50_000, portfolio, getPrice, DateTime.Now);
        Assert.Contains(engine.NewUnlocksThisTick, a => a.Id == "hundred_trades");
    }

    [Fact]
    public void ShouldUnlockMarketVeteranAfter252Days()
    {
        var engine = CreateEngine();
        var portfolio = new Portfolio(50_000);
        Func<string, decimal> getPrice = _ => 100m;
        var time = new DateTime(2027, 1, 5, 16, 0, 0);

        for (int d = 0; d < 252; d++)
            engine.RecordEquitySnapshot(50_000, 50_000, 0, time.AddDays(d));

        engine.CheckAchievements(50_000, portfolio, getPrice, time.AddDays(252));
        Assert.Contains(engine.NewUnlocksThisTick, a => a.Id == "market_veteran");
    }

    [Fact]
    public void AchievementCategories_ShouldBeCovered()
    {
        var engine = CreateEngine();
        var categories = engine.Achievements.Select(a => a.Category).Distinct().ToList();

        Assert.Contains(AchievementCategory.Wealth, categories);
        Assert.Contains(AchievementCategory.Trading, categories);
        Assert.Contains(AchievementCategory.Market, categories);
        Assert.Contains(AchievementCategory.Risk, categories);
    }
}
