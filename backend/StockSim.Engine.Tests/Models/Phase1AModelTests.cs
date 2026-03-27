using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

public class Phase1AModelTests
{
    // ========================
    // GameEvent Backward Compatibility
    // ========================

    [Fact]
    public void GameEvent_OldStyleCreation_ShouldHaveNullDefaults()
    {
        var evt = new GameEvent
        {
            Type = EventType.Company,
            Severity = EventSeverity.Major,
            Sentiment = -0.5f,
            Headline = "Test headline",
            PriceEffect = -0.05f,
            DurationMinutes = 60,
            RemainingMinutes = 60,
            TriggeredAt = DateTime.Now,
        };

        // All new fields should be null or default
        Assert.Null(evt.Summary);
        Assert.Null(evt.AnalystQuote);
        Assert.Null(evt.AnalystName);
        Assert.Null(evt.AnalystFirm);
        Assert.Null(evt.HistoricalParallel);
        Assert.Null(evt.WhatToWatch);
        Assert.Equal(EventTier.Tier1, evt.Tier);
        Assert.Null(evt.Tags);
        Assert.Null(evt.Season);
        Assert.Null(evt.RequiresPhase);
        Assert.Null(evt.RequiresMarketCap);
        Assert.Null(evt.SectorImpacts);
        Assert.Null(evt.DetailedImpacts);
        Assert.Null(evt.PossibleOutcomes);
        Assert.Null(evt.ArcId);
        Assert.Null(evt.ArcPhaseIndex);
        Assert.Null(evt.ArcPath);

        // Original fields still work
        Assert.Equal(EventType.Company, evt.Type);
        Assert.Equal(-0.05f, evt.PriceEffect);
        Assert.Equal("Test headline", evt.Headline);
    }

    [Fact]
    public void GameEvent_WithNewFields_ShouldRoundTrip()
    {
        var evt = new GameEvent
        {
            Type = EventType.Macro,
            Severity = EventSeverity.Major,
            Headline = "Fed raises rates",
            PriceEffect = -0.02f,
            Tier = EventTier.Tier2,
            Summary = "The Federal Reserve raised interest rates by 25 basis points.",
            AnalystQuote = "This was expected but the tone was hawkish.",
            AnalystName = "Sarah Chen",
            AnalystFirm = "Atlantic Research",
            HistoricalParallel = "Similar to the 2004-2006 tightening cycle.",
            WhatToWatch = new List<string> { "10Y yield reaction", "Bank stocks" },
            Tags = new List<string> { "rates", "fed", "macro" },
            Season = "Q1",
            RequiresPhase = "bull",
            ArcId = "rate_hike_cycle",
            ArcPhaseIndex = 2,
            ArcPath = "A",
            SectorImpacts = new Dictionary<string, SectorImpact>
            {
                ["Financials"] = new SectorImpact { Sector = "Financials", PriceEffect = 0.02f, Reason = "Higher margins" },
                ["Technology"] = new SectorImpact { Sector = "Technology", PriceEffect = -0.03f, Reason = "Higher discount rates" },
            },
            DetailedImpacts = new List<AffectedCompany>
            {
                new() { Symbol = "BNKX", PriceEffect = 0.03f, Role = "beneficiary", Reason = "Largest bank exposure" },
            },
            PossibleOutcomes = new List<FollowUpScenario>
            {
                new() { Id = "market_selloff", Headline = "Markets sell off on rate fears", Probability = 0.4f },
            },
        };

        Assert.Equal(EventTier.Tier2, evt.Tier);
        Assert.Equal("Sarah Chen", evt.AnalystName);
        Assert.Equal(2, evt.SectorImpacts.Count);
        Assert.Equal(0.02f, evt.SectorImpacts["Financials"].PriceEffect);
        Assert.Single(evt.DetailedImpacts);
        Assert.Equal("beneficiary", evt.DetailedImpacts[0].Role);
        Assert.Single(evt.PossibleOutcomes);
        Assert.Equal(2, evt.ArcPhaseIndex);
    }

    // ========================
    // EventTier Enum
    // ========================

    [Fact]
    public void EventTier_ShouldHaveCorrectValues()
    {
        Assert.Equal(1, (int)EventTier.Tier1);
        Assert.Equal(2, (int)EventTier.Tier2);
        Assert.Equal(3, (int)EventTier.Tier3);
        Assert.Equal(4, (int)EventTier.Tier4);
    }

    // ========================
    // EventTierConfig
    // ========================

    [Theory]
    [InlineData("Easy")]
    [InlineData("Normal")]
    [InlineData("Hard")]
    [InlineData("Brutal")]
    public void EventTierConfig_ForDifficulty_ShouldReturnConfig(string difficulty)
    {
        var config = EventTierConfig.ForDifficulty(difficulty);

        Assert.Equal(difficulty, config.Difficulty);
        Assert.Equal(4, config.Tiers.Count);
    }

    [Fact]
    public void EventTierConfig_Easy_ShouldOnlyEnableTier1()
    {
        var config = EventTierConfig.ForDifficulty("Easy");

        Assert.NotNull(config.GetTier(EventTier.Tier1));
        Assert.Null(config.GetTier(EventTier.Tier2));
        Assert.Null(config.GetTier(EventTier.Tier3));
        Assert.Null(config.GetTier(EventTier.Tier4));
    }

    [Fact]
    public void EventTierConfig_Normal_ShouldEnableTier1And2()
    {
        var config = EventTierConfig.ForDifficulty("Normal");

        Assert.NotNull(config.GetTier(EventTier.Tier1));
        Assert.NotNull(config.GetTier(EventTier.Tier2));
        Assert.Null(config.GetTier(EventTier.Tier3));
        Assert.Null(config.GetTier(EventTier.Tier4));
    }

    [Fact]
    public void EventTierConfig_Hard_ShouldEnableTier1To3()
    {
        var config = EventTierConfig.ForDifficulty("Hard");

        Assert.NotNull(config.GetTier(EventTier.Tier1));
        Assert.NotNull(config.GetTier(EventTier.Tier2));
        Assert.NotNull(config.GetTier(EventTier.Tier3));
        Assert.Null(config.GetTier(EventTier.Tier4));
    }

    [Fact]
    public void EventTierConfig_Brutal_ShouldEnableAllTiers()
    {
        var config = EventTierConfig.ForDifficulty("Brutal");

        Assert.NotNull(config.GetTier(EventTier.Tier1));
        Assert.NotNull(config.GetTier(EventTier.Tier2));
        Assert.NotNull(config.GetTier(EventTier.Tier3));
        Assert.NotNull(config.GetTier(EventTier.Tier4));
    }

    [Fact]
    public void EventTierConfig_Brutal_ShouldHaveHigherNegativeBias()
    {
        var brutal = EventTierConfig.ForDifficulty("Brutal");
        var normal = EventTierConfig.ForDifficulty("Normal");

        Assert.True(brutal.GetTier(EventTier.Tier1)!.NegativeBias > normal.GetTier(EventTier.Tier1)!.NegativeBias);
    }

    [Fact]
    public void EventTierConfig_Easy_ShouldCapNegativeEffect()
    {
        var easy = EventTierConfig.ForDifficulty("Easy");
        var tier1 = easy.GetTier(EventTier.Tier1)!;

        Assert.Equal(0.05f, tier1.MaxNegativeEffect);
        Assert.Equal(1.5f, tier1.RecoverySpeed);
    }

    [Fact]
    public void EventTierConfig_Unknown_ShouldDefaultToNormal()
    {
        var config = EventTierConfig.ForDifficulty("Unknown");

        Assert.Equal("Normal", config.Difficulty);
        Assert.NotNull(config.GetTier(EventTier.Tier1));
        Assert.NotNull(config.GetTier(EventTier.Tier2));
    }

    // ========================
    // SectorImpact Defaults
    // ========================

    [Fact]
    public void SectorImpact_Defaults_ShouldBeNeutral()
    {
        var impact = new SectorImpact();

        Assert.Equal("", impact.Sector);
        Assert.Equal(0f, impact.PriceEffect);
        Assert.Equal(1.0f, impact.VolatilityMultiplier);
        Assert.Equal(1.0f, impact.VolumeMultiplier);
        Assert.Null(impact.Reason);
    }

    [Fact]
    public void FollowUpScenario_Defaults_ShouldBeSensible()
    {
        var follow = new FollowUpScenario();

        Assert.Equal(0.5f, follow.Probability);
        Assert.Equal(1, follow.DelayMinDays);
        Assert.Equal(5, follow.DelayMaxDays);
        Assert.Equal(EventSeverity.Minor, follow.Severity);
        Assert.Null(follow.RequiresMarketPhase);
    }

    // ========================
    // EventArc
    // ========================

    [Fact]
    public void EventArc_ShouldStartNotStarted()
    {
        var arc = new EventArc { Id = "test_arc", Name = "Test Arc" };

        Assert.Equal(ArcStatus.NotStarted, arc.Status);
        Assert.Equal(0, arc.CurrentPhaseIndex);
        Assert.Null(arc.CurrentPath);
        Assert.Null(arc.StartedAt);
    }

    [Fact]
    public void EventArc_StatusLifecycle_ShouldProgress()
    {
        var arc = new EventArc { Id = "crisis", Name = "Financial Crisis" };

        Assert.Equal(ArcStatus.NotStarted, arc.Status);

        arc.Status = ArcStatus.Active;
        arc.StartedAt = DateTime.Now;
        Assert.Equal(ArcStatus.Active, arc.Status);

        arc.CurrentPhaseIndex = 2;
        arc.CurrentPath = "B";
        Assert.Equal("B", arc.CurrentPath);

        arc.Status = ArcStatus.Completed;
        Assert.Equal(ArcStatus.Completed, arc.Status);
    }

    [Fact]
    public void ArcPath_Weights_ShouldBeConfigurable()
    {
        var branch = new ArcBranch
        {
            AfterPhaseIndex = 1,
            Paths = new List<ArcPath>
            {
                new() { PathId = "A", Name = "Squeeze", BaseWeight = 2.0f, FavoredInPhase = "bull" },
                new() { PathId = "B", Name = "Fizzle", BaseWeight = 1.0f },
                new() { PathId = "C", Name = "Trap", BaseWeight = 0.5f, FavoredInPhase = "bear" },
            },
        };

        Assert.Equal(3, branch.Paths.Count);
        Assert.Equal(2.0f, branch.Paths[0].BaseWeight);
        Assert.Equal("bull", branch.Paths[0].FavoredInPhase);
        Assert.Null(branch.Paths[1].FavoredInPhase);
    }
}
