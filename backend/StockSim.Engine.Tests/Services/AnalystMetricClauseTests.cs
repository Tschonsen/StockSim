using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 5: analyst quotes lead with a clause grounded in the company's real metrics and the
/// event type, instead of generic directional chatter that fits any stock.
/// </summary>
public class AnalystMetricClauseTests
{
    [Fact]
    public void PositiveEarnings_CitesRevenueGrowth()
    {
        var clause = FundamentalDynamics.AnalystMetricClause(
            "Acme", positive: true, peRatio: 25m, revenueGrowthPct: 15m, marginPct: 8m, isEarnings: true);
        Assert.Contains("15", clause);
        Assert.Contains("revenue", clause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Positive_HighMargin_CitesMargin()
    {
        var clause = FundamentalDynamics.AnalystMetricClause(
            "Acme", positive: true, peRatio: 40m, revenueGrowthPct: 1m, marginPct: 20m, isEarnings: false);
        Assert.Contains("20", clause);
        Assert.Contains("margin", clause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Positive_ReasonableValuation_CitesPE()
    {
        var clause = FundamentalDynamics.AnalystMetricClause(
            "Acme", positive: true, peRatio: 18m, revenueGrowthPct: 1m, marginPct: 4m, isEarnings: false);
        Assert.Contains("18", clause);
    }

    [Fact]
    public void NegativeEarnings_CitesDecelerationWithNumber()
    {
        var clause = FundamentalDynamics.AnalystMetricClause(
            "Acme", positive: false, peRatio: 15m, revenueGrowthPct: -5m, marginPct: 10m, isEarnings: true);
        Assert.Contains("-5", clause);
    }

    [Fact]
    public void Negative_ExpensiveValuation_WarnsNoMarginOfSafety()
    {
        var clause = FundamentalDynamics.AnalystMetricClause(
            "Acme", positive: false, peRatio: 40m, revenueGrowthPct: 2m, marginPct: 10m, isEarnings: false);
        Assert.Contains("40", clause);
        Assert.Contains("margin of safety", clause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Negative_ThinMargins_CitesMargin()
    {
        var clause = FundamentalDynamics.AnalystMetricClause(
            "Acme", positive: false, peRatio: 15m, revenueGrowthPct: 2m, marginPct: 2m, isEarnings: false);
        Assert.Contains("2", clause);
        Assert.Contains("thin", clause, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoNotableMetrics_FallsBackButNamesTheCompany()
    {
        var clause = FundamentalDynamics.AnalystMetricClause(
            "Acme", positive: true, peRatio: 0m, revenueGrowthPct: 0m, marginPct: 5m, isEarnings: false);
        Assert.False(string.IsNullOrWhiteSpace(clause));
        Assert.Contains("Acme", clause);
    }
}
