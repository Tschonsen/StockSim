namespace StockSim.Engine.Models;

/// <summary>
/// Status of an event arc in the current game.
/// </summary>
public enum ArcStatus
{
    NotStarted,
    Active,
    Completed,
    Abandoned,  // Superseded by another arc or conditions changed
}

/// <summary>
/// A multi-phase event storyline that unfolds over days or weeks.
/// Tier 3 arcs: ~2-3 phases with 2 paths.
/// Tier 4 arcs: ~3-5 phases with 2-4 paths.
/// See AI_EVENT_SYSTEM.md section 1.3.
/// </summary>
public class EventArc
{
    // --- Template definition ---

    /// <summary>Unique arc identifier (e.g. "meme_stock_squeeze")</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g. "Meme Stock Squeeze")</summary>
    public string Name { get; set; } = "";

    /// <summary>Description of the arc storyline</summary>
    public string Description { get; set; } = "";

    /// <summary>Which tier this arc belongs to</summary>
    public EventTier Tier { get; set; } = EventTier.Tier3;

    /// <summary>Tags for categorization (e.g. "financial", "geopolitics")</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>All phases in this arc (ordered by Index)</summary>
    public List<ArcPhase> Phases { get; set; } = new();

    /// <summary>Branch points where the story can diverge</summary>
    public List<ArcBranch> Branches { get; set; } = new();

    // --- Activation conditions ---

    /// <summary>Required market phase ("bear", "bull", null = any)</summary>
    public string? RequiresPhase { get; set; }

    /// <summary>Required season ("Q1", "Q4", null = any)</summary>
    public string? RequiresSeason { get; set; }

    /// <summary>Minimum days since last arc of same tier completed</summary>
    public float MinDaysSinceLastArc { get; set; } = 30;

    // --- Runtime state (set when arc is active in a game) ---

    /// <summary>Current arc status</summary>
    public ArcStatus Status { get; set; } = ArcStatus.NotStarted;

    /// <summary>Index of the current phase (0-based)</summary>
    public int CurrentPhaseIndex { get; set; }

    /// <summary>Selected path after branch point (null until branched)</summary>
    public string? CurrentPath { get; set; }

    /// <summary>Game time when this arc was activated</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Stock symbol this arc is affecting (resolved at activation)</summary>
    public string? AffectedSymbol { get; set; }

    /// <summary>Sector this arc is affecting (resolved at activation)</summary>
    public string? AffectedSector { get; set; }
}

/// <summary>
/// A single phase within an arc (e.g. "Setup", "Escalation", "Resolution").
/// </summary>
public class ArcPhase
{
    /// <summary>0-based phase order</summary>
    public int Index { get; set; }

    /// <summary>Phase name (e.g. "Setup", "Escalation")</summary>
    public string Name { get; set; } = "";

    /// <summary>null for pre-branch phases, "A"/"B"/"C" for path-specific phases</summary>
    public string? Path { get; set; }

    /// <summary>Reference to event template ID</summary>
    public string EventTemplateId { get; set; } = "";

    /// <summary>Minimum days after previous phase</summary>
    public int DelayMinDays { get; set; }

    /// <summary>Maximum days after previous phase</summary>
    public int DelayMaxDays { get; set; }

    /// <summary>Price effect for this phase</summary>
    public float PriceEffect { get; set; }

    /// <summary>Volatility multiplier for this phase</summary>
    public float VolatilityMultiplier { get; set; } = 1.0f;

    /// <summary>Severity of events in this phase</summary>
    public EventSeverity Severity { get; set; } = EventSeverity.Moderate;
}

/// <summary>
/// A branch point in an arc where the story can diverge into multiple paths.
/// </summary>
public class ArcBranch
{
    /// <summary>Branch occurs after this phase index</summary>
    public int AfterPhaseIndex { get; set; }

    /// <summary>Available paths to branch into</summary>
    public List<ArcPath> Paths { get; set; } = new();
}

/// <summary>
/// One possible path after a branch point, with dynamic probability weights.
/// </summary>
public class ArcPath
{
    /// <summary>Path identifier ("A", "B", "C")</summary>
    public string PathId { get; set; } = "";

    /// <summary>Human-readable name (e.g. "Squeeze", "Fizzle", "Trap")</summary>
    public string Name { get; set; } = "";

    /// <summary>Base probability weight (relative to other paths)</summary>
    public float BaseWeight { get; set; } = 1.0f;

    /// <summary>Market phase that favors this path ("bull", "bear", null)</summary>
    public string? FavoredInPhase { get; set; }

    /// <summary>How much market phase shifts the weight (0.0-1.0)</summary>
    public float PhaseBias { get; set; } = 0.3f;
}
