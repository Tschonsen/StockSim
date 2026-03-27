using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class PriceEngineTests
{
    private readonly PriceEngine _engine;

    public PriceEngineTests()
    {
        _engine = new PriceEngine(seed: 42);
    }

    [Fact]
    public void Tick_ShouldUpdateStockPrice()
    {
        var stock = CreateTestStock(100m);
        var oldPrice = stock.CurrentPrice;

        _engine.Tick(stock, TimeSpan.FromMinutes(1));

        Assert.NotEqual(oldPrice, stock.CurrentPrice);
    }

    [Fact]
    public void Tick_ShouldKeepPricePositive()
    {
        var stock = CreateTestStock(1.00m);

        for (int i = 0; i < 10000; i++)
        {
            _engine.Tick(stock, TimeSpan.FromMinutes(1));
        }

        Assert.True(stock.CurrentPrice > 0, $"Price went to {stock.CurrentPrice}");
    }

    [Fact]
    public void Tick_ShouldUpdateBidAsk()
    {
        var stock = CreateTestStock(100m);

        _engine.Tick(stock, TimeSpan.FromMinutes(1));

        Assert.True(stock.BidPrice > 0);
        Assert.True(stock.AskPrice > 0);
        Assert.True(stock.AskPrice > stock.BidPrice, "Ask should be greater than Bid");
    }

    [Fact]
    public void Tick_ShouldUpdateDayHighLow()
    {
        var stock = CreateTestStock(100m);
        stock.DayHigh = 100m;
        stock.DayLow = 100m;

        for (int i = 0; i < 100; i++)
        {
            _engine.Tick(stock, TimeSpan.FromMinutes(1));
        }

        Assert.True(stock.DayHigh >= stock.DayLow);
        Assert.True(stock.DayHigh > 0);
    }

    [Fact]
    public void Tick_ShouldIncrementVolume()
    {
        var stock = CreateTestStock(100m);
        stock.DayVolume = 0;

        _engine.Tick(stock, TimeSpan.FromMinutes(1));

        Assert.True(stock.DayVolume > 0);
    }

    [Fact]
    public void HighVolatilityStock_ShouldMoveMoreThanLowVolatility()
    {
        var engine = new PriceEngine(seed: 42);
        var highVol = CreateTestStock(100m, volatility: 0.04m);
        var lowVol = CreateTestStock(100m, volatility: 0.005m);

        decimal highVolTotalMove = 0;
        decimal lowVolTotalMove = 0;

        for (int i = 0; i < 1000; i++)
        {
            var oldHigh = highVol.CurrentPrice;
            var oldLow = lowVol.CurrentPrice;
            engine.Tick(highVol, TimeSpan.FromMinutes(1));
            engine.Tick(lowVol, TimeSpan.FromMinutes(1));
            highVolTotalMove += Math.Abs(highVol.CurrentPrice - oldHigh);
            lowVolTotalMove += Math.Abs(lowVol.CurrentPrice - oldLow);
        }

        Assert.True(highVolTotalMove > lowVolTotalMove,
            $"High vol moved {highVolTotalMove}, low vol moved {lowVolTotalMove}");
    }

    [Fact]
    public void SameSeed_ShouldProduceSameResults()
    {
        var engine1 = new PriceEngine(seed: 123);
        var engine2 = new PriceEngine(seed: 123);
        var stock1 = CreateTestStock(100m);
        var stock2 = CreateTestStock(100m);

        for (int i = 0; i < 100; i++)
        {
            engine1.Tick(stock1, TimeSpan.FromMinutes(1));
            engine2.Tick(stock2, TimeSpan.FromMinutes(1));
        }

        Assert.Equal(stock1.CurrentPrice, stock2.CurrentPrice);
    }

    [Fact]
    public void DifferentSeeds_ShouldProduceDifferentResults()
    {
        var engine1 = new PriceEngine(seed: 100);
        var engine2 = new PriceEngine(seed: 200);
        var stock1 = CreateTestStock(100m);
        var stock2 = CreateTestStock(100m);

        for (int i = 0; i < 100; i++)
        {
            engine1.Tick(stock1, TimeSpan.FromMinutes(1));
            engine2.Tick(stock2, TimeSpan.FromMinutes(1));
        }

        Assert.NotEqual(stock1.CurrentPrice, stock2.CurrentPrice);
    }

    [Fact]
    public void MeanReversion_ShouldPullPriceTowardFairValue()
    {
        var stock = CreateTestStock(200m); // Price far above fair value of 100
        stock.FairValue = 100m;

        decimal totalDrift = 0;
        for (int i = 0; i < 5000; i++)
        {
            var oldPrice = stock.CurrentPrice;
            _engine.Tick(stock, TimeSpan.FromMinutes(1));
            totalDrift += stock.CurrentPrice - oldPrice;
        }

        // Over many ticks, mean reversion should pull price down (negative total drift)
        Assert.True(totalDrift < 0,
            $"Expected negative drift toward fair value, got {totalDrift}");
    }

    [Fact]
    public void Spread_ShouldBeWiderForIlliquidStocks()
    {
        var liquid = CreateTestStock(100m, liquidityScore: 9);
        var illiquid = CreateTestStock(100m, liquidityScore: 2);

        _engine.Tick(liquid, TimeSpan.FromMinutes(1));
        _engine.Tick(illiquid, TimeSpan.FromMinutes(1));

        Assert.True(illiquid.SpreadPercent > liquid.SpreadPercent,
            $"Illiquid spread {illiquid.SpreadPercent}% should be > liquid {liquid.SpreadPercent}%");
    }

    [Fact]
    public void Tick_ShouldUpdateYearHighLow()
    {
        var stock = CreateTestStock(100m);
        stock.YearHigh = 100m;
        stock.YearLow = 100m;

        for (int i = 0; i < 500; i++)
        {
            _engine.Tick(stock, TimeSpan.FromMinutes(1));
        }

        // After many ticks, price should have moved, updating YearHigh or YearLow
        Assert.True(stock.YearHigh >= stock.YearLow);
        Assert.True(stock.YearHigh > 0);
        // At least one must have diverged from the initial 100
        Assert.True(stock.YearHigh > 100m || stock.YearLow < 100m,
            $"YearHigh={stock.YearHigh}, YearLow={stock.YearLow} — at least one should have changed");
    }

    [Fact]
    public void Tick_YearHighLow_ShouldInitializeFromZero()
    {
        var stock = CreateTestStock(50m);
        stock.YearHigh = 0;
        stock.YearLow = 0;

        _engine.Tick(stock, TimeSpan.FromMinutes(1));

        // When starting from 0, both should be set to current price
        Assert.True(stock.YearHigh > 0, "YearHigh should be initialized from zero");
        Assert.True(stock.YearLow > 0, "YearLow should be initialized from zero");
    }

    private static Stock CreateTestStock(
        decimal price,
        decimal volatility = 0.02m,
        int liquidityScore = 5)
    {
        return new Stock("TEST", "Test Corp", "Technology")
        {
            CurrentPrice = price,
            PreviousClose = price,
            BidPrice = price - 0.05m,
            AskPrice = price + 0.05m,
            DayHigh = price,
            DayLow = price,
            BaseVolatility = volatility,
            LiquidityScore = liquidityScore,
            FairValue = price,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
        };
    }
}
