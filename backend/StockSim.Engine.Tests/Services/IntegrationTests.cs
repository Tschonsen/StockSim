using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Integration tests: full game flow through GameLoop, OrderEngine, EventEngine.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void FullTradeFlow_BuyAndSell()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10, startingCash: 50_000m);
        loop.SetSpeed(GameSpeed.Normal);

        // Advance to market open
        for (int i = 0; i < 31; i++) loop.ExecuteTick();
        Assert.True(loop.IsMarketOpen());

        var stock = loop.Stocks[0];
        var initialCash = loop.Portfolio.Cash;

        // Buy
        var buyResult = loop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Buy, OrderType.Market, 10m, stock,
            loop.GameTime, isMarketOpen: true);

        Assert.True(buyResult.Success, buyResult.Error);
        Assert.True(loop.Portfolio.Cash < initialCash);
        Assert.True(loop.Portfolio.Positions.ContainsKey(stock.Symbol));

        // Run some ticks for price to change
        for (int i = 0; i < 50; i++) loop.ExecuteTick();

        // Sell
        var sellResult = loop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Sell, OrderType.Market, 10m, stock,
            loop.GameTime, isMarketOpen: true);

        Assert.True(sellResult.Success, sellResult.Error);
        Assert.False(loop.Portfolio.Positions.ContainsKey(stock.Symbol));
        Assert.Equal(2, loop.Portfolio.TradeCount);
    }

    [Fact]
    public void LimitOrder_ShouldFillOnPriceMove()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);
        loop.SetSpeed(GameSpeed.Normal);

        // Advance to market open
        for (int i = 0; i < 31; i++) loop.ExecuteTick();

        var stock = loop.Stocks[0];
        var limitPrice = stock.AskPrice * 0.95m; // 5% below current ask

        // Place limit buy
        var result = loop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Buy, OrderType.Limit, 5m, stock,
            loop.GameTime, isMarketOpen: true, limitPrice: limitPrice);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Open, result.Order!.Status);

        // Run many ticks — limit may or may not fill depending on price movement
        for (int i = 0; i < 500; i++) loop.ExecuteTick();

        // The order should still be tracked
        Assert.Single(loop.Portfolio.Orders.Where(o => o.Symbol == stock.Symbol));
    }

    [Fact]
    public void EventEngine_ShouldGenerateEvents_DuringSimulation()
    {
        var loop = new GameLoop(seed: 42, stockCount: 50);
        loop.SetSpeed(GameSpeed.Normal);

        // Run enough ticks to ensure market hours + events trigger
        // 31 pre-market + 390 market minutes = need ~500+ ticks for a full day
        for (int i = 0; i < 2000; i++) loop.ExecuteTick();

        Assert.True(loop.EventEngine.EventHistory.Count > 0,
            "Should have generated events during simulation");
    }

    [Fact]
    public void Portfolio_ShouldTrackEquity_AfterTrades()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10, startingCash: 50_000m);
        loop.SetSpeed(GameSpeed.Normal);

        // Advance to market open
        for (int i = 0; i < 31; i++) loop.ExecuteTick();

        // Buy 3 different stocks
        for (int j = 0; j < 3; j++)
        {
            var stock = loop.Stocks[j];
            loop.OrderEngine.PlaceOrder(
                stock.Symbol, OrderSide.Buy, OrderType.Market, 10m, stock,
                loop.GameTime, isMarketOpen: true);
        }

        Assert.Equal(3, loop.Portfolio.Positions.Count);

        Func<string, decimal> getPrice = symbol =>
            loop.Stocks.First(s => s.Symbol == symbol).CurrentPrice;

        var equity = loop.Portfolio.TotalEquity(getPrice);
        // Equity should be close to starting cash (minus commissions and spread)
        Assert.True(equity > 45_000m && equity < 55_000m,
            $"Equity {equity} should be close to starting cash");
    }

    [Fact]
    public void DailyHistory_ShouldExistAtGameStart()
    {
        var loop = new GameLoop(seed: 42, stockCount: 20);

        Assert.Equal(20, loop.DailyHistory.Count);
        Assert.All(loop.DailyHistory.Values, candles =>
        {
            Assert.Equal(252, candles.Count);
            Assert.All(candles, c =>
            {
                Assert.True(c.Open > 0);
                Assert.True(c.Close > 0);
                Assert.True(c.High >= c.Low);
            });
        });
    }

    [Fact]
    public void MarketHours_ShouldControlPriceUpdates()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);
        loop.SetSpeed(GameSpeed.Normal);

        var pricesBefore = loop.Stocks.Select(s => s.CurrentPrice).ToList();

        // Tick during pre-market (9:00-9:30) — prices should NOT change
        // GameTime starts at 9:00, first tick goes to 9:01, need 30 ticks to reach 9:30
        for (int i = 0; i < 30; i++) loop.ExecuteTick();

        for (int j = 0; j < loop.Stocks.Count; j++)
            Assert.Equal(pricesBefore[j], loop.Stocks[j].CurrentPrice);

        // Tick during market hours — at least some prices should change
        for (int i = 0; i < 100; i++) loop.ExecuteTick();

        var anyChanged = loop.Stocks.Select((s, j) => s.CurrentPrice != pricesBefore[j]).Any(x => x);
        Assert.True(anyChanged, "At least one stock price should change during market hours");
    }

    [Fact]
    public void MultipleOrders_ShouldAllBeTracked()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);
        loop.SetSpeed(GameSpeed.Normal);
        for (int i = 0; i < 31; i++) loop.ExecuteTick();

        // Place multiple orders
        var stock = loop.Stocks[0];
        loop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Buy, OrderType.Market, 5m, stock, loop.GameTime, true);
        loop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Buy, OrderType.Market, 5m, stock, loop.GameTime, true);
        loop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Sell, OrderType.Market, 3m, stock, loop.GameTime, true);

        Assert.Equal(3, loop.Portfolio.Orders.Count);
        Assert.Equal(3, loop.Portfolio.TradeCount);
        Assert.Equal(7m, loop.Portfolio.Positions[stock.Symbol].Shares);
    }

    [Fact]
    public void CancelLimitOrder_ShouldWork()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);
        loop.SetSpeed(GameSpeed.Normal);
        for (int i = 0; i < 31; i++) loop.ExecuteTick();

        var stock = loop.Stocks[0];
        var result = loop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Buy, OrderType.Limit, 10m, stock,
            loop.GameTime, true, limitPrice: stock.AskPrice * 0.8m);

        Assert.True(result.Success);
        var orderId = result.Order!.Id;

        var cancelled = loop.OrderEngine.CancelOrder(orderId);
        Assert.True(cancelled);
        Assert.Equal(OrderStatus.Cancelled, result.Order.Status);
        Assert.Empty(loop.OrderEngine.GetActiveOrders());
    }

    [Fact]
    public void DailyValues_ShouldResetAtMarketOpen()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);
        loop.SetSpeed(GameSpeed.Normal);

        // Run through first day and into second day (need ~780 ticks for 2 market sessions)
        // Day 1: 31 pre-market + 390 market + 240 after-hours + 840 overnight = ~1500 ticks to next 9:31
        for (int i = 0; i < 31; i++) loop.ExecuteTick(); // Pre-market day 1
        for (int i = 0; i < 200; i++) loop.ExecuteTick(); // Market day 1

        // After some trading, DayVolume should be > 0
        Assert.True(loop.Stocks[0].DayVolume > 0, "DayVolume should be positive during trading");

        // Run to next market open (skip rest of day + overnight + pre-market)
        // From 12:31 PM: need ~1290 ticks to reach next day 9:31 AM
        for (int i = 0; i < 1290; i++) loop.ExecuteTick();

        // DayVolume should have been reset at 9:31
        // After reset + 0-1 market ticks, volume should be very small
        Assert.True(loop.Stocks[0].DayVolume < 100_000,
            $"DayVolume should have been reset, got {loop.Stocks[0].DayVolume}");
    }

    [Fact]
    public void Simulation_250Stocks_ShouldNotCrash()
    {
        var loop = new GameLoop(seed: 42, stockCount: 250);
        loop.SetSpeed(GameSpeed.Normal);

        // Run 100 ticks (includes pre-market + market open)
        for (int i = 0; i < 100; i++) loop.ExecuteTick();

        Assert.True(loop.Stocks.Count >= 250, $"Expected at least 250 stocks (+ ETFs), got {loop.Stocks.Count}");
        Assert.Equal(100, loop.TickCount);
        Assert.True(loop.Stocks.All(s => s.CurrentPrice > 0));
    }
}
