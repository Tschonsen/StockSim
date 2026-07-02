using System.Linq;
using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Emergent coupling, the two level-based channels (design/EMERGENT_COUPLING.md). Demand: an off-baseline
/// demand driver shifts the growth rate a company sustains. Valuation: an off-baseline driver moves the
/// PE multiple (fair value) without touching earnings. Both react to a driver's deviation from normal.
/// </summary>
public class DemandValuationTests
{
    [Fact]
    public void Demand_ConfidenceLifts_RatesSap_Growth()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        var consumer = MakeCompany("SHOP", "Consumer Goods");
        consumer.DriverExposures.Add(new DriverExposure("ConsumerConfidence", ExposureChannel.Demand, 0.4m));
        var homebuilder = MakeCompany("HOME", "Real Estate");
        homebuilder.DriverExposures.Add(new DriverExposure("InterestRate", ExposureChannel.Demand, -0.8m));
        loop.MutableStocks.Add(consumer);
        loop.MutableStocks.Add(homebuilder);

        loop.Economy.Data.ConsumerConfidence = 120m; // above baseline 90
        loop.Economy.Data.InterestRate = 8m;         // above baseline 3

        loop.DriftFundamentals();

        // High confidence pulls the retailer's growth up; high rates pull the homebuilder's down.
        Assert.True(consumer.RevenueGrowth > 0m, $"strong demand should lift growth, got {consumer.RevenueGrowth}");
        Assert.True(homebuilder.RevenueGrowth < 0m, $"high rates should sap growth, got {homebuilder.RevenueGrowth}");
    }

    [Fact]
    public void Valuation_HigherRates_CompressFairValue_NotEarnings()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        // Two identical tech companies (eps × sector PE == fair value, so the change stays unclamped);
        // only one is rate-sensitive on its valuation multiple.
        var control = MakeTech("CTRL");
        var rateSensitive = MakeTech("RATE");
        rateSensitive.DriverExposures.Add(new DriverExposure("InterestRate", ExposureChannel.Valuation, -0.35m));
        loop.MutableStocks.Add(control);
        loop.MutableStocks.Add(rateSensitive);

        loop.Economy.Data.InterestRate = 4.5m; // mildly above baseline 3

        var earningsBefore = rateSensitive.NetIncome;
        loop.RecalculateFairValues();

        // Higher rates compress the multiple → lower fair value, with earnings untouched.
        Assert.True(rateSensitive.FairValue < control.FairValue, $"rate-sensitive fair value {rateSensitive.FairValue} should sit below control {control.FairValue}");
        Assert.Equal(earningsBefore, rateSensitive.NetIncome);
    }

    private static Stock MakeCompany(string symbol, string sector) => new(symbol, symbol, sector)
    {
        CurrentPrice = 100m,
        PreviousClose = 100m,
        FairValue = 100m,
        SharesOutstanding = 100,
        Revenue = 1000m,
        NetIncome = 100m,
        RevenueGrowth = 0m,
    };

    // eps = 400/100 = 4; tech sector PE = 25 → PE fair value = 100 == FairValue (unclamped baseline).
    private static Stock MakeTech(string symbol) => new(symbol, symbol, "Technology")
    {
        CurrentPrice = 100m,
        PreviousClose = 100m,
        FairValue = 100m,
        SharesOutstanding = 100,
        Revenue = 1000m,
        NetIncome = 400m,
        RevenueGrowth = 0m,
    };
}
