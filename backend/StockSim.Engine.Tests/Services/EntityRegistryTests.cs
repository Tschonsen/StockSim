using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 7: named entities (activists, executives, investors) stick to a company so news has
/// continuity — the activist hounding a stock stays the same across its events.
/// </summary>
public class EntityRegistryTests
{
    [Fact]
    public void FirstAssignment_BindsTheCandidate()
    {
        var reg = new EntityRegistry();
        Assert.Equal("Elliott Management", reg.GetOrAssign("VRTX", "activist", "Elliott Management"));
    }

    [Fact]
    public void SubsequentCalls_ReuseTheSameEntity_IgnoringNewCandidate()
    {
        var reg = new EntityRegistry();
        reg.GetOrAssign("VRTX", "activist", "Elliott Management");
        Assert.Equal("Elliott Management", reg.GetOrAssign("VRTX", "activist", "Starboard Value"));
    }

    [Fact]
    public void DifferentCompany_GetsItsOwnEntity()
    {
        var reg = new EntityRegistry();
        reg.GetOrAssign("VRTX", "activist", "Elliott Management");
        Assert.Equal("Starboard Value", reg.GetOrAssign("ACME", "activist", "Starboard Value"));
    }

    [Fact]
    public void DifferentRole_IsTrackedIndependently()
    {
        var reg = new EntityRegistry();
        reg.GetOrAssign("VRTX", "activist", "Elliott Management");
        Assert.Equal("BlackRock", reg.GetOrAssign("VRTX", "investor", "BlackRock"));
    }
}
