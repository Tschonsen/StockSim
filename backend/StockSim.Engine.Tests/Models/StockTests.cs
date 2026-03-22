using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

public class StockTests
{
    [Fact]
    public void Stock_ShouldInitializeWithBasicProperties()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology");

        Assert.Equal("VTXD", stock.Symbol);
        Assert.Equal("Vertex Dynamics", stock.Name);
        Assert.Equal("Technology", stock.Sector);
    }

    [Fact]
    public void Stock_ShouldHaveDefaultPriceOfZero()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology");

        Assert.Equal(0m, stock.CurrentPrice);
        Assert.Equal(0m, stock.PreviousClose);
    }

    [Fact]
    public void Stock_ShouldCalculateDayChange()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology")
        {
            CurrentPrice = 142.58m,
            PreviousClose = 139.32m
        };

        Assert.Equal(3.26m, stock.DayChange);
        Assert.True(Math.Abs(stock.DayChangePercent - 2.34m) < 0.01m);
    }

    [Fact]
    public void Stock_ShouldCalculateNegativeDayChange()
    {
        var stock = new Stock("PTVE", "PetroVolt Energy", "Energy")
        {
            CurrentPrice = 45.20m,
            PreviousClose = 48.80m
        };

        Assert.Equal(-3.60m, stock.DayChange);
        Assert.True(stock.DayChangePercent < 0);
    }

    [Fact]
    public void Stock_ShouldHandleZeroPreviousClose()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology")
        {
            CurrentPrice = 100m,
            PreviousClose = 0m
        };

        Assert.Equal(0m, stock.DayChangePercent);
    }

    [Fact]
    public void Stock_ShouldTrackBidAskSpread()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology")
        {
            BidPrice = 142.45m,
            AskPrice = 142.63m
        };

        Assert.Equal(0.18m, stock.Spread);
        Assert.True(stock.SpreadPercent > 0);
    }

    [Fact]
    public void Stock_ShouldTrackVolume()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology")
        {
            DayVolume = 2_345_000,
            AverageVolume = 4_100_000
        };

        Assert.Equal(2_345_000, stock.DayVolume);
        Assert.Equal(4_100_000, stock.AverageVolume);
    }

    [Fact]
    public void Stock_ShouldHaveTraits()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology");
        stock.Traits.Add("Growth Stock");
        stock.Traits.Add("Momentum Stock");

        Assert.Contains("Growth Stock", stock.Traits);
        Assert.Contains("Momentum Stock", stock.Traits);
        Assert.Equal(2, stock.Traits.Count);
    }

    [Fact]
    public void Stock_ShouldTrackMarketCap()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology")
        {
            CurrentPrice = 142.58m,
            SharesOutstanding = 150_000_000
        };

        // MarketCap = Price * SharesOutstanding
        Assert.Equal(142.58m * 150_000_000, stock.MarketCap);
    }

    [Fact]
    public void Stock_ShouldTrackFloat()
    {
        var stock = new Stock("VTXD", "Vertex Dynamics", "Technology")
        {
            SharesOutstanding = 150_000_000,
            InsiderOwnership = 0.18m,
            InstitutionalOwnership = 0.52m
        };

        // Float = Outstanding * (1 - InsiderLockup)
        // Assuming ~60% of insider shares are locked
        Assert.True(stock.Float > 0);
        Assert.True(stock.Float < stock.SharesOutstanding);
        Assert.True(stock.FloatPercentage > 0.4m);
        Assert.True(stock.FloatPercentage < 1.0m);
    }
}
