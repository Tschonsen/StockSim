using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

public class PositionTests
{
    [Fact]
    public void Position_ShouldInitializeCorrectly()
    {
        var pos = new Position("AAPL", 10m, 150m);

        Assert.Equal("AAPL", pos.Symbol);
        Assert.Equal(10m, pos.Shares);
        Assert.Equal(150m, pos.AverageCost);
    }

    [Fact]
    public void TotalCost_ShouldBeSharesTimesAvgCost()
    {
        var pos = new Position("AAPL", 10m, 150m);

        Assert.Equal(1500m, pos.TotalCost);
    }

    [Fact]
    public void MarketValue_ShouldUseCurrentPrice()
    {
        var pos = new Position("AAPL", 10m, 150m);

        Assert.Equal(1600m, pos.MarketValue(160m));
    }

    [Fact]
    public void UnrealizedPnL_ShouldBePositiveWhenPriceUp()
    {
        var pos = new Position("AAPL", 10m, 150m);

        Assert.Equal(100m, pos.UnrealizedPnL(160m));
    }

    [Fact]
    public void UnrealizedPnL_ShouldBeNegativeWhenPriceDown()
    {
        var pos = new Position("AAPL", 10m, 150m);

        Assert.Equal(-50m, pos.UnrealizedPnL(145m));
    }

    [Fact]
    public void UnrealizedPnLPercent_ShouldCalculateCorrectly()
    {
        var pos = new Position("AAPL", 10m, 100m);

        Assert.Equal(10m, pos.UnrealizedPnLPercent(110m));
    }

    [Fact]
    public void AddShares_ShouldRecalculateAverageCost()
    {
        var pos = new Position("AAPL", 10m, 100m);

        pos.AddShares(10m, 120m);

        Assert.Equal(20m, pos.Shares);
        Assert.Equal(110m, pos.AverageCost); // (1000 + 1200) / 20
    }

    [Fact]
    public void AddShares_MultipleAdds_ShouldAccumulate()
    {
        var pos = new Position("AAPL", 10m, 100m);
        pos.AddShares(5m, 110m);
        pos.AddShares(5m, 120m);

        Assert.Equal(20m, pos.Shares);
        // (1000 + 550 + 600) / 20 = 107.5
        Assert.Equal(107.5m, pos.AverageCost);
    }

    [Fact]
    public void RemoveShares_ShouldReturnRealizedPnL()
    {
        var pos = new Position("AAPL", 10m, 100m);

        var pnl = pos.RemoveShares(5m, 120m);

        Assert.Equal(100m, pnl); // 5 * (120 - 100)
        Assert.Equal(5m, pos.Shares);
        Assert.Equal(100m, pos.AverageCost); // Avg cost doesn't change on sell
    }

    [Fact]
    public void RemoveShares_NegativePnL()
    {
        var pos = new Position("AAPL", 10m, 100m);

        var pnl = pos.RemoveShares(5m, 90m);

        Assert.Equal(-50m, pnl); // 5 * (90 - 100)
    }

    [Fact]
    public void RemoveShares_AllShares_ShouldZeroOut()
    {
        var pos = new Position("AAPL", 10m, 100m);

        pos.RemoveShares(10m, 120m);

        Assert.Equal(0m, pos.Shares);
        Assert.Equal(0m, pos.AverageCost);
    }

    [Fact]
    public void RemoveShares_MoreThanOwned_ShouldThrow()
    {
        var pos = new Position("AAPL", 10m, 100m);

        Assert.Throws<InvalidOperationException>(() => pos.RemoveShares(15m, 120m));
    }

    [Fact]
    public void FractionalShares_ShouldWork()
    {
        var pos = new Position("AAPL", 3.512m, 142.58m);

        Assert.Equal(3.512m, pos.Shares);
        var value = pos.MarketValue(150m);
        Assert.Equal(526.80m, value);
    }

    [Fact]
    public void ToString_ShouldIncludeRelevantInfo()
    {
        var pos = new Position("AAPL", 10m, 150m);

        var str = pos.ToString();
        Assert.Contains("AAPL", str);
        Assert.Contains("10", str);
        Assert.Contains("150", str);
    }
}
