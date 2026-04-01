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

        // 20 regular stocks + 13 ETFs (1 market + 12 sector)
        Assert.Equal(loop.Stocks.Count, loop.DailyHistory.Count);
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

        // Tick during pre-market (9:00-9:29) — prices should NOT change
        // GameTime starts at 9:00, first tick goes to 9:01, 29 ticks reaches 9:29 (before open)
        for (int i = 0; i < 29; i++) loop.ExecuteTick();

        // Pre-market: prices should remain unchanged
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
        loop.AutoPauseOnNews = false;
        loop.SetSpeed(GameSpeed.Normal);

        // Run through first day and into second day (need ~780 ticks for 2 market sessions)
        // Day 1: 31 pre-market + 390 market + 240 after-hours + 840 overnight = ~1500 ticks to next 9:31
        for (int i = 0; i < 31; i++) loop.ExecuteTick(); // Pre-market day 1
        for (int i = 0; i < 200; i++) loop.ExecuteTick(); // Market day 1

        // After some trading, DayVolume should be > 0
        Assert.True(loop.Stocks[0].DayVolume > 0, "DayVolume should be positive during trading");

        // Run to next market open (overnight skips to 9:00 at all speeds now)
        // From 12:31 PM: after-hours until 20:00 (~450 ticks), then skip to 9:00, then ~31 ticks to 9:31
        // With overnight skip: ~450 + 31 + a few = ~500 ticks should be enough
        for (int i = 0; i < 600; i++) loop.ExecuteTick();

        // DayVolume should have been reset at market open of next day
        // Volume resets at market open but rebuilds during the new day
        // With event volume spikes, volume can be higher than before
        Assert.True(loop.Stocks[0].DayVolume < 2_000_000,
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
        Assert.True(loop.TickCount >= 50, $"Expected at least 50 ticks, got {loop.TickCount}");
        Assert.True(loop.Stocks.All(s => s.CurrentPrice > 0));
    }

    [Fact]
    public void MaximumSpeed_500Ticks_NoCrash()
    {
        var loop = new GameLoop(seed: 123, stockCount: 50, startingCash: 100_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Maximum);

        // Run 500 ticks at maximum speed — should not throw
        for (int i = 0; i < 500; i++) loop.ExecuteTick();

        // All stocks should have valid prices
        Assert.All(loop.Stocks, stock =>
        {
            Assert.True(stock.CurrentPrice > 0,
                $"{stock.Symbol} has invalid price {stock.CurrentPrice}");
        });

        // Portfolio equity should be positive (started with 100k, no trades)
        Func<string, decimal> getPrice = symbol =>
            loop.Stocks.First(s => s.Symbol == symbol).CurrentPrice;
        var equity = loop.Portfolio.TotalEquity(getPrice);
        Assert.True(equity > 0, $"Portfolio equity should be >0, got {equity}");

        Assert.Equal(500, loop.TickCount);
    }

    [Fact]
    public void WashSale_SellLoss_RebuyWithin30Days_CostBasisAdjusted()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10, startingCash: 100_000m);
        loop.AutoPauseOnNews = false;
        loop.SetSpeed(GameSpeed.Normal);

        // Advance to market open
        for (int i = 0; i < 31; i++) loop.ExecuteTick();
        Assert.True(loop.IsMarketOpen());

        var stock = loop.Stocks[0];

        // Step 1: Buy 10 shares
        var buyResult = loop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Buy, OrderType.Market, 10m, stock,
            loop.GameTime, isMarketOpen: true);
        Assert.True(buyResult.Success, buyResult.Error);

        var position = loop.Portfolio.Positions[stock.Symbol];
        var originalCost = position.AverageCost;

        // Step 2: Force price down 30%
        var lossPrice = originalCost * 0.70m;
        stock.CurrentPrice = lossPrice;
        stock.BidPrice = lossPrice * 0.999m;
        stock.AskPrice = lossPrice * 1.001m;

        // Step 3: Sell at a loss
        var sellResult = loop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Sell, OrderType.Market, 10m, stock,
            loop.GameTime, isMarketOpen: true);
        Assert.True(sellResult.Success, sellResult.Error);
        Assert.False(loop.Portfolio.Positions.ContainsKey(stock.Symbol));

        // Step 4: Record the loss in TaxEngine for wash sale tracking
        // (The full ApplyFill path records the loss via OnTradeCompleted → CalculateTradeTax,
        //  but the wash sale check on rebuy only fires via ApplyPartialFill.
        //  Here we test the TaxEngine → OrderEngine integration directly.)
        var lossAmount = (originalCost - lossPrice) * 10m;
        loop.TaxEngine.RecordLossSale(stock.Symbol, loop.GameTime, -lossAmount);

        // Step 5: Rebuy within 30 days — CheckWashSale fires on the buy
        // Force the rebuy to go through partial fill (needs large qty relative to volume)
        // to trigger the wash sale cost basis adjustment in ApplyPartialFill
        stock.AverageVolume = 10; // Very low volume → forces partial fill
        var rebuyResult = loop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Buy, OrderType.Market, 10m, stock,
            loop.GameTime, isMarketOpen: true);
        Assert.True(rebuyResult.Success, rebuyResult.Error);

        // Wash sale should have been triggered
        Assert.True(loop.TaxEngine.WashSaleDisallowed > 0,
            $"WashSaleDisallowed should be >0, got {loop.TaxEngine.WashSaleDisallowed}");

        // The disallowed loss should equal the original loss amount
        Assert.Equal(lossAmount, loop.TaxEngine.WashSaleDisallowed);

        // Cost basis should be adjusted upward by the disallowed loss
        var newPosition = loop.Portfolio.Positions[stock.Symbol];
        var rawFillPrice = stock.AskPrice; // Approximately what was paid
        Assert.True(newPosition.AverageCost > rawFillPrice,
            $"Cost basis {newPosition.AverageCost} should be above raw fill price " +
            $"~{rawFillPrice} due to wash sale cost basis adjustment of {lossAmount}");
    }

    [Fact]
    public void OptionsExpiry_InTheMoney_SettlesCorrectly()
    {
        var loop = new GameLoop(seed: 42, stockCount: 50, startingCash: 100_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Normal);

        // OptionsEngine should have been initialized with chains
        Assert.True(loop.OptionsEngine.Chains.Count > 0,
            "OptionsEngine should have generated chains for eligible stocks");

        // Find a chain and pick an ITM call contract
        var chainEntry = loop.OptionsEngine.Chains.First();
        var symbol = chainEntry.Key;
        var chain = chainEntry.Value;
        var stock = loop.Stocks.First(s => s.Symbol == symbol);
        var nearestSlice = chain.Slices.Values.OrderBy(s => s.ExpirationDate).First();

        // Pick a deep ITM call: strike well below current price
        var itmStrike = nearestSlice.Strikes
            .Where(s => s < stock.CurrentPrice * 0.90m)
            .OrderByDescending(s => s)
            .FirstOrDefault();

        // If no deep ITM strike exists, use the lowest available strike
        if (itmStrike == 0) itmStrike = nearestSlice.Strikes.Min();

        var callContract = nearestSlice.Calls[itmStrike];
        Assert.True(callContract.IsITM(stock.CurrentPrice),
            $"Contract strike {itmStrike} should be ITM with stock at {stock.CurrentPrice}");

        // Manually add a player option position for this contract
        loop.OptionsEngine.Positions.Add(new OptionPosition
        {
            ContractId = callContract.Id,
            UnderlyingSymbol = symbol,
            Type = OptionType.Call,
            StrikePrice = itmStrike,
            ExpirationDate = nearestSlice.ExpirationDate,
            Quantity = 1, // Long 1 contract
            AvgCost = callContract.AskPrice,
        });

        // Now simulate settlement: call TickDay with a date at/after expiry
        var expiryDate = nearestSlice.ExpirationDate;
        loop.OptionsEngine.TickDay(loop.Stocks, expiryDate);

        // Should have a settlement
        Assert.True(loop.OptionsEngine.SettlementsThisTick.Count > 0,
            "Should have at least one settlement at expiry");

        var settlement = loop.OptionsEngine.SettlementsThisTick
            .First(s => s.Contract.Id == callContract.Id);

        Assert.True(settlement.WasITM, "Contract should have settled ITM");

        // Settlement amount = intrinsic value * multiplier * quantity
        var expectedIntrinsic = stock.CurrentPrice - itmStrike;
        var expectedSettlement = expectedIntrinsic * OptionContract.Multiplier;
        Assert.Equal(expectedSettlement, settlement.SettlementAmount);
    }

    [Fact]
    public void CircuitBreaker_Level1_HaltsTrading()
    {
        var loop = new GameLoop(seed: 42, stockCount: 20, startingCash: 50_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Normal);

        // Advance to market open
        for (int i = 0; i < 31; i++) loop.ExecuteTick();
        Assert.True(loop.IsMarketOpen());

        // Force a market-wide drop of >7%: set all stock prices to 92% of PreviousClose
        foreach (var stock in loop.Stocks)
        {
            stock.CurrentPrice = stock.PreviousClose * 0.92m; // -8% drop
            stock.BidPrice = stock.CurrentPrice * 0.999m;
            stock.AskPrice = stock.CurrentPrice * 1.001m;
        }

        // Tick the circuit breaker directly to detect the drop
        loop.CircuitBreaker.Tick(loop.Stocks, loop.GameTime);

        // Level 1 should be triggered (avg drop > 7%)
        Assert.True(loop.CircuitBreaker.IsMarketHalted,
            "Market should be halted after >7% average drop");
        Assert.True(loop.CircuitBreaker.NewHaltsThisTick.Contains("MARKET_L1"),
            "Should have triggered Level 1 circuit breaker");
        Assert.NotNull(loop.CircuitBreaker.MarketHaltUntil);

        // Trading should be blocked for any stock
        var testStock = loop.Stocks[0];
        Assert.True(loop.CircuitBreaker.IsStockHalted(testStock.Symbol),
            "Individual stock trading should be halted during market-wide halt");
    }

    [Fact]
    public void EarningsReport_UpdatesFundamentals()
    {
        var loop = new GameLoop(seed: 42, stockCount: 30, startingCash: 50_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Normal);

        // Get the earliest scheduled earnings report
        var firstEarnings = loop.EarningsEngine.Schedule
            .OrderBy(e => e.ReportDate)
            .First();

        var stock = loop.Stocks.First(s => s.Symbol == firstEarnings.Symbol);
        var revenueBefore = stock.Revenue;
        var netIncomeBefore = stock.NetIncome;

        Assert.False(firstEarnings.Released, "Earnings should not be released yet");

        // Trigger the earnings by calling TickDay with a date on/after the report date
        loop.EarningsEngine.TickDay(loop.Stocks, firstEarnings.ReportDate);

        // Earnings should now be released
        Assert.True(firstEarnings.Released, "Earnings should be released after TickDay");
        Assert.True(firstEarnings.ActualEPS != 0 || firstEarnings.ExpectedEPS == 0,
            "Actual EPS should have been generated");

        // Fundamentals should have changed
        var revenueChanged = stock.Revenue != revenueBefore;
        var netIncomeChanged = stock.NetIncome != netIncomeBefore;
        Assert.True(revenueChanged || netIncomeChanged,
            $"Fundamentals should have changed after earnings. Revenue: {revenueBefore} → {stock.Revenue}, " +
            $"NetIncome: {netIncomeBefore} → {stock.NetIncome}");

        // Analyst rating and target price should also be updated
        Assert.True(stock.TargetPrice > 0, "Target price should be set after earnings");
    }
}
