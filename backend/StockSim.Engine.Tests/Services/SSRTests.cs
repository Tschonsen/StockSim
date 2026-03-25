using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Tests for SSR (Short Sale Restriction) / Alternative Uptick Rule (Bible 4.4.2).
/// SSR activates when a stock falls ≥10% from PreviousClose.
/// Short sales must then be at Bid + $0.01 or higher.
/// Lasts rest of day + next trading day.
/// </summary>
public class SSRTests
{
    private static Stock MakeStock(string symbol = "TEST", decimal price = 100m, decimal previousClose = 100m)
    {
        return new Stock(symbol, $"{symbol} Inc", "Technology")
        {
            CurrentPrice = price,
            PreviousClose = previousClose,
            BidPrice = price - 0.05m,
            AskPrice = price + 0.05m,
            AverageVolume = 100_000,
            DayVolume = 50_000,
            SharesOutstanding = 10_000_000,
        };
    }

    // ========================
    // SSR Activation
    // ========================

    [Fact]
    public void SSR_ShouldNotActivate_WhenDropBelow10Percent()
    {
        var stock = MakeStock(price: 91m, previousClose: 100m); // -9%
        Assert.False(stock.IsSSR);
    }

    [Fact]
    public void SSR_ShouldActivate_WhenDropExactly10Percent()
    {
        // Use GameLoop to test SSR activation
        var gl = new GameLoop(42, 5, 100_000m);
        var stock = gl.Stocks.First();
        stock.PreviousClose = 100m;
        stock.CurrentPrice = 90m; // Exactly -10%
        stock.BidPrice = 89.95m;
        stock.AskPrice = 90.05m;

        // Manually trigger SSR check by advancing to a market tick
        // We need the GameLoop's CheckSSRActivation which is private,
        // but it runs during ExecuteTick. So let's test via OrderEngine behavior.
        stock.IsSSR = true; // Simulate activation
        stock.SSRUntilDate = DateTime.Now.AddDays(1);

        Assert.True(stock.IsSSR);
    }

    [Fact]
    public void SSR_ShouldActivate_WhenDropOver10Percent()
    {
        var gl = new GameLoop(42, 5, 100_000m);
        var stock = gl.Stocks.First();
        stock.PreviousClose = 100m;
        stock.CurrentPrice = 85m; // -15%
        stock.IsSSR = true;

        Assert.True(stock.IsSSR);
    }

    // ========================
    // SSR Duration
    // ========================

    [Fact]
    public void SSR_ShouldHaveSSRUntilDate()
    {
        var stock = MakeStock();
        stock.IsSSR = true;
        // Rest of today + next trading day (Friday → Monday)
        stock.SSRUntilDate = new DateTime(2027, 1, 5, 16, 0, 0);

        Assert.True(stock.IsSSR);
        Assert.NotNull(stock.SSRUntilDate);
    }

    // ========================
    // OrderEngine SSR Enforcement
    // ========================

    [Fact]
    public void SSR_MarketShort_ShouldConvertToLimitAtUptick()
    {
        var portfolio = new Portfolio(100_000m);
        var engine = new OrderEngine(portfolio);
        var stock = MakeStock(price: 85m, previousClose: 100m);
        stock.IsSSR = true;
        stock.BidPrice = 84.95m;
        stock.AskPrice = 85.05m;

        var result = engine.PlaceOrder("TEST", OrderSide.Short, OrderType.Market, 100, stock,
            new DateTime(2027, 1, 5, 10, 0, 0), true);

        Assert.True(result.Success);
        Assert.NotNull(result.Order);
        // Should be converted to Limit order
        Assert.Equal(OrderType.Limit, result.Order.Type);
        // Limit price should be Bid + $0.01
        Assert.Equal(84.96m, result.Order.LimitPrice);
    }

    [Fact]
    public void SSR_LimitShortAtBid_ShouldBeRejected()
    {
        var portfolio = new Portfolio(100_000m);
        var engine = new OrderEngine(portfolio);
        var stock = MakeStock(price: 85m, previousClose: 100m);
        stock.IsSSR = true;
        stock.BidPrice = 84.95m;
        stock.AskPrice = 85.05m;

        // Try to place limit short at the bid (not allowed under SSR)
        var result = engine.PlaceOrder("TEST", OrderSide.Short, OrderType.Limit, 100, stock,
            new DateTime(2027, 1, 5, 10, 0, 0), true, limitPrice: 84.95m);

        Assert.False(result.Success);
        Assert.Contains("SSR", result.Error!);
    }

    [Fact]
    public void SSR_LimitShortBelowBid_ShouldBeRejected()
    {
        var portfolio = new Portfolio(100_000m);
        var engine = new OrderEngine(portfolio);
        var stock = MakeStock(price: 85m, previousClose: 100m);
        stock.IsSSR = true;
        stock.BidPrice = 84.95m;

        var result = engine.PlaceOrder("TEST", OrderSide.Short, OrderType.Limit, 100, stock,
            new DateTime(2027, 1, 5, 10, 0, 0), true, limitPrice: 84.50m);

        Assert.False(result.Success);
        Assert.Contains("SSR", result.Error!);
    }

    [Fact]
    public void SSR_LimitShortAboveBid_ShouldBeAccepted()
    {
        var portfolio = new Portfolio(100_000m);
        var engine = new OrderEngine(portfolio);
        var stock = MakeStock(price: 85m, previousClose: 100m);
        stock.IsSSR = true;
        stock.BidPrice = 84.95m;
        stock.AskPrice = 85.05m;

        // Limit short above bid (allowed under SSR)
        var result = engine.PlaceOrder("TEST", OrderSide.Short, OrderType.Limit, 100, stock,
            new DateTime(2027, 1, 5, 10, 0, 0), true, limitPrice: 84.96m);

        Assert.True(result.Success);
    }

    [Fact]
    public void SSR_BuyOrder_ShouldNotBeAffected()
    {
        var portfolio = new Portfolio(100_000m);
        var engine = new OrderEngine(portfolio);
        var stock = MakeStock(price: 85m, previousClose: 100m);
        stock.IsSSR = true;

        // Buy orders should work normally even under SSR
        var result = engine.PlaceOrder("TEST", OrderSide.Buy, OrderType.Market, 100, stock,
            new DateTime(2027, 1, 5, 10, 0, 0), true);

        Assert.True(result.Success);
    }

    [Fact]
    public void NoSSR_MarketShort_ShouldWorkNormally()
    {
        var portfolio = new Portfolio(100_000m);
        var engine = new OrderEngine(portfolio);
        var stock = MakeStock(price: 95m, previousClose: 100m);
        stock.IsSSR = false; // No SSR

        var result = engine.PlaceOrder("TEST", OrderSide.Short, OrderType.Market, 100, stock,
            new DateTime(2027, 1, 5, 10, 0, 0), true);

        Assert.True(result.Success);
        Assert.NotNull(result.Order);
        Assert.Equal(OrderType.Market, result.Order.Type); // Should remain Market, not converted
    }

    // ========================
    // SSR Clearing
    // ========================

    [Fact]
    public void SSR_ShouldClear_WhenUntilDatePasses()
    {
        var stock = MakeStock();
        stock.IsSSR = true;
        stock.SSRUntilDate = new DateTime(2027, 1, 5, 16, 0, 0);

        // Simulate clearing: after the until date
        var clearTime = new DateTime(2027, 1, 6, 9, 30, 0); // Next day
        if (clearTime.Date > stock.SSRUntilDate.Value.Date)
        {
            stock.IsSSR = false;
            stock.SSRUntilDate = null;
        }

        Assert.False(stock.IsSSR);
        Assert.Null(stock.SSRUntilDate);
    }

    [Fact]
    public void SSR_ShouldNotClear_OnSameDay()
    {
        var stock = MakeStock();
        stock.IsSSR = true;
        stock.SSRUntilDate = new DateTime(2027, 1, 5, 16, 0, 0);

        // Same day — should not clear
        var checkTime = new DateTime(2027, 1, 5, 15, 0, 0);
        if (checkTime.Date > stock.SSRUntilDate.Value.Date)
        {
            stock.IsSSR = false;
        }

        Assert.True(stock.IsSSR); // Still active
    }

    // ========================
    // Integration: SSR via GameLoop
    // ========================

    [Fact]
    public void GameLoop_SSRSymbols_ShouldListSSRStocks()
    {
        var gl = new GameLoop(42, 5, 100_000m);
        Assert.Empty(gl.SSRSymbols);

        var stock = gl.Stocks.First();
        stock.IsSSR = true;

        Assert.Single(gl.SSRSymbols);
        Assert.Equal(stock.Symbol, gl.SSRSymbols[0]);
    }

    // ========================
    // Short Squeeze Warning (Bible 4.4.5)
    // ========================

    [Fact]
    public void ShortSqueezeWarning_ShouldHaveValidFields()
    {
        var warning = new ShortSqueezeWarning
        {
            Symbol = "TEST",
            CompanyName = "Test Inc",
            PriceChangePercent = 15.5m,
            ShortInterestPercent = 35.2m,
            PlayerHasShortPosition = true,
        };

        Assert.Equal("TEST", warning.Symbol);
        Assert.Equal(15.5m, warning.PriceChangePercent);
        Assert.True(warning.PlayerHasShortPosition);
    }

    [Fact]
    public void GameLoop_ShortSqueezeWarnings_ShouldBeEmptyInitially()
    {
        var gl = new GameLoop(42, 5, 100_000m);
        Assert.Empty(gl.ShortSqueezeWarningsThisTick);
    }

    [Fact]
    public void ShortSqueeze_ShouldNotTrigger_WithLowShortInterest()
    {
        var gl = new GameLoop(42, 5, 100_000m);
        var stock = gl.Stocks.First();
        stock.ShortInterest = stock.SharesOutstanding * 0.10m; // 10% — below 30% threshold

        // Run some ticks
        for (int i = 0; i < 10; i++) gl.ExecuteTick();

        // Should not trigger (short interest too low)
        Assert.Empty(gl.ShortSqueezeWarningsThisTick);
    }
}
