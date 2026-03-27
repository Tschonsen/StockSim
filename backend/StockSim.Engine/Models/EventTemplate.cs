using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockSim.Engine.Models;

/// <summary>
/// A JSON-serializable event template loaded from data/events/.
/// Templates are blueprints — at runtime the engine picks a headline variant,
/// resolves placeholders ({company}, {ceo}, etc.), and constructs a GameEvent.
/// </summary>
public class EventTemplate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    /// <summary>Multiple headline variations — engine picks one at random</summary>
    [JsonPropertyName("headlines")]
    public List<string> Headlines { get; set; } = new();

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>Sentiment: -1.0 (very negative) to +1.0 (very positive)</summary>
    [JsonPropertyName("sentiment")]
    public float Sentiment { get; set; }

    /// <summary>Price effect range [min, max] as decimals (e.g. [0.03, 0.08] = 3-8%)</summary>
    [JsonPropertyName("priceEffect")]
    public float[] PriceEffect { get; set; } = new float[2];

    [JsonPropertyName("volatilityMultiplier")]
    public float VolatilityMultiplier { get; set; } = 1.0f;

    [JsonPropertyName("volumeMultiplier")]
    public float VolumeMultiplier { get; set; } = 1.0f;

    /// <summary>Duration range [min, max] in game-minutes</summary>
    [JsonPropertyName("durationMinutes")]
    public int[] DurationMinutes { get; set; } = new int[2];

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Minor";

    /// <summary>"Macro", "Sector", or "Company"</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Company";

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>Season filter: "Q1"-"Q4" or null</summary>
    [JsonPropertyName("season")]
    public string? Season { get; set; }

    /// <summary>Market phase filter: "bull", "bear", or null</summary>
    [JsonPropertyName("requiresPhase")]
    public string? RequiresPhase { get; set; }

    /// <summary>Market cap filter: "large", "small", or null</summary>
    [JsonPropertyName("requiresMarketCap")]
    public string? RequiresMarketCap { get; set; }

    /// <summary>Per-sector impact overrides (key = sector name)</summary>
    [JsonPropertyName("sectorImpacts")]
    public Dictionary<string, SectorImpactTemplate>? SectorImpacts { get; set; }

    /// <summary>Possible follow-up events</summary>
    [JsonPropertyName("followUps")]
    [JsonConverter(typeof(FollowUpListConverter))]
    public List<FollowUpTemplate>? FollowUps { get; set; }
}

/// <summary>
/// Converter that handles followUps being either objects or plain strings (ID references).
/// Strings are converted to FollowUpTemplate with just the Id set.
/// </summary>
public class FollowUpListConverter : JsonConverter<List<FollowUpTemplate>?>
{
    public override List<FollowUpTemplate>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.StartArray) return null;

        var list = new List<FollowUpTemplate>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;

            if (reader.TokenType == JsonTokenType.String)
            {
                // Plain string = just an ID reference
                list.Add(new FollowUpTemplate { Id = reader.GetString() ?? "" });
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                var follow = JsonSerializer.Deserialize<FollowUpTemplate>(ref reader, options);
                if (follow != null) list.Add(follow);
            }
        }
        return list.Count > 0 ? list : null;
    }

    public override void Write(Utf8JsonWriter writer, List<FollowUpTemplate>? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// JSON-serializable sector impact within a template.
/// </summary>
public class SectorImpactTemplate
{
    [JsonPropertyName("priceEffect")]
    public float PriceEffect { get; set; }

    [JsonPropertyName("volatilityMultiplier")]
    public float VolatilityMultiplier { get; set; } = 1.0f;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// JSON-serializable follow-up scenario within a template.
/// </summary>
public class FollowUpTemplate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("headline")]
    public string Headline { get; set; } = "";

    [JsonPropertyName("probability")]
    public float Probability { get; set; } = 0.5f;

    [JsonPropertyName("delayMinDays")]
    public int DelayMinDays { get; set; } = 1;

    [JsonPropertyName("delayMaxDays")]
    public int DelayMaxDays { get; set; } = 5;

    [JsonPropertyName("priceEffect")]
    public float PriceEffect { get; set; }

    [JsonPropertyName("sentiment")]
    public float Sentiment { get; set; }
}

/// <summary>
/// JSON-serializable analyst profile loaded from data/analysts/.
/// </summary>
public class AnalystProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("firm")]
    public string Firm { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("specialty")]
    public string Specialty { get; set; } = "";
}
