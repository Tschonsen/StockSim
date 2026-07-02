using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Emergent driver coupling (design/EMERGENT_COUPLING.md): a macro driver reaches fundamentals
/// through a channel. Pure, deterministic, calibrated. The marquee property: the SAME driver move
/// hits the InputCost channel (margin down) and the OutputPrice channel (revenue up) with opposite
/// sign — oil hurts airlines, helps oil producers.
/// </summary>
public class DriverCouplingTests
{
    // ---- InputCost channel (driver is a cost → margin) ----

    [Fact]
    public void InputCost_NoExposureOrNoChange_NoImpact()
    {
        Assert.Equal(0m, FundamentalDynamics.InputCostMarginLevel(0.50m, 0m));
        Assert.Equal(0m, FundamentalDynamics.InputCostMarginLevel(0m, 0.30m));
    }

    [Fact]
    public void InputCost_DriverSpike_CompressesMargin_RealisticBand()
    {
        var delta = FundamentalDynamics.InputCostMarginLevel(0.50m, 0.30m);
        Assert.True(delta < 0m, $"margin should compress, got {delta}");
        Assert.InRange(-delta, 0.05m, 0.10m); // several margin points, not annihilation
    }

    [Fact]
    public void InputCost_HigherExposure_HurtsMore()
    {
        var low = FundamentalDynamics.InputCostMarginLevel(0.50m, 0.15m);
        var high = FundamentalDynamics.InputCostMarginLevel(0.50m, 0.35m);
        Assert.True(high < low, $"higher exposure {high} should hurt more than {low}");
    }

    [Fact]
    public void InputCost_DriverDrop_ImprovesMargin()
    {
        Assert.True(FundamentalDynamics.InputCostMarginLevel(-0.30m, 0.30m) > 0m);
    }

    [Fact]
    public void InputCost_ExtremeShock_Clamped()
    {
        Assert.True(FundamentalDynamics.InputCostMarginLevel(5.0m, 0.35m) >= -0.25m);
    }

    // ---- OutputPrice channel (company sells the driver → margin) ----

    [Fact]
    public void OutputPrice_NoExposureOrNoDeviation_NoImpact()
    {
        Assert.Equal(0m, FundamentalDynamics.OutputPriceMarginLevel(0.50m, 0m));
        Assert.Equal(0m, FundamentalDynamics.OutputPriceMarginLevel(0m, 0.50m));
    }

    [Fact]
    public void OutputPrice_DriverHigh_LiftsMargin()
    {
        var delta = FundamentalDynamics.OutputPriceMarginLevel(0.50m, 0.50m);
        Assert.True(delta > 0m, $"producer margin should rise, got {delta}");
    }

    [Fact]
    public void OutputPrice_HigherExposure_HelpsMore()
    {
        var low = FundamentalDynamics.OutputPriceMarginLevel(0.50m, 0.30m);
        var high = FundamentalDynamics.OutputPriceMarginLevel(0.50m, 0.60m);
        Assert.True(high > low, $"more pure-play {high} should benefit more than {low}");
    }

    [Fact]
    public void OutputPrice_ExtremeShock_Clamped()
    {
        Assert.True(FundamentalDynamics.OutputPriceMarginLevel(5.0m, 0.60m) <= 0.25m);
    }

    // ---- the marquee: same driver, opposite sign across channels ----

    [Fact]
    public void SameDriverMove_HurtsInputCost_HelpsOutputPrice()
    {
        var oilUp = 0.50m;
        var airlineMargin = FundamentalDynamics.InputCostMarginLevel(oilUp, 0.30m);
        var producerRevenue = FundamentalDynamics.OutputPriceMarginLevel(oilUp, 0.50m);
        Assert.True(airlineMargin < 0m, "oil up should hurt the fuel buyer's margin");
        Assert.True(producerRevenue > 0m, "oil up should help the oil seller's revenue");
    }

    // ---- Demand channel (level-based → growth baseline; signed elasticity) ----

    [Fact]
    public void Demand_PositiveDriver_LiftsGrowthBaseline()
    {
        // High consumer confidence (deviation +0.3) with a positive elasticity sustains growth.
        Assert.True(FundamentalDynamics.DemandGrowthBaseline(0.30m, 0.4m) > 0m);
    }

    [Fact]
    public void Demand_SignedElasticity_InvertsEffect()
    {
        // Rates above baseline (deviation +1.0) with a NEGATIVE elasticity sap demand → negative baseline.
        Assert.True(FundamentalDynamics.DemandGrowthBaseline(1.0m, -0.8m) < 0m);
    }

    [Fact]
    public void Demand_NoDeviation_NoBaseline()
    {
        Assert.Equal(0m, FundamentalDynamics.DemandGrowthBaseline(0m, 0.5m));
    }

    [Fact]
    public void Demand_Clamped()
    {
        Assert.InRange(FundamentalDynamics.DemandGrowthBaseline(5.0m, 0.8m), 0m, 0.15m);
    }

    // ---- Valuation channel (level-based → PE multiple; signed elasticity) ----

    [Fact]
    public void Valuation_RatesUp_CompressMultiple()
    {
        // Rates above baseline (deviation +1.0) with a negative elasticity → PE multiple lower.
        Assert.True(FundamentalDynamics.ValuationMultipleFactor(1.0m, -0.35m) < 0m);
    }

    [Fact]
    public void Valuation_NoDeviation_NoEffect()
    {
        Assert.Equal(0m, FundamentalDynamics.ValuationMultipleFactor(0m, -0.35m));
    }

    [Fact]
    public void Valuation_Clamped()
    {
        Assert.True(FundamentalDynamics.ValuationMultipleFactor(5.0m, -0.35m) >= -0.30m);
    }
}
