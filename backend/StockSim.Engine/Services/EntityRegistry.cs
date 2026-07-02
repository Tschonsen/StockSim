namespace StockSim.Engine.Services;

/// <summary>
/// Point 7: keeps named entities (activists, executives, investors) consistent per company across
/// events. The first time a role is needed for a company a candidate is bound; later events reuse
/// it, so the news reads as a continuing story rather than a fresh random cast each time.
/// </summary>
public class EntityRegistry
{
    private readonly Dictionary<string, string> _assigned = new();

    /// <summary>Return the entity already bound to (symbol, role), or bind <paramref name="candidate"/> and return it.</summary>
    public string GetOrAssign(string symbol, string role, string candidate)
    {
        var key = $"{symbol}|{role}";
        if (_assigned.TryGetValue(key, out var existing)) return existing;
        _assigned[key] = candidate;
        return candidate;
    }
}
