using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class OrderEngineTests : IDisposable
{
    private readonly OrderEngine _engine;
    private readonly Portfolio _portfolio;
    private readonly Stock _stock;
    private readonly DateTime _now = new(2027, 1, 5, 10, 0, 0); // Mon 10 AM (market open)

    public OrderEngineTests()
    {
        Order.ResetIdCounter();
        _portfolio = new Portfolio(50_000m);
        _engine = new OrderEngine(_portfolio);
        _stock = CreateStock("AAPL", 150m);
    }

    public void Dispose() => Order.ResetIdCounter();

    // --- Market Buy ---

    [Fact]
    public void MarketBuy_ShouldExecuteAtAskPrice()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.True(result.Success, result.Error);
        Assert.Equal(OrderStatus.Filled, result.Order!.Status);
        Assert.Equal(_stock.AskPrice, result.Order.FillPrice);
    }

    [Fact]
    public void MarketBuy_ShouldReduceCash()
    {
        var cashBefore = _portfolio.Cash;

        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        var expectedCost = _stock.AskPrice * 10m + OrderEngine.DefaultCommission;
        Assert.Equal(cashBefore - expectedCost, _portfolio.Cash);
    }

    [Fact]
    public void MarketBuy_ShouldCreatePosition()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.True(_portfolio.Positions.ContainsKey("AAPL"));
        Assert.Equal(10m, _portfolio.Positions["AAPL"].Shares);
        Assert.Equal(_stock.AskPrice, _portfolio.Positions["AAPL"].AverageCost);
    }

    [Fact]
    public void MarketBuy_ShouldAddToExistingPosition()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);
        var firstCost = _stock.AskPrice;

        // Change price and buy more
        _stock.AskPrice = 160m;
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.Equal(20m, _portfolio.Positions["AAPL"].Shares);
        // Average cost should be between the two prices
        var avgCost = _portfolio.Positions["AAPL"].AverageCost;
        Assert.True(avgCost > firstCost && avgCost < 160m);
    }

    [Fact]
    public void MarketBuy_ShouldRejectIfInsufficientCash()
    {
        _portfolio.Cash = 100m;

        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.False(result.Success);
        Assert.Contains("Insufficient funds", result.Error);
        Assert.Equal(OrderStatus.Rejected, result.Order!.Status);
    }

    [Fact]
    public void MarketBuy_ShouldRejectZeroQuantity()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 0m, _stock, _now, isMarketOpen: true);

        Assert.False(result.Success);
        Assert.Contains("positive", result.Error);
    }

    [Fact]
    public void MarketBuy_ShouldChargeCommission()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.Equal(OrderEngine.DefaultCommission, result.Order!.Commission);
        Assert.Equal(OrderEngine.DefaultCommission, _portfolio.TotalCommissions);
    }

    // --- Market Sell ---

    [Fact]
    public void MarketSell_ShouldExecuteAtBidPrice()
    {
        // First buy some shares
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 5m, _stock, _now, isMarketOpen: true);

        Assert.True(result.Success, result.Error);
        Assert.Equal(_stock.BidPrice, result.Order!.FillPrice);
    }

    [Fact]
    public void MarketSell_ShouldIncreaseCash()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);
        var cashBefore = _portfolio.Cash;

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 5m, _stock, _now, isMarketOpen: true);

        var expectedProceeds = _stock.BidPrice * 5m - OrderEngine.DefaultCommission;
        Assert.Equal(cashBefore + expectedProceeds, _portfolio.Cash);
    }

    [Fact]
    public void MarketSell_ShouldReducePosition()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 7m, _stock, _now, isMarketOpen: true);

        Assert.Equal(3m, _portfolio.Positions["AAPL"].Shares);
    }

    [Fact]
    public void MarketSell_ShouldRemovePositionWhenSellAll()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.False(_portfolio.Positions.ContainsKey("AAPL"));
    }

    [Fact]
    public void MarketSell_ShouldRejectIfNoPosition()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.False(result.Success);
        Assert.Contains("No position", result.Error);
    }

    [Fact]
    public void MarketSell_ShouldRejectIfInsufficientShares()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 5m, _stock, _now, isMarketOpen: true);

        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.False(result.Success);
        Assert.Contains("only own", result.Error);
    }

    [Fact]
    public void MarketSell_ShouldTrackRealizedPnL()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        // Price rises
        _stock.BidPrice = 170m;
        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.True(_portfolio.RealizedPnL > 0, $"Should have profit, got {_portfolio.RealizedPnL}");
    }

    // --- Market Closed ---

    [Fact]
    public void MarketOrder_WhenMarketClosed_ShouldBePending()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: false);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Pending, result.Order!.Status);
        // Cash should NOT be deducted yet
        Assert.Equal(50_000m, _portfolio.Cash);
    }

    // --- Limit Buy ---

    [Fact]
    public void LimitBuy_BelowCurrentPrice_ShouldBeOpen()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 140m);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Open, result.Order!.Status);
    }

    [Fact]
    public void LimitBuy_AboveAskPrice_ShouldExecuteImmediately()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 160m);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Filled, result.Order!.Status);
        // Should fill at ask price (better than limit)
        Assert.Equal(_stock.AskPrice, result.Order.FillPrice);
    }

    [Fact]
    public void LimitBuy_ShouldTriggerWhenPriceDrops()
    {
        // Place limit buy at $140
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 140m);

        // Simulate price drop
        _stock.AskPrice = 139m;
        _stock.CurrentPrice = 139m;

        var fills = _engine.CheckLimitOrders(_stock, _now, isMarketOpen: true);

        Assert.Single(fills);
        Assert.Equal(OrderStatus.Filled, fills[0].Status);
        Assert.Equal(139m, fills[0].FillPrice); // Fills at ask (better than limit)
        Assert.True(_portfolio.Positions.ContainsKey("AAPL"));
    }

    // --- Limit Sell ---

    [Fact]
    public void LimitSell_AboveCurrentPrice_ShouldBeOpen()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 160m);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Open, result.Order!.Status);
    }

    [Fact]
    public void LimitSell_BelowBidPrice_ShouldExecuteImmediately()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 140m);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Filled, result.Order!.Status);
        Assert.Equal(_stock.BidPrice, result.Order.FillPrice); // Fills at bid (better than limit)
    }

    [Fact]
    public void LimitSell_ShouldTriggerWhenPriceRises()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 160m);

        // Simulate price rise
        _stock.BidPrice = 161m;
        _stock.CurrentPrice = 161m;

        var fills = _engine.CheckLimitOrders(_stock, _now, isMarketOpen: true);

        Assert.Single(fills);
        Assert.Equal(OrderStatus.Filled, fills[0].Status);
        Assert.False(_portfolio.Positions.ContainsKey("AAPL")); // Position closed
    }

    // --- Limit Missing LimitPrice ---

    [Fact]
    public void LimitOrder_WithoutLimitPrice_ShouldReject()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: null);

        Assert.False(result.Success);
        Assert.Contains("Limit price", result.Error);
    }

    // --- Day Order Expiry ---

    [Fact]
    public void DayOrder_ShouldExpireAtMarketClose()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 140m, timeInForce: TimeInForce.Day);

        var expired = _engine.ExpireDayOrders();

        Assert.Single(expired);
        Assert.Equal(OrderStatus.Expired, expired[0].Status);
    }

    // --- Order Tracking ---

    [Fact]
    public void Orders_ShouldBeTrackedInPortfolio()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);
        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Market, 5m, _stock, _now, isMarketOpen: true);

        Assert.Equal(2, _portfolio.Orders.Count);
        Assert.Equal(2, _portfolio.TradeCount);
    }

    [Fact]
    public void ActiveOrders_ShouldReturnOnlyOpenOrders()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true); // Filled
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 140m); // Open

        var active = _engine.GetActiveOrders();
        Assert.Single(active);
        Assert.Equal(OrderType.Limit, active[0].Type);
    }

    [Fact]
    public void CancelOrder_ShouldWork()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Limit, 10m, _stock, _now,
            isMarketOpen: true, limitPrice: 140m);

        var order = _portfolio.Orders[0];
        var cancelled = _engine.CancelOrder(order.Id);

        Assert.True(cancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    // --- Pending Orders (market closed) ---

    [Fact]
    public void PendingMarketBuy_ShouldExecuteAtMarketOpen()
    {
        // Place when market closed
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: false);

        Assert.Equal(OrderStatus.Pending, _portfolio.Orders[0].Status);

        // Market opens
        var fills = _engine.ExecutePendingOrders(_stock, _now, isMarketOpen: true);

        Assert.Single(fills);
        Assert.Equal(OrderStatus.Filled, fills[0].Status);
        Assert.True(_portfolio.Positions.ContainsKey("AAPL"));
    }

    // --- Slippage ---

    [Fact]
    public void MarketBuy_LargeOrder_ShouldHaveSlippage()
    {
        _portfolio.Cash = 10_000_000m;
        // Order for 10% of average volume should have noticeable slippage
        _stock.AverageVolume = 100_000;
        var hugeQuantity = 10_000m; // 10% of avg volume

        var result = _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, hugeQuantity, _stock, _now, isMarketOpen: true);

        Assert.True(result.Success);
        Assert.True(result.Order!.FillPrice > _stock.AskPrice,
            $"Fill price {result.Order.FillPrice} should be > ask {_stock.AskPrice} due to slippage");
    }

    // --- Stop Orders (Spec 4.2.5) ---

    [Fact]
    public void StopSell_ShouldBeOpen_WhenPriceAboveStop()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Stop, 10m, _stock, _now,
            isMarketOpen: true, stopPrice: 140m);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Open, result.Order!.Status);
    }

    [Fact]
    public void StopSell_ShouldTrigger_WhenPriceDropsBelowStop()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Stop, 10m, _stock, _now,
            isMarketOpen: true, stopPrice: 145m);

        // Price drops below stop
        _stock.CurrentPrice = 144m;
        _stock.BidPrice = 143.90m;

        var fills = _engine.CheckStopOrders(_stock, _now, isMarketOpen: true);

        Assert.Single(fills);
        Assert.Equal(OrderStatus.Filled, fills[0].Status);
        Assert.False(_portfolio.Positions.ContainsKey("AAPL"));
    }

    [Fact]
    public void StopSell_ShouldNotTrigger_WhenPriceAboveStop()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Stop, 10m, _stock, _now,
            isMarketOpen: true, stopPrice: 140m);

        // Price stays above stop
        _stock.CurrentPrice = 152m;

        var fills = _engine.CheckStopOrders(_stock, _now, isMarketOpen: true);
        Assert.Empty(fills);
    }

    // --- Stop-Limit (Spec 4.2.6) ---

    [Fact]
    public void StopLimit_ShouldTriggerThenCreateLimitOrder()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.StopLimit, 10m, _stock, _now,
            isMarketOpen: true, stopPrice: 145m, limitPrice: 144m);

        // Price drops below stop trigger
        _stock.CurrentPrice = 144.50m;
        _stock.BidPrice = 144.40m;

        var fills = _engine.CheckStopOrders(_stock, _now, isMarketOpen: true);

        // Stop triggers but price is above limit, so it becomes an open limit order
        var order = _portfolio.Orders.Last(o => o.Type == OrderType.StopLimit);
        Assert.True(order.StopTriggered);
    }

    // --- Trailing Stop (Spec 4.2.7) ---

    [Fact]
    public void TrailingStop_ShouldTrackHighWaterMark()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.TrailingStop, 10m, _stock, _now,
            isMarketOpen: true, trailAmount: 5m);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Open, result.Order!.Status);
        // Initial stop = currentPrice - trail = 150 - 5 = 145
        Assert.Equal(145m, result.Order.StopPrice);
    }

    [Fact]
    public void TrailingStop_ShouldRaiseStopWhenPriceRises()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.TrailingStop, 10m, _stock, _now,
            isMarketOpen: true, trailAmount: 5m);

        // Price rises
        _stock.CurrentPrice = 160m;
        _engine.CheckStopOrders(_stock, _now, isMarketOpen: true);

        var order = _portfolio.Orders.Last(o => o.Type == OrderType.TrailingStop);
        // Stop should have risen to 160 - 5 = 155
        Assert.Equal(155m, order.StopPrice);
        Assert.Equal(160m, order.HighWaterMark);
    }

    [Fact]
    public void TrailingStop_ShouldTriggerWhenPriceFalls()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Buy, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.TrailingStop, 10m, _stock, _now,
            isMarketOpen: true, trailAmount: 5m);

        // Price rises then falls below trailing stop
        _stock.CurrentPrice = 160m;
        _engine.CheckStopOrders(_stock, _now, isMarketOpen: true);

        _stock.CurrentPrice = 154m;
        _stock.BidPrice = 153.90m;
        var fills = _engine.CheckStopOrders(_stock, _now, isMarketOpen: true);

        Assert.Single(fills);
        Assert.Equal(OrderStatus.Filled, fills[0].Status);
    }

    // --- Short Selling (Spec 4.4) ---

    [Fact]
    public void Short_ShouldCreateNegativePosition()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Short, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.True(result.Success, result.Error);
        Assert.True(_portfolio.Positions.ContainsKey("AAPL"));
        Assert.True(_portfolio.Positions["AAPL"].IsShort);
        Assert.Equal(-10m, _portfolio.Positions["AAPL"].Shares);
    }

    [Fact]
    public void Short_ShouldCreditCash()
    {
        var cashBefore = _portfolio.Cash;

        _engine.PlaceOrder("AAPL", OrderSide.Short, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        // Short sells at bid, receives proceeds minus commission
        var expected = _stock.BidPrice * 10m - OrderEngine.DefaultCommission;
        Assert.Equal(cashBefore + expected, _portfolio.Cash);
    }

    [Fact]
    public void Cover_ShouldCloseShortPosition()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Short, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        var result = _engine.PlaceOrder("AAPL", OrderSide.Cover, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.True(result.Success, result.Error);
        Assert.False(_portfolio.Positions.ContainsKey("AAPL"));
    }

    [Fact]
    public void Cover_WithoutShortPosition_ShouldReject()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Cover, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.False(result.Success);
        Assert.Contains("No short position", result.Error);
    }

    [Fact]
    public void Short_ShouldProfitWhenPriceFalls()
    {
        _engine.PlaceOrder("AAPL", OrderSide.Short, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        // Price drops
        _stock.AskPrice = 140m;
        _stock.CurrentPrice = 140m;

        _engine.PlaceOrder("AAPL", OrderSide.Cover, OrderType.Market, 10m, _stock, _now, isMarketOpen: true);

        Assert.True(_portfolio.RealizedPnL > 0, $"Should profit on short when price falls, got {_portfolio.RealizedPnL}");
    }

    [Fact]
    public void StopOrder_WithoutStopPrice_ShouldReject()
    {
        var result = _engine.PlaceOrder("AAPL", OrderSide.Sell, OrderType.Stop, 10m, _stock, _now,
            isMarketOpen: true);

        Assert.False(result.Success);
        Assert.Contains("Stop price", result.Error);
    }

    // --- Helpers ---

    private static Stock CreateStock(string symbol, decimal price)
    {
        return new Stock(symbol, $"{symbol} Corp", "Technology")
        {
            CurrentPrice = price,
            PreviousClose = price,
            BidPrice = price - 0.10m,
            AskPrice = price + 0.10m,
            DayHigh = price,
            DayLow = price,
            BaseVolatility = 0.02m,
            LiquidityScore = 7,
            FairValue = price,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
        };
    }
}
