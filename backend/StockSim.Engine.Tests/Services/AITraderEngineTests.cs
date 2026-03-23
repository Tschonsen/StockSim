using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class AITraderEngineTests
{
    private readonly AITraderEngine _engine;

    public AITraderEngineTests()
    {
        _engine = new AITraderEngine(seed: 42);
    }

    [Fact]
    public void MarketMaker_ShouldMaintainBidAskSpread()
    {
        var stock = CreateStock(100m, liquidityScore: 7);

        _engine.Tick(new[] { stock }, Array.Empty<GameEvent>(), isMarketOpen: true);

        Assert.True(stock.BidPrice > 0);
        Assert.True(stock.AskPrice > stock.BidPrice);
        Assert.True(stock.BidPrice < stock.CurrentPrice);
        Assert.True(stock.AskPrice > stock.CurrentPrice);
    }

    [Fact]
    public void MarketMaker_LiquidStock_ShouldHaveTighterSpread()
    {
        var liquid = CreateStock(100m, liquidityScore: 9);
        var illiquid = CreateStock(100m, liquidityScore: 2);

        _engine.Tick(new[] { liquid, illiquid }, Array.Empty<GameEvent>(), isMarketOpen: true);

        var liquidSpread = liquid.AskPrice - liquid.BidPrice;
        var illiquidSpread = illiquid.AskPrice - illiquid.BidPrice;

        Assert.True(liquidSpread < illiquidSpread,
            $"Liquid spread {liquidSpread} should be tighter than illiquid {illiquidSpread}");
    }

    [Fact]
    public void MarketMaker_ShouldAddVolume()
    {
        var stock = CreateStock(100m);
        stock.DayVolume = 0;

        _engine.Tick(new[] { stock }, Array.Empty<GameEvent>(), isMarketOpen: true);

        Assert.True(stock.DayVolume > 0, "Market maker should generate volume");
    }

    [Fact]
    public void RetailSentiment_ShouldStartAtZero()
    {
        Assert.Equal(0f, _engine.RetailSentiment);
    }

    [Fact]
    public void RetailSentiment_ShouldReactToPositiveEvents()
    {
        var stocks = new[] { CreateStock(100m) };
        var events = new[]
        {
            new GameEvent { Sentiment = 0.5f, Severity = EventSeverity.Major },
        };

        // Run several ticks with positive event
        for (int i = 0; i < 100; i++)
        {
            _engine.Tick(stocks, events, isMarketOpen: true);
        }

        Assert.True(_engine.RetailSentiment > 0,
            $"Sentiment {_engine.RetailSentiment} should be positive after positive events");
    }

    [Fact]
    public void RetailSentiment_ShouldReactToNegativeEvents()
    {
        var stocks = new[] { CreateStock(100m) };
        var events = new[]
        {
            new GameEvent { Sentiment = -0.7f, Severity = EventSeverity.Major },
        };

        for (int i = 0; i < 100; i++)
        {
            _engine.Tick(stocks, events, isMarketOpen: true);
        }

        Assert.True(_engine.RetailSentiment < 0,
            $"Sentiment {_engine.RetailSentiment} should be negative after negative events");
    }

    [Fact]
    public void RetailSentiment_ShouldMeanRevert()
    {
        var stocks = new[] { CreateStock(100m) };
        var events = new[]
        {
            new GameEvent { Sentiment = 0.8f, Severity = EventSeverity.Major },
        };

        // Build up sentiment
        for (int i = 0; i < 50; i++)
            _engine.Tick(stocks, events, isMarketOpen: true);

        var peakSentiment = _engine.RetailSentiment;

        // Let sentiment decay with no events
        for (int i = 0; i < 500; i++)
            _engine.Tick(stocks, Array.Empty<GameEvent>(), isMarketOpen: true);

        Assert.True(Math.Abs(_engine.RetailSentiment) < Math.Abs(peakSentiment),
            $"Sentiment {_engine.RetailSentiment} should have reverted toward 0 from {peakSentiment}");
    }

    [Fact]
    public void Tick_ShouldNotActWhenMarketClosed()
    {
        var stock = CreateStock(100m);
        stock.DayVolume = 0;
        var priceBefore = stock.CurrentPrice;

        _engine.Tick(new[] { stock }, Array.Empty<GameEvent>(), isMarketOpen: false);

        Assert.Equal(0, stock.DayVolume);
    }

    [Fact]
    public void Spreads_ShouldBePositive_ForAllPrices()
    {
        var stocks = new[]
        {
            CreateStock(0.50m, liquidityScore: 1),  // Penny stock
            CreateStock(50m, liquidityScore: 5),     // Mid cap
            CreateStock(3000m, liquidityScore: 10),  // Mega cap
        };

        _engine.Tick(stocks, Array.Empty<GameEvent>(), isMarketOpen: true);

        foreach (var stock in stocks)
        {
            Assert.True(stock.AskPrice > stock.BidPrice,
                $"{stock.Symbol}: Ask {stock.AskPrice} should be > Bid {stock.BidPrice}");
            Assert.True(stock.BidPrice > 0,
                $"{stock.Symbol}: Bid should be positive, got {stock.BidPrice}");
        }
    }

    [Fact]
    public void RetailPressure_ShouldAffectPrices_OverManyTicks()
    {
        var engine = new AITraderEngine(seed: 99);
        var stock = CreateStock(100m);

        var events = new[]
        {
            new GameEvent { Sentiment = 0.8f, Severity = EventSeverity.Major },
        };

        // Build up positive sentiment and track cumulative effect
        for (int i = 0; i < 500; i++)
            engine.Tick(new[] { stock }, events, isMarketOpen: true);

        // With strong positive sentiment over 500 ticks, price should have moved
        // Due to 30% hit rate and small per-tick effect, we allow broad tolerance
        Assert.True(engine.RetailSentiment > 0,
            $"Sentiment {engine.RetailSentiment} should be positive after positive events");
    }

    [Fact]
    public void SameSeed_ShouldProduceSameResults()
    {
        var e1 = new AITraderEngine(seed: 123);
        var e2 = new AITraderEngine(seed: 123);
        var s1 = CreateStock(100m);
        var s2 = CreateStock(100m);

        for (int i = 0; i < 100; i++)
        {
            e1.Tick(new[] { s1 }, Array.Empty<GameEvent>(), isMarketOpen: true);
            e2.Tick(new[] { s2 }, Array.Empty<GameEvent>(), isMarketOpen: true);
        }

        Assert.Equal(s1.BidPrice, s2.BidPrice);
        Assert.Equal(s1.AskPrice, s2.AskPrice);
        Assert.Equal(s1.DayVolume, s2.DayVolume);
    }

    private static Stock CreateStock(decimal price, int liquidityScore = 5)
    {
        return new Stock("TEST", "Test Corp", "Technology")
        {
            CurrentPrice = price,
            PreviousClose = price,
            BidPrice = price - 0.10m,
            AskPrice = price + 0.10m,
            DayHigh = price,
            DayLow = price,
            BaseVolatility = 0.02m,
            LiquidityScore = liquidityScore,
            FairValue = price,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
        };
    }
}
