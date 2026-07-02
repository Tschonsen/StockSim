using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 4: news text reflects the company's actual trajectory (turnaround, miss streak,
/// momentum) instead of just swapping in names.
/// </summary>
public class TrajectoryPhraseTests
{
    [Fact]
    public void NeutralCompany_ProducesNoClause()
    {
        Assert.Equal(string.Empty, FundamentalDynamics.TrajectoryPhrase(0m, 0, 0, 0m));
    }

    [Fact]
    public void SustainedStrongRun_ReadsAsMomentum()
    {
        var phrase = FundamentalDynamics.TrajectoryPhrase(0.05m, performanceStreak: 50, consecutiveMisses: 0, recentReturn: 0.05m);
        Assert.Contains("strong run", phrase);
    }

    [Fact]
    public void HighGrowth_ReadsAsMomentum()
    {
        var phrase = FundamentalDynamics.TrajectoryPhrase(0.20m, 0, 0, 0m);
        Assert.Contains("momentum", phrase);
    }

    [Fact]
    public void MissStreak_ReadsAsEarningsTrouble()
    {
        var phrase = FundamentalDynamics.TrajectoryPhrase(0m, 0, consecutiveMisses: 2, recentReturn: 0m);
        Assert.Contains("earnings", phrase);
    }

    [Fact]
    public void ProlongedWeakRun_ReadsAsRoughStretch()
    {
        var phrase = FundamentalDynamics.TrajectoryPhrase(0m, performanceStreak: -50, consecutiveMisses: 0, recentReturn: 0m);
        Assert.Contains("rough stretch", phrase);
    }

    [Fact]
    public void NegativeSignalsWinOverPositive()
    {
        // A company both on a long win-streak AND missing earnings reads as the trouble — distress dominates the story.
        var phrase = FundamentalDynamics.TrajectoryPhrase(0.20m, performanceStreak: 50, consecutiveMisses: 3, recentReturn: 0.20m);
        Assert.Contains("earnings", phrase);
    }
}
