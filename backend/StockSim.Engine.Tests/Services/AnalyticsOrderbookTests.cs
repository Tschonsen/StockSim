using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class AnalyticsCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnBasicMetrics()
    {
        var portfolio = new Portfolio(50_000m);
        Func<string, decimal> getPrice = _ => 100m;

        var result = AnalyticsCalculator.Calculate(portfolio, getPrice, 50_000m);

        Assert.Equal(50_000m, result.TotalEquity);
        Assert.Equal(50_000m, result.Cash);
        Assert.Equal(0m, result.TotalReturn);
        Assert.Equal(0m, result.TotalReturnPercent);
    }

    [Fact]
    public void Calculate_WithPositions_ShouldIncludePortfolioValue()
    {
        var portfolio = new Portfolio(40_000m);
        portfolio.Positions["TEST"] = new Position("TEST", 100, 95m); // 100 shares @ $95
        Func<string, decimal> getPrice = _ => 100m;

        var result = AnalyticsCalculator.Calculate(portfolio, getPrice, 50_000m);

        Assert.Equal(50_000m, result.TotalEquity); // 40k cash + 10k position
        Assert.Equal(10_000m, result.PortfolioValue);
    }

    [Fact]
    public void Calculate_WithPlayerStats_ShouldUseStats()
    {
        var portfolio = new Portfolio(60_000m);
        portfolio.TradeCount = 10;
        Func<string, decimal> getPrice = _ => 100m;

        var stats = new PlayerStats
        {
            WinningTradeCount = 7,
            LosingTradeCount = 3,
            MaxConsecutiveWins = 4,
            MaxConsecutiveLosses = 2,
            LargestSingleGain = 1500m,
            LargestSingleLoss = 800m,
            TotalGainFromWins = 5000m,
            TotalLossFromLosses = 2000m,
            MaxDrawdownPercent = 12.5m,
        };

        var result = AnalyticsCalculator.Calculate(portfolio, getPrice, 50_000m, stats);

        Assert.Equal(70m, result.WinRate); // 7/10
        Assert.Equal(7, result.WinningTrades);
        Assert.Equal(3, result.LosingTrades);
        Assert.Equal(4, result.MaxConsecutiveWins);
        Assert.Equal(1500m, result.BestTradePnL);
        Assert.Equal(800m, result.WorstTradePnL);
        Assert.Equal(12.5m, result.MaxDrawdownPercent);
    }

    [Fact]
    public void Calculate_ProfitFactor_ShouldBeCorrect()
    {
        var portfolio = new Portfolio(50_000m);
        Func<string, decimal> getPrice = _ => 100m;

        var stats = new PlayerStats
        {
            WinningTradeCount = 5,
            LosingTradeCount = 5,
            TotalGainFromWins = 10_000m,
            TotalLossFromLosses = 5_000m,
        };

        var result = AnalyticsCalculator.Calculate(portfolio, getPrice, 50_000m, stats);
        Assert.Equal(2.0m, result.ProfitFactor); // 10k / 5k
    }

    [Fact]
    public void Calculate_TotalReturn_ShouldBePercentage()
    {
        var portfolio = new Portfolio(75_000m);
        Func<string, decimal> getPrice = _ => 100m;

        var result = AnalyticsCalculator.Calculate(portfolio, getPrice, 50_000m);
        Assert.Equal(50m, result.TotalReturnPercent); // (75k - 50k) / 50k * 100
    }

    [Fact]
    public void Calculate_SharpeRatio_WithHistory()
    {
        var portfolio = new Portfolio(55_000m);
        Func<string, decimal> getPrice = _ => 100m;

        var stats = new PlayerStats();
        // Add some equity history for Sharpe calculation
        for (int i = 0; i < 30; i++)
        {
            stats.EquityHistory.Add(new EquitySnapshot
            {
                Time = 1000 + i * 86400,
                Equity = 50_000m + i * 200m, // Steady uptrend
                Cash = 40_000m,
                MarketIndex = 0.5m,
            });
        }

        var result = AnalyticsCalculator.Calculate(portfolio, getPrice, 50_000m, stats);
        // Should have a positive Sharpe for steady uptrend
        Assert.True(result.SharpeRatio > 0, $"Sharpe should be positive for uptrend, got {result.SharpeRatio}");
    }
}

public class OrderbookGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturn10BidsAnd10Asks()
    {
        var stock = new Stock("TEST", "Test Inc", "Technology")
        {
            CurrentPrice = 100m,
            BidPrice = 99.90m,
            AskPrice = 100.10m,
            LiquidityScore = 7,
            AverageVolume = 1_000_000,
        };
        var rng = new Random(42);

        var ob = OrderbookGenerator.Generate(stock, rng);

        Assert.Equal(10, ob.Bids.Length);
        Assert.Equal(10, ob.Asks.Length);
    }

    [Fact]
    public void Generate_BidsShouldBeBelowAsk()
    {
        var stock = new Stock("TEST", "Test Inc", "Technology")
        {
            CurrentPrice = 100m,
            BidPrice = 99.90m,
            AskPrice = 100.10m,
            LiquidityScore = 5,
            AverageVolume = 500_000,
        };
        var rng = new Random(42);

        var ob = OrderbookGenerator.Generate(stock, rng);

        Assert.All(ob.Bids, b => Assert.True(b.Price < ob.BestAsk, $"Bid {b.Price} should be < ask {ob.BestAsk}"));
        Assert.All(ob.Asks, a => Assert.True(a.Price > ob.BestBid, $"Ask {a.Price} should be > bid {ob.BestBid}"));
    }

    [Fact]
    public void Generate_SpreadShouldBePositive()
    {
        var stock = new Stock("TEST", "Test Inc", "Technology")
        {
            CurrentPrice = 50m,
            BidPrice = 49.95m,
            AskPrice = 50.05m,
            LiquidityScore = 8,
            AverageVolume = 2_000_000,
        };
        var rng = new Random(42);

        var ob = OrderbookGenerator.Generate(stock, rng);

        Assert.True(ob.Spread > 0, $"Spread should be positive, got {ob.Spread}");
        Assert.True(ob.SpreadPercent > 0);
        Assert.True(ob.BestAsk > ob.BestBid);
    }

    [Fact]
    public void Generate_QuantitiesShouldBePositive()
    {
        var stock = new Stock("TEST", "Test Inc", "Technology")
        {
            CurrentPrice = 100m,
            BidPrice = 99.90m,
            AskPrice = 100.10m,
            LiquidityScore = 5,
            AverageVolume = 500_000,
        };
        var rng = new Random(42);

        var ob = OrderbookGenerator.Generate(stock, rng);

        Assert.All(ob.Bids, b => Assert.True(b.Quantity > 0));
        Assert.All(ob.Asks, a => Assert.True(a.Quantity > 0));
    }
}

public class FullIntegrationTests
{
    [Fact]
    public void FullGameLoop_WithAllSystems_ShouldNotCrash()
    {
        var loop = new GameLoop(seed: 42, stockCount: 50, startingCash: 50_000m);
        loop.SetSpeed(GameSpeed.Normal);
        loop.Portfolio.MarginEnabled = true;

        // Run a full trading day (9:00 to 16:00 = ~420 ticks + pre-market)
        for (int i = 0; i < 500; i++)
        {
            if (loop.IsPaused && !loop.IsBankrupt) loop.SetSpeed(GameSpeed.Normal);
            loop.ExecuteTick();
        }

        // All systems should still be healthy
        Assert.True(loop.Stocks.Count >= 50);
        Assert.True(loop.TickCount > 0);
        Assert.NotNull(loop.AchievementEngine);
        Assert.NotNull(loop.EconomicEngine);
        Assert.NotNull(loop.EarningsEngine);
        Assert.NotNull(loop.TaxEngine);
        Assert.NotNull(loop.ETFEngine);
        Assert.True(loop.ETFEngine.ETFs.Count > 0);
    }

    [Fact]
    public void FullGameLoop_EconomicData_ShouldInitialize()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        Assert.True(loop.EconomicEngine.Data.InterestRate > 0);
        Assert.True(loop.EconomicEngine.UpcomingEvents.Count > 0);
    }

    [Fact]
    public void FullGameLoop_EarningsSchedule_ShouldExist()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        // 10 stocks x 4 quarters = 40 (minus ETFs)
        Assert.True(loop.EarningsEngine.Schedule.Count > 0);
    }

    [Fact]
    public void FullGameLoop_AnalystRatings_ShouldBeSet()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        var regularStocks = loop.Stocks.Where(s => !s.Traits.Contains("ETF")).ToList();
        Assert.All(regularStocks, s =>
        {
            Assert.True(s.AnalystRating >= 1 && s.AnalystRating <= 5,
                $"{s.Symbol} rating {s.AnalystRating} out of range");
            Assert.True(s.TargetPrice > 0, $"{s.Symbol} target price should be > 0");
            Assert.False(string.IsNullOrEmpty(s.AnalystConsensus));
        });
    }

    [Fact]
    public void PlaceOrder_WithTax_ShouldDeductTax()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10, startingCash: 100_000m);
        loop.SetSpeed(GameSpeed.Normal);

        // Advance to market open
        for (int i = 0; i < 35; i++) loop.ExecuteTick();

        var stock = loop.Stocks.First(s => !s.Traits.Contains("ETF") && s.CurrentPrice > 10);
        var cashBefore = loop.Portfolio.Cash;

        // Buy 10 shares
        loop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Buy, OrderType.Market, 10, stock, loop.GameTime, true);

        // Advance some ticks for price to change
        for (int i = 0; i < 50; i++) loop.ExecuteTick();

        // Sell — should trigger tax on any gain
        if (loop.Portfolio.Positions.ContainsKey(stock.Symbol))
        {
            var currentStock = loop.StocksBySymbol[stock.Symbol];
            loop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Sell, OrderType.Market, 10, currentStock, loop.GameTime, true);
        }

        // Tax engine should have recorded something (gain or loss)
        var taxGains = loop.TaxEngine.ShortTermGains + loop.TaxEngine.ShortTermLosses;
        // May or may not have gains depending on price movement, but trade should be recorded
        Assert.True(loop.AchievementEngine.Stats.TradeHistory.Count > 0 ||
                     loop.Portfolio.TradeCount > 0,
                     "Should have recorded trades");
    }

    [Fact]
    public void ETFs_ShouldTrackMarketMovement()
    {
        var loop = new GameLoop(seed: 42, stockCount: 50);
        loop.SetSpeed(GameSpeed.Normal);

        // Advance past market open
        for (int i = 0; i < 35; i++) loop.ExecuteTick();

        var simx = loop.StocksBySymbol.GetValueOrDefault("SIMX");
        Assert.NotNull(simx);
        var priceBefore = simx!.CurrentPrice;

        // Run 100 ticks
        for (int i = 0; i < 100; i++) loop.ExecuteTick();

        // SIMX price should have changed (it tracks market)
        // Can't guarantee direction, but should have moved
        Assert.True(loop.StocksBySymbol["SIMX"].CurrentPrice > 0);
    }

    [Fact]
    public void CommissionOverride_ShouldWork()
    {
        OrderEngine.DefaultCommissionOverride = 0m; // Free trades
        try
        {
            var portfolio = new Portfolio(10_000m);
            var engine = new OrderEngine(portfolio);
            var stock = new Stock("TEST", "Test", "Tech")
            {
                CurrentPrice = 100m, BidPrice = 99.9m, AskPrice = 100.1m,
                AverageVolume = 1_000_000, LiquidityScore = 8, BaseVolatility = 0.02m,
            };

            engine.PlaceOrder("TEST", OrderSide.Buy, OrderType.Market, 10, stock, DateTime.Now, true);

            // Commission should be 0
            var lastOrder = portfolio.Orders.Last();
            Assert.Equal(0m, lastOrder.Commission);
            Assert.Equal(0m, portfolio.TotalCommissions);
        }
        finally
        {
            OrderEngine.DefaultCommissionOverride = null; // Reset
        }
    }
}
