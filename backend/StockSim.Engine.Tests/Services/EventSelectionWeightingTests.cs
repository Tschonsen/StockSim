using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 2: which company makes the news is weighted by its health, not uniform random.
/// Troubled and extreme companies are more newsworthy than steady neutral ones.
/// </summary>
public class EventSelectionWeightingTests
{
    [Fact]
    public void HealthScore_StrongCompany_IsPositive()
    {
        // High margin, strong growth, low debt, AA, good momentum.
        var score = FundamentalDynamics.CompanyHealthScore(
            revenueGrowth: 0.20m, netIncome: 250m, revenue: 1000m,
            debtToEquity: 0.3m, creditRating: "AA", recentReturn: 0.10m);
        Assert.True(score > 0, $"score {score} should be positive");
    }

    [Fact]
    public void HealthScore_DistressedCompany_IsNegative()
    {
        // Losing money, declining, over-leveraged, junk-rated, falling.
        var score = FundamentalDynamics.CompanyHealthScore(
            revenueGrowth: -0.20m, netIncome: -150m, revenue: 1000m,
            debtToEquity: 4.0m, creditRating: "B", recentReturn: -0.15m);
        Assert.True(score < 0, $"score {score} should be negative");
    }

    [Fact]
    public void HealthScore_StaysWithinRange()
    {
        var extreme = FundamentalDynamics.CompanyHealthScore(
            revenueGrowth: 5m, netIncome: 9999m, revenue: 1m,
            debtToEquity: -10m, creditRating: "AAA", recentReturn: 5m);
        Assert.InRange(extreme, -1m, 1m);
    }

    [Fact]
    public void NewsWeight_TroubledMoreNewsworthyThanHealthy_BothAboveNeutral()
    {
        var troubled = FundamentalDynamics.NewsWeight(-0.8m);
        var healthy = FundamentalDynamics.NewsWeight(0.8m);
        var neutral = FundamentalDynamics.NewsWeight(0m);
        Assert.True(troubled > healthy, "distress is the most newsworthy");
        Assert.True(healthy > neutral, "strong results also make news");
    }

    [Fact]
    public void NewsWeight_NeutralIsBaseline()
    {
        Assert.Equal(1.0, FundamentalDynamics.NewsWeight(0m), precision: 6);
    }

    [Fact]
    public void WeightedPick_FavorsHeavierWeight()
    {
        var weights = new[] { 1.0, 3.0 }; // index 1 is 3x as likely
        // roll in the lower quarter → index 0; roll past the first weight → index 1
        Assert.Equal(0, FundamentalDynamics.WeightedPick(weights, 0.1));
        Assert.Equal(1, FundamentalDynamics.WeightedPick(weights, 0.5));
    }

    [Fact]
    public void WeightedPick_AllZero_FallsBackToFirst()
    {
        Assert.Equal(0, FundamentalDynamics.WeightedPick(new[] { 0.0, 0.0 }, 0.5));
    }
}
