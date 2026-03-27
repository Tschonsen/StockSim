namespace StockSim.Engine.Models;

/// <summary>
/// Per-tier settings for a given difficulty level.
/// Controls which event tiers are active, their frequency, and bias.
/// See AI_EVENT_SYSTEM.md section 1.2.
/// </summary>
public class TierSettings
{
    /// <summary>Which tier these settings apply to</summary>
    public EventTier Tier { get; set; }

    /// <summary>Whether this tier generates events at this difficulty</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Multiplier on base event frequency (1.0 = normal)</summary>
    public float FrequencyMultiplier { get; set; } = 1.0f;

    /// <summary>Fraction of events that are negative (0.5 = balanced, 0.7 = 70% negative)</summary>
    public float NegativeBias { get; set; } = 0.5f;

    /// <summary>Multiplier on follow-up cascade probability (1.0 = normal)</summary>
    public float CascadeProbability { get; set; } = 1.0f;

    /// <summary>Multiplier on recovery speed (1.0 = normal, 0.5 = half speed)</summary>
    public float RecoverySpeed { get; set; } = 1.0f;

    /// <summary>Max negative price effect (1.0 = no cap, 0.05 = -5% max)</summary>
    public float MaxNegativeEffect { get; set; } = 1.0f;
}

/// <summary>
/// Complete event tier configuration for a difficulty level.
/// Provides presets via ForDifficulty() factory method.
/// </summary>
public class EventTierConfig
{
    /// <summary>Difficulty name ("Easy", "Normal", "Hard", "Brutal")</summary>
    public string Difficulty { get; set; } = "Normal";

    /// <summary>Settings per tier</summary>
    public List<TierSettings> Tiers { get; set; } = new();

    /// <summary>
    /// Get the tier config for a specific difficulty level.
    /// </summary>
    public static EventTierConfig ForDifficulty(string difficulty)
    {
        return difficulty switch
        {
            "Easy" => new EventTierConfig
            {
                Difficulty = "Easy",
                Tiers = new List<TierSettings>
                {
                    new() { Tier = EventTier.Tier1, Enabled = true, FrequencyMultiplier = 1.0f, NegativeBias = 0.4f, MaxNegativeEffect = 0.05f, RecoverySpeed = 1.5f },
                    new() { Tier = EventTier.Tier2, Enabled = false },
                    new() { Tier = EventTier.Tier3, Enabled = false },
                    new() { Tier = EventTier.Tier4, Enabled = false },
                }
            },
            "Hard" => new EventTierConfig
            {
                Difficulty = "Hard",
                Tiers = new List<TierSettings>
                {
                    new() { Tier = EventTier.Tier1, Enabled = true, FrequencyMultiplier = 1.2f, NegativeBias = 0.6f },
                    new() { Tier = EventTier.Tier2, Enabled = true, FrequencyMultiplier = 1.2f, NegativeBias = 0.6f },
                    new() { Tier = EventTier.Tier3, Enabled = true, FrequencyMultiplier = 1.0f, NegativeBias = 0.6f, CascadeProbability = 1.2f, RecoverySpeed = 0.8f },
                    new() { Tier = EventTier.Tier4, Enabled = false },
                }
            },
            "Brutal" => new EventTierConfig
            {
                Difficulty = "Brutal",
                Tiers = new List<TierSettings>
                {
                    new() { Tier = EventTier.Tier1, Enabled = true, FrequencyMultiplier = 1.5f, NegativeBias = 0.7f },
                    new() { Tier = EventTier.Tier2, Enabled = true, FrequencyMultiplier = 1.5f, NegativeBias = 0.7f },
                    new() { Tier = EventTier.Tier3, Enabled = true, FrequencyMultiplier = 1.3f, NegativeBias = 0.7f, CascadeProbability = 1.5f, RecoverySpeed = 0.5f },
                    new() { Tier = EventTier.Tier4, Enabled = true, FrequencyMultiplier = 1.0f, NegativeBias = 0.7f, CascadeProbability = 1.5f, RecoverySpeed = 0.5f },
                }
            },
            // Normal is default
            _ => new EventTierConfig
            {
                Difficulty = "Normal",
                Tiers = new List<TierSettings>
                {
                    new() { Tier = EventTier.Tier1, Enabled = true, FrequencyMultiplier = 1.0f, NegativeBias = 0.5f },
                    new() { Tier = EventTier.Tier2, Enabled = true, FrequencyMultiplier = 1.0f, NegativeBias = 0.5f },
                    new() { Tier = EventTier.Tier3, Enabled = false },
                    new() { Tier = EventTier.Tier4, Enabled = false },
                }
            },
        };
    }

    /// <summary>
    /// Get settings for a specific tier. Returns null if tier is disabled.
    /// </summary>
    public TierSettings? GetTier(EventTier tier)
    {
        foreach (var settings in Tiers)
        {
            if (settings.Tier == tier && settings.Enabled)
                return settings;
        }
        return null;
    }
}
