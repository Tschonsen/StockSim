namespace StockSim.Engine.Models;

/// <summary>
/// Gives each stock a unique identity: CEO, products, founding story, and rivalries.
/// Generated procedurally at game start for narrative depth in events and UI.
/// </summary>
public class CompanyPersonality
{
    /// <summary>CEO full name (e.g. "Sarah Chen").</summary>
    public string CEOName { get; set; } = "";

    /// <summary>CEO archetype that influences event tone (e.g. "Visionary", "Cost-Cutter").</summary>
    public string CEOArchetype { get; set; } = "";

    /// <summary>Year the company was founded (e.g. 1987, 2019).</summary>
    public int FoundedYear { get; set; }

    /// <summary>Company origin city (e.g. "San Francisco, CA").</summary>
    public string Headquarters { get; set; } = "";

    /// <summary>One-line company description / mission.</summary>
    public string Description { get; set; } = "";

    /// <summary>Flagship product or service name (e.g. "QuantumCore Platform").</summary>
    public string FlagshipProduct { get; set; } = "";

    /// <summary>Secondary product line (may be empty for smaller companies).</summary>
    public string SecondaryProduct { get; set; } = "";

    /// <summary>Symbol of a rival company in the same sector (empty if none).</summary>
    public string RivalSymbol { get; set; } = "";

    /// <summary>Short founding story snippet for UI display.</summary>
    public string FoundingStory { get; set; } = "";
}
