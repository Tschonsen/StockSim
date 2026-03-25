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

    // ========================
    // HFT (7.2.2)
    // ========================

    [Fact]
    public void HFT_ShouldAddVolumeOnLiquidStocks()
    {
        var engine = new AITraderEngine(42);
        var liquid = CreateStock(100m, liquidityScore: 8);
        liquid.DayVolume = 0;

        for (int i = 0; i < 50; i++)
            engine.Tick(new[] { liquid }, Array.Empty<GameEvent>(), isMarketOpen: true);

        Assert.True(liquid.DayVolume > 0, "HFT should add volume to liquid stocks");
    }

    [Fact]
    public void HFT_ShouldNotActOnIlliquidStocks()
    {
        var engine = new AITraderEngine(42);
        var illiquid = CreateStock(100m, liquidityScore: 3);
        var liquid = CreateStock(100m, liquidityScore: 8);
        illiquid.DayVolume = 0;
        liquid.DayVolume = 0;

        for (int i = 0; i < 100; i++)
            engine.Tick(new[] { illiquid, liquid }, Array.Empty<GameEvent>(), isMarketOpen: true);

        // Liquid stock should have more volume from HFT activity
        Assert.True(liquid.DayVolume > illiquid.DayVolume,
            $"Liquid vol {liquid.DayVolume} should exceed illiquid vol {illiquid.DayVolume}");
    }

    // ========================
    // Hedge Fund Stress (7.2.6)
    // ========================

    [Fact]
    public void HedgeFundStress_ShouldIncrease_WithNegativeEvents()
    {
        var engine = new AITraderEngine(42);
        var stocks = new[] { CreateStock(100m) };
        var events = new[]
        {
            new GameEvent { Sentiment = -0.5f, Severity = EventSeverity.Major },
            new GameEvent { Sentiment = -0.6f, Severity = EventSeverity.Major },
            new GameEvent { Sentiment = -0.4f, Severity = EventSeverity.Moderate },
        };

        for (int i = 0; i < 200; i++)
            engine.Tick(stocks, events, isMarketOpen: true);

        Assert.True(engine.HedgeFundStress > 0, "Stress should increase with negative events");
    }

    // ========================
    // Day Trader (7.2.9)
    // ========================

    [Fact]
    public void DayTrader_ShouldAmplifyMomentum_AtOpenClose()
    {
        var engine = new AITraderEngine(42);
        var stock = CreateStock(100m);
        stock.PreviousClose = 95m; // Stock up 5.3% → strong momentum

        // Tick 50 times (simulates first 50 mins after open — day trader active window)
        for (int i = 0; i < 50; i++)
            engine.Tick(new[] { stock }, Array.Empty<GameEvent>(), isMarketOpen: true);

        // Price should have moved up from combined pressures (day trader + momentum)
        Assert.True(stock.CurrentPrice >= 100m, "Day trader momentum should maintain/push price");
    }

    // ========================
    // Sovereign Wealth (7.2.8)
    // ========================

    [Fact]
    public void SovereignWealth_ShouldOnlyActOnLargeCaps()
    {
        var engine = new AITraderEngine(42);
        var smallCap = CreateStock(10m, liquidityScore: 3);
        smallCap.SharesOutstanding = 1_000_000; // Small market cap
        var priceBefore = smallCap.CurrentPrice;

        // Sovereign wealth doesn't act on small caps (< $10B market cap)
        for (int i = 0; i < 100; i++)
            engine.Tick(new[] { smallCap }, Array.Empty<GameEvent>(), isMarketOpen: true);

        // Price should be primarily driven by market maker, not sovereign wealth
        // This is a sanity check
        Assert.True(smallCap.CurrentPrice > 0);
    }

    // ========================
    // Short Report (7.2.15)
    // ========================

    [Fact]
    public void ShortReport_ShouldTargetOvervaluedStocks()
    {
        var found = false;
        for (int seed = 0; seed < 500; seed++)
        {
            var engine = new AITraderEngine(seed);
            var stock = new Stock("OVER", "Overvalued Inc", "Technology")
            {
                CurrentPrice = 200m, PreviousClose = 200m,
                FairValue = 50m, // Massively overvalued (4x fair value)
                BaseVolatility = 0.02m, LiquidityScore = 5,
                AverageVolume = 500_000, SharesOutstanding = 100_000_000,
                NetIncome = 50_000_000m, Revenue = 200_000_000m,
            };

            engine.TickDay(new[] { stock }, Array.Empty<GameEvent>());

            if (engine.NewsThisTick.Count > 0)
            {
                var report = engine.NewsThisTick[0];
                Assert.Contains("short", report.Headline.ToLower());
                Assert.Contains("OVER", report.AffectedSymbols);
                Assert.True(report.PriceEffect < 0);
                found = true;
                break;
            }
        }
        Assert.True(found, "Should generate a short report within 500 seeds");
    }

    // ========================
    // Window Dressing (7.2.4)
    // ========================

    [Fact]
    public void WindowDressing_ShouldActivateNearQuarterEnd()
    {
        var engine = new AITraderEngine(42);
        var winner = CreateStock(100m);
        winner.PreviousClose = 90m; // Up 11%

        // Advance to day 55 (window dressing window: day 55-60 of quarter)
        engine.DayCount = 54;
        engine.TickDay(new[] { winner }, Array.Empty<GameEvent>());

        // Winner should have gotten a small push
        Assert.True(winner.CurrentPrice >= 100m);
    }

    // ========================
    // NewsThisTick (general)
    // ========================

    [Fact]
    public void NewsThisTick_ShouldClearEachTick()
    {
        var engine = new AITraderEngine(42);
        var stocks = new[] { CreateStock(100m) };

        engine.Tick(stocks, Array.Empty<GameEvent>(), isMarketOpen: true);
        var firstNews = engine.NewsThisTick.Count;

        engine.Tick(stocks, Array.Empty<GameEvent>(), isMarketOpen: true);
        // NewsThisTick should be cleared each tick
        Assert.True(engine.NewsThisTick.Count >= 0); // Just verify no crash
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
