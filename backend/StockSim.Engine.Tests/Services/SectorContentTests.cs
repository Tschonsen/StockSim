using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 6 (part 1): placeholder pools are sector-aware, so a healthcare company doesn't launch a
/// "blockchain solution" and a bank doesn't miss on "rising input costs".
/// </summary>
public class SectorContentTests
{
    [Fact]
    public void Reasons_AreSectorSpecific()
    {
        Assert.Contains(SectorContent.Reasons("Healthcare"), r => r.Contains("trial") || r.Contains("payer") || r.Contains("patent"));
        Assert.Contains(SectorContent.Reasons("Financials"), r => r.Contains("credit") || r.Contains("deposit") || r.Contains("interest margin"));
    }

    [Fact]
    public void Technologies_AreSectorSpecific_AndAvoidCrossSectorNonsense()
    {
        var healthcare = SectorContent.Technologies("Healthcare");
        Assert.Contains(healthcare, t => t.Contains("drug") || t.Contains("clinical") || t.Contains("therapy") || t.Contains("device"));
        Assert.DoesNotContain(healthcare, t => t.Contains("blockchain"));

        Assert.Contains(SectorContent.Technologies("Technology"), t => t.Contains("AI") || t.Contains("cloud"));
    }

    [Fact]
    public void EarningsMetrics_AreSectorSpecific()
    {
        Assert.Contains(SectorContent.EarningsMetrics("Financials"), m => m.Contains("interest margin") || m.Contains("loan") || m.Contains("trading"));
        Assert.Contains(SectorContent.EarningsMetrics("Healthcare"), m => m.Contains("pipeline") || m.Contains("drug") || m.Contains("trial"));
        Assert.Contains(SectorContent.EarningsMetrics("Consumer Goods"), m => m.Contains("comparable") || m.Contains("volume"));
    }

    [Fact]
    public void EarningsMetrics_UnknownSector_FallsBackToNeutral()
    {
        Assert.NotEmpty(SectorContent.EarningsMetrics("Nonexistent"));
        Assert.NotEmpty(SectorContent.EarningsMetrics(null));
    }

    [Fact]
    public void UnknownSector_FallsBackToNeutralPool()
    {
        var reasons = SectorContent.Reasons("Nonexistent");
        var tech = SectorContent.Technologies(null);
        Assert.NotEmpty(reasons);
        Assert.NotEmpty(tech);
        // Neutral fallback must not contain sector-specific nonsense.
        Assert.DoesNotContain(tech, t => t.Contains("blockchain"));
    }
}
