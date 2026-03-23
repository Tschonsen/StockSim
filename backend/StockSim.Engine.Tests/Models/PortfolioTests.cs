using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

public class PortfolioTests
{
    [Fact]
    public void Portfolio_ShouldInitializeWithCash()
    {
        var portfolio = new Portfolio(50_000m);

        Assert.Equal(50_000m, portfolio.Cash);
        Assert.Equal(0m, portfolio.RealizedPnL);
        Assert.Equal(0m, portfolio.TotalCommissions);
        Assert.Equal(0, portfolio.TradeCount);
        Assert.Empty(portfolio.Positions);
        Assert.Empty(portfolio.Orders);
    }

    [Fact]
    public void PortfolioValue_ShouldSumAllPositions()
    {
        var portfolio = new Portfolio(10_000m);
        portfolio.Positions["AAPL"] = new Position("AAPL", 10m, 150m);
        portfolio.Positions["GOOG"] = new Position("GOOG", 5m, 100m);

        Func<string, decimal> getPrice = symbol => symbol switch
        {
            "AAPL" => 160m,
            "GOOG" => 110m,
            _ => 0m,
        };

        // 10*160 + 5*110 = 1600 + 550 = 2150
        Assert.Equal(2150m, portfolio.PortfolioValue(getPrice));
    }

    [Fact]
    public void TotalEquity_ShouldBeCashPlusPortfolioValue()
    {
        var portfolio = new Portfolio(10_000m);
        portfolio.Positions["AAPL"] = new Position("AAPL", 10m, 150m);

        Func<string, decimal> getPrice = _ => 160m;

        // 10000 + 10*160 = 11600
        Assert.Equal(11_600m, portfolio.TotalEquity(getPrice));
    }

    [Fact]
    public void TotalEquity_WithNoPositions_ShouldEqualCash()
    {
        var portfolio = new Portfolio(50_000m);

        Assert.Equal(50_000m, portfolio.TotalEquity(_ => 0m));
    }

    [Fact]
    public void TotalUnrealizedPnL_ShouldSumAllPositions()
    {
        var portfolio = new Portfolio(10_000m);
        portfolio.Positions["AAPL"] = new Position("AAPL", 10m, 150m); // Cost: 1500
        portfolio.Positions["GOOG"] = new Position("GOOG", 5m, 100m);  // Cost: 500

        Func<string, decimal> getPrice = symbol => symbol switch
        {
            "AAPL" => 160m,  // PnL: +100
            "GOOG" => 90m,   // PnL: -50
            _ => 0m,
        };

        // 100 + (-50) = 50
        Assert.Equal(50m, portfolio.TotalUnrealizedPnL(getPrice));
    }

    [Fact]
    public void PortfolioValue_WithNoPositions_ShouldBeZero()
    {
        var portfolio = new Portfolio(50_000m);

        Assert.Equal(0m, portfolio.PortfolioValue(_ => 100m));
    }

    [Fact]
    public void Orders_ShouldBeTrackable()
    {
        var portfolio = new Portfolio(50_000m);
        var order = new Order("AAPL", OrderSide.Buy, OrderType.Market, 10m, DateTime.Now);
        portfolio.Orders.Add(order);

        Assert.Single(portfolio.Orders);
        Assert.Equal("AAPL", portfolio.Orders[0].Symbol);
    }
}
