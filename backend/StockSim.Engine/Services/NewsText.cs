using System.Text.RegularExpressions;

namespace StockSim.Engine.Services;

/// <summary>
/// Point 8: fix garbage in rendered news. Many of the ~250 template placeholders aren't
/// explicitly resolved and used to all fall back to the literal "the company" — which reads
/// fine in a name slot ("the company resigns") but broken in a number slot ("$the companyM",
/// "the company prior quarters"). This routes leftover placeholders by what their name implies.
/// </summary>
public static class NewsText
{
    private static readonly Regex Placeholder = new(@"\{([a-z_]+)\}", RegexOptions.Compiled);

    // Name fragments that mark a placeholder as a number rather than an entity/name.
    private static readonly Regex NumericName = new(
        "num|amount|pct|count|qty|shares|price|value|rate|growth|decline|cost|fee|eps|" +
        "revenue|margin|yield|ratio|paid|burn|cash|fcf|backlog|capacity|premium|savings|" +
        "stake|payout|threshold|days|months|quarters|years|hours|notches|size|points|score|" +
        "fine|damages|tam|arr|loss|gain|spread|change|impact|dilution|erosion|approval|efficacy|" +
        "salary|bonus|tranche|seats|jobs|employees|users|subscribers|deal|total|sum|" +
        "writedown|settlement|charge|proceeds|funding|valuation|dividend|debt|interest|" +
        "leverage|royalty|upfront|bid|offer|reserves|savings",
        RegexOptions.Compiled);

    // A formatted amount (e.g. "162M") directly followed by a redundant unit letter from the
    // template (e.g. "${amount}B" → "162MB").
    private static readonly Regex DoubleUnit = new(@"(\d[MBK])[MB]\b", RegexOptions.Compiled);

    /// <summary>True if a leftover placeholder name implies a numeric value.</summary>
    public static bool IsNumericPlaceholder(string name) => NumericName.IsMatch(name);

    /// <summary>Collapse a double unit suffix like "$162MB" → "$162M" or "$5.0BB" → "$5.0B".</summary>
    public static string FixDoubleUnits(string text) => DoubleUnit.Replace(text, "$1");

    /// <summary>
    /// Replace any unresolved <c>{placeholder}</c>: numeric-looking ones (by name, or by a $ before /
    /// % after) get <paramref name="number"/>, the rest get "the company".
    /// </summary>
    public static string FillLeftovers(string text, Func<string> number) =>
        Placeholder.Replace(text, m =>
        {
            int after = m.Index + m.Length;
            bool numeric = IsNumericPlaceholder(m.Groups[1].Value)
                || (m.Index > 0 && text[m.Index - 1] == '$')
                || (after < text.Length && text[after] == '%');
            if (numeric) return number();
            // Avoid "the the company" when the slot already follows an article.
            bool precededByThe = m.Index >= 4
                && text.Substring(m.Index - 4, 4).Equals("the ", StringComparison.OrdinalIgnoreCase);
            return precededByThe ? "company" : "the company";
        });
}
