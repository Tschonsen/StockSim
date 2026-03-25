namespace StockSim.Engine.Models;

/// <summary>
/// A market rumor — a vague hint about a future company event.
/// Bible 4.8: appears every 20-40 game days, 80% true / 20% false.
/// </summary>
public class Rumor
{
    private static long _nextId = 1;

    public long Id { get; set; }

    /// <summary>Stock symbol the rumor is about.</summary>
    public string Symbol { get; set; } = "";

    /// <summary>Company name for display.</summary>
    public string CompanyName { get; set; } = "";

    /// <summary>The vague hint headline shown to the player.</summary>
    public string Headline { get; set; } = "";

    /// <summary>Game time when the rumor appeared.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Game time when the real event should fire (if true rumor).</summary>
    public DateTime EventExpectedAt { get; set; }

    /// <summary>Whether this rumor will lead to a real event (80% true).</summary>
    public bool IsTrue { get; set; }

    /// <summary>Whether the linked event has already been triggered.</summary>
    public bool EventFired { get; set; }

    /// <summary>Index into the rumor template that was used (for generating the matching event).</summary>
    public int TemplateIndex { get; set; }

    /// <summary>Whether the price effect is positive (true) or negative (false).</summary>
    public bool IsPositive { get; set; }

    public Rumor()
    {
        Id = _nextId++;
    }

    public static void ResetIdCounter() => _nextId = 1;
    public static void SetIdCounter(long val) => _nextId = val;
}
