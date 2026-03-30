using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

public class OrderTests : IDisposable
{
    public OrderTests() => Order.ResetIdCounter();
    public void Dispose() => Order.ResetIdCounter();

    [Fact]
    public void Order_ShouldAutoIncrementId()
    {
        var o1 = new Order("AAPL", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);
        var o2 = new Order("GOOG", OrderSide.Sell, OrderType.Limit, 5m, DateTime.Now, 150m);

        // IDs are sequential; exact values may vary with parallel test execution
        Assert.True(o2.Id == o1.Id + 1, $"o2.Id ({o2.Id}) should be o1.Id ({o1.Id}) + 1");
    }

    [Fact]
    public void Order_ShouldStartAsPending()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);

        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Order_ShouldStoreAllFields()
    {
        var now = new DateTime(2027, 1, 5, 10, 0, 0);
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Limit, 10m, now, 145m, TimeInForce.Day);

        Assert.Equal("AAPL", order.Symbol);
        Assert.Equal(OrderSide.Buy, order.Side);
        Assert.Equal(OrderType.Limit, order.Type);
        Assert.Equal(10m, order.Quantity);
        Assert.Equal(145m, order.LimitPrice);
        Assert.Equal(TimeInForce.Day, order.TimeInForce);
        Assert.Equal(now, order.PlacedAt);
    }

    [Fact]
    public void IsFilled_ShouldBeTrueWhenFilled()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);
        order.Status = OrderStatus.Filled;

        Assert.True(order.IsFilled);
    }

    [Fact]
    public void IsActive_ShouldBeTrueForPendingOpenPartiallyFilled()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Limit, 10m, DateTime.Now, 145m);

        order.Status = OrderStatus.Pending;
        Assert.True(order.IsActive);

        order.Status = OrderStatus.Open;
        Assert.True(order.IsActive);

        order.Status = OrderStatus.PartiallyFilled;
        Assert.True(order.IsActive);
    }

    [Fact]
    public void IsActive_ShouldBeFalseForTerminalStatuses()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);

        order.Status = OrderStatus.Filled;
        Assert.False(order.IsActive);

        order.Status = OrderStatus.Cancelled;
        Assert.False(order.IsActive);

        order.Status = OrderStatus.Rejected;
        Assert.False(order.IsActive);

        order.Status = OrderStatus.Expired;
        Assert.False(order.IsActive);
    }

    [Fact]
    public void RemainingQuantity_ShouldTrackFills()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Limit, 100m, DateTime.Now, 145m);
        order.FilledQuantity = 40m;

        Assert.Equal(60m, order.RemainingQuantity);
    }

    [Fact]
    public void ResetIdCounter_ShouldResetToOne()
    {
        _ = new Order("AAPL", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);
        _ = new Order("GOOG", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);

        Order.ResetIdCounter();

        var o3 = new Order("MSFT", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);
        Assert.Equal(1, o3.Id);
    }

    [Fact]
    public void ToString_ShouldIncludeRelevantInfo()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Limit, 10m, DateTime.Now, 145m);

        var str = order.ToString();
        Assert.Contains("AAPL", str);
        Assert.Contains("Buy", str);
        Assert.Contains("Limit", str);
        Assert.Contains("145", str);
    }

    [Fact]
    public void MarketOrder_ShouldHaveNoLimitPrice()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);

        Assert.Null(order.LimitPrice);
    }

    [Fact]
    public void Order_DefaultTimeInForce_ShouldBeGTC()
    {
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Limit, 10m, DateTime.Now, 145m);

        Assert.Equal(TimeInForce.GTC, order.TimeInForce);
    }
}
