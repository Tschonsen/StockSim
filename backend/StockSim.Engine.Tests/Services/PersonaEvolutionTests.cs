using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 3b: a company's CEO archetype drifts along a defensive↔growth spectrum after a
/// sustained good or bad run, instead of staying frozen until the CEO is fired.
/// </summary>
public class PersonaEvolutionTests
{
    [Fact]
    public void Streak_RisesOnStrength_FallsOnWeakness()
    {
        Assert.Equal(1, FundamentalDynamics.UpdateStreak(0, 0.8m));
        Assert.Equal(-1, FundamentalDynamics.UpdateStreak(0, -0.8m));
    }

    [Fact]
    public void Streak_DecaysTowardZero_OnNeutralHealth()
    {
        Assert.Equal(4, FundamentalDynamics.UpdateStreak(5, 0m));
        Assert.Equal(-2, FundamentalDynamics.UpdateStreak(-3, 0m));
        Assert.Equal(0, FundamentalDynamics.UpdateStreak(0, 0m));
    }

    [Fact]
    public void EvolutionDirection_TriggersOnlyAtSustainedThreshold()
    {
        Assert.Equal(1, FundamentalDynamics.PersonaEvolutionDirection(90));
        Assert.Equal(-1, FundamentalDynamics.PersonaEvolutionDirection(-90));
        Assert.Equal(0, FundamentalDynamics.PersonaEvolutionDirection(50));
        Assert.Equal(0, FundamentalDynamics.PersonaEvolutionDirection(-89));
    }

    [Fact]
    public void EvolveArchetype_MovesOneStepAlongSpectrum()
    {
        Assert.Equal("Finance Veteran", FundamentalDynamics.EvolveArchetype("Steady Hand", +1));
        Assert.Equal("Founder-CEO", FundamentalDynamics.EvolveArchetype("Visionary", -1));
    }

    [Fact]
    public void EvolveArchetype_ClampsAtSpectrumEnds()
    {
        Assert.Equal("Disruptor", FundamentalDynamics.EvolveArchetype("Disruptor", +1));
        Assert.Equal("Steady Hand", FundamentalDynamics.EvolveArchetype("Steady Hand", -1));
    }

    [Fact]
    public void EvolveArchetype_LeavesOffSpectrumArchetypeUnchanged()
    {
        // Turnaround Artist is handled by CEO firing, not gradual evolution.
        Assert.Equal("Turnaround Artist", FundamentalDynamics.EvolveArchetype("Turnaround Artist", +1));
    }
}
