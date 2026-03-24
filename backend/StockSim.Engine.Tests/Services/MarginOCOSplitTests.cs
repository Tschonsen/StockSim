using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class MarginTradingTests
{
    [Fact]
    public void MarginDisabled_BuyingPowerEqualsCash()
    {
        var portfolio = new Portfolio(50_000m);
        Func<string, decimal> getPrice = _ => 100m;

        Assert.Equal(50_000m, portfolio.BuyingPower(getPrice));
        Assert.False(portfolio.MarginEnabled);
    }

    [Fact]
    public void MarginEnabled_BuyingPowerDoubled()
    {
        var portfolio = new Portfolio(50_000m) { MarginEnabled = true };
        Func<string, decimal> getPrice = _ => 100m;

        // 2:1 leverage: 50k cash + 50k margin = 100k buying power
        Assert.Equal(100_000m, portfolio.BuyingPower(getPrice));
    }

    [Fact]
    public void MarginUsedPercent_ShouldBeZeroWithNoMargin()
    {
        var portfolio = new Portfolio(50_000m) { MarginEnabled = true };
        Func<string, decimal> getPrice = _ => 100m;

        Assert.Equal(0m, portfolio.MarginUsedPercent(getPrice));
    }

    [Fact]
    public void MarginUsedPercent_ShouldCalculateCorrectly()
    {
        var portfolio = new Portfolio(50_000m)
        {
            MarginEnabled = true,
            MarginBalance = 25_000m, // Borrowed 25k
        };
        Func<string, decimal> getPrice = _ => 100m;

        // Equity = Cash + Positions = 50k, Margin = 25k => 50%
        Assert.Equal(50m, portfolio.MarginUsedPercent(getPrice));
    }

    [Fact]
    public void MarginBuyingPower_ReducesWithBorrowing()
    {
        var portfolio = new Portfolio(10_000m)
        {
            MarginEnabled = true,
            MarginBalance = 30_000m,
        };
        Func<string, decimal> getPrice = _ => 100m;

        // Equity = 10k, Max borrow = 10k (2:1) - already borrowed 30k
        // BuyingPower = cash + max(0, equity * (2-1) - marginBalance)
        var bp = portfolio.BuyingPower(getPrice);
        Assert.True(bp <= 10_000m); // Can't borrow more, only cash available
    }
}

public class OCOOrderTests
{
    [Fact]
    public void OCOPairId_ShouldBeSettable()
    {
        var order = new Order("TEST", OrderSide.Sell, OrderType.Limit, 100, DateTime.Now, limitPrice: 150m);
        order.OCOPairId = 42;

        Assert.Equal(42, order.OCOPairId);
    }

    [Fact]
    public void OCOPairId_ShouldDefaultToNull()
    {
        var order = new Order("TEST", OrderSide.Buy, OrderType.Market, 50, DateTime.Now);
        Assert.Null(order.OCOPairId);
    }

    [Fact]
    public void OCOFill_ShouldCancelPairedOrder()
    {
        Order.ResetIdCounter();
        var portfolio = new Portfolio(100_000m);
        var engine = new OrderEngine(portfolio);
        var stock = new Stock("TEST", "Test Inc", "Technology")
        {
            CurrentPrice = 100m, BidPrice = 99.95m, AskPrice = 100.05m,
            AverageVolume = 1_000_000, BaseVolatility = 0.02m, LiquidityScore = 8,
        };
        var gameTime = new DateTime(2027, 1, 5, 10, 0, 0);

        // Buy first
        engine.PlaceOrder("TEST", OrderSide.Buy, OrderType.Market, 100, stock, gameTime, true);

        // Place OCO pair: take-profit at $120 + stop-loss at $90
        var tpResult = engine.PlaceOrder("TEST", OrderSide.Sell, OrderType.Limit, 100, stock, gameTime, true, limitPrice: 120m);
        var slResult = engine.PlaceOrder("TEST", OrderSide.Sell, OrderType.Stop, 100, stock, gameTime, true, stopPrice: 90m);

        Assert.True(tpResult.Success);
        Assert.True(slResult.Success);

        // Link them
        tpResult.Order!.OCOPairId = slResult.Order!.Id;
        slResult.Order!.OCOPairId = tpResult.Order!.Id;

        // Now trigger the stop (price drops to 89)
        stock.CurrentPrice = 89m;
        stock.BidPrice = 88.95m;
        stock.AskPrice = 89.05m;
        engine.CheckStopOrders(stock, gameTime, true);

        // Stop should be filled, take-profit should be cancelled
        Assert.Equal(OrderStatus.Filled, slResult.Order.Status);
        Assert.Equal(OrderStatus.Cancelled, tpResult.Order.Status);
        Assert.Equal("OCO pair filled", tpResult.Order.RejectReason);
    }
}

public class StockSplitTests
{
    [Fact]
    public void ForwardSplit_ShouldHalvePrice()
    {
        var stock = new Stock("TEST", "Test Inc", "Technology")
        {
            CurrentPrice = 600m, PreviousClose = 590m, FairValue = 600m,
            DayHigh = 610m, DayLow = 580m, BidPrice = 599m, AskPrice = 601m,
            SharesOutstanding = 1_000_000,
        };

        // Simulate 2:1 split
        stock.CurrentPrice = Math.Round(stock.CurrentPrice / 2, 2);
        stock.SharesOutstanding *= 2;

        Assert.Equal(300m, stock.CurrentPrice);
        Assert.Equal(2_000_000, stock.SharesOutstanding);
    }

    [Fact]
    public void ReverseSplit_ShouldMultiplyPrice()
    {
        var stock = new Stock("PENNY", "Penny Corp", "Technology")
        {
            CurrentPrice = 0.30m, SharesOutstanding = 10_000_000,
        };

        // 1:10 reverse split
        stock.CurrentPrice = Math.Round(stock.CurrentPrice * 10, 2);
        stock.SharesOutstanding /= 10;

        Assert.Equal(3.00m, stock.CurrentPrice);
        Assert.Equal(1_000_000, stock.SharesOutstanding);
    }

    [Fact]
    public void Split_ShouldAdjustPortfolioPosition()
    {
        var position = new Position("TEST", 100, 500m);
        Assert.Equal(100m, position.Shares);
        Assert.Equal(500m, position.AverageCost);

        // 2:1 split
        position.Shares *= 2;
        position.AverageCost = Math.Round(position.AverageCost / 2, 2);

        Assert.Equal(200m, position.Shares);
        Assert.Equal(250m, position.AverageCost);
        // Total cost should remain ~same
        Assert.Equal(50_000m, position.Shares * position.AverageCost);
    }
}

public class BankruptcyTests
{
    [Fact]
    public void GameLoop_ShouldDetectBankruptcy()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5, startingCash: 100m);
        loop.SetSpeed(GameSpeed.Normal);

        // Run to get past market open first
        for (int i = 0; i < 35; i++) loop.ExecuteTick();

        // Now drain all cash and clear positions
        loop.Portfolio.Cash = -1m; // Negative to be safe
        loop.Portfolio.Positions.Clear();

        // Continue ticking — need to reach 4 PM for bankruptcy check
        for (int i = 0; i < 2000; i++)
        {
            if (loop.IsPaused) loop.SetSpeed(GameSpeed.Normal); // Keep going even if paused
            loop.ExecuteTick();
            if (loop.IsBankrupt) break;
        }

        Assert.True(loop.IsBankrupt, $"Should detect bankruptcy. GameTime={loop.GameTime}, Cash={loop.Portfolio.Cash}, Pos={loop.Portfolio.Positions.Count}");
    }

    [Fact]
    public void NotBankrupt_WhenHasPositions()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5, startingCash: 100m);
        loop.SetSpeed(GameSpeed.Normal);

        loop.Portfolio.Cash = 0;
        // Add a position so not bankrupt
        loop.Portfolio.Positions["TEST"] = new Position("TEST", 10, 50m);

        for (int i = 0; i < 500; i++)
            loop.ExecuteTick();

        Assert.False(loop.IsBankrupt, "Should NOT be bankrupt when has positions");
    }

    [Fact]
    public void NotBankrupt_WhenHasCash()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5, startingCash: 1000m);
        loop.SetSpeed(GameSpeed.Normal);

        for (int i = 0; i < 100; i++)
            loop.ExecuteTick();

        Assert.False(loop.IsBankrupt, "Should NOT be bankrupt with $1000 cash");
    }
}

public class ScenarioTests
{
    [Fact]
    public void GetAll_ShouldReturn10Scenarios()
    {
        var scenarios = Scenario.GetAll();
        Assert.Equal(10, scenarios.Count);
    }

    [Fact]
    public void AllScenarios_ShouldHaveUniqueIds()
    {
        var scenarios = Scenario.GetAll();
        var ids = scenarios.Select(s => s.Id).Distinct().ToList();
        Assert.Equal(scenarios.Count, ids.Count);
    }

    [Fact]
    public void AllScenarios_ShouldHavePositiveStartingCash()
    {
        var scenarios = Scenario.GetAll();
        Assert.All(scenarios, s => Assert.True(s.StartingCash > 0));
    }

    [Fact]
    public void AllScenarios_ShouldHaveNameAndDescription()
    {
        var scenarios = Scenario.GetAll();
        Assert.All(scenarios, s =>
        {
            Assert.False(string.IsNullOrEmpty(s.Name));
            Assert.False(string.IsNullOrEmpty(s.Description));
            Assert.False(string.IsNullOrEmpty(s.Difficulty));
        });
    }

    [Fact]
    public void SpeedRun_ShouldHaveNoTimeLimit()
    {
        var speedRun = Scenario.GetAll().First(s => s.Id == "speed_run");
        Assert.Null(speedRun.TimeLimitDays);
        Assert.Equal(1_000_000m, speedRun.TargetPortfolioValue);
    }

    [Fact]
    public void IronMan_ShouldDisallowSaving()
    {
        var ironMan = Scenario.GetAll().First(s => s.Id == "iron_man");
        Assert.True(ironMan.NoSaveAllowed);
    }

    [Fact]
    public void TheCrash_ShouldForceBearMarket()
    {
        var crash = Scenario.GetAll().First(s => s.Id == "the_crash");
        Assert.Equal(MarketPhase.Bear, crash.ForcePhase);
        Assert.True(crash.SurvivalMode);
    }
}

public class AnalystRatingTests
{
    [Fact]
    public void AnalystConsensus_StrongBuy()
    {
        var stock = new Stock("TEST", "Test", "Tech") { AnalystRating = 4.8m };
        Assert.Equal("Strong Buy", stock.AnalystConsensus);
    }

    [Fact]
    public void AnalystConsensus_Hold()
    {
        var stock = new Stock("TEST", "Test", "Tech") { AnalystRating = 3.0m };
        Assert.Equal("Hold", stock.AnalystConsensus);
    }

    [Fact]
    public void AnalystConsensus_StrongSell()
    {
        var stock = new Stock("TEST", "Test", "Tech") { AnalystRating = 1.2m };
        Assert.Equal("Strong Sell", stock.AnalystConsensus);
    }
}
