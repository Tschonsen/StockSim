using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 3 (FoundedYear): a company's age shapes its drift and volatility.
/// Young firms compound faster and swing harder; old institutions are stable.
/// </summary>
public class CompanyMaturityTests
{
    [Fact]
    public void YoungFirm_HasHigherDriftAndVolThanOldFirm()
    {
        var young = FundamentalDynamics.MaturityModifiers(2);
        var old = FundamentalDynamics.MaturityModifiers(80);
        Assert.True(young.DriftMultiplier > old.DriftMultiplier);
        Assert.True(young.VolMultiplier > old.VolMultiplier);
    }

    [Fact]
    public void YoungFirm_AmplifiesAboveOne_OldFirmDampensBelowOne()
    {
        Assert.True(FundamentalDynamics.MaturityModifiers(1).DriftMultiplier > 1m);
        Assert.True(FundamentalDynamics.MaturityModifiers(80).DriftMultiplier < 1m);
    }

    [Fact]
    public void Age_IsClampedAtBothEnds()
    {
        // Negative age behaves like a brand-new firm.
        Assert.Equal(FundamentalDynamics.MaturityModifiers(0), FundamentalDynamics.MaturityModifiers(-5));
        // Very old firms hit the stable floor (no further decay past the reference age).
        Assert.Equal(FundamentalDynamics.MaturityModifiers(60), FundamentalDynamics.MaturityModifiers(200));
    }

    [Fact]
    public void Modifiers_DecreaseMonotonicallyWithAge()
    {
        var a = FundamentalDynamics.MaturityModifiers(5);
        var b = FundamentalDynamics.MaturityModifiers(25);
        var c = FundamentalDynamics.MaturityModifiers(50);
        Assert.True(a.DriftMultiplier > b.DriftMultiplier && b.DriftMultiplier > c.DriftMultiplier);
        Assert.True(a.VolMultiplier > b.VolMultiplier && b.VolMultiplier > c.VolMultiplier);
    }
}
