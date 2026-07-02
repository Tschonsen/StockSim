using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Point 8: leftover placeholders are routed by name — numbers into number slots, "the company"
/// only into name slots — so the news stops printing "$the companyM".
/// </summary>
public class NewsTextTests
{
    [Theory]
    [InlineData("num_quarters")]
    [InlineData("damages")]
    [InlineData("restate_amount")]
    [InlineData("miss_pct")]
    [InlineData("years")]
    [InlineData("writedown")]
    public void NumericPlaceholders_AreDetected(string name)
        => Assert.True(NewsText.IsNumericPlaceholder(name));

    [Theory]
    [InlineData("executive")]
    [InlineData("competitor")]
    [InlineData("auditor")]
    [InlineData("plaintiff")]
    public void EntityPlaceholders_AreNotNumeric(string name)
        => Assert.False(NewsText.IsNumericPlaceholder(name));

    [Fact]
    public void FillLeftovers_PutsNumbersInNumberSlots_AndCompanyInNameSlots()
    {
        var result = NewsText.FillLeftovers(
            "ordered to pay ${damages}M to {competitor} over {num_quarters} quarters", () => "7");
        Assert.Equal("ordered to pay $7M to the company over 7 quarters", result);
        Assert.DoesNotContain("$the company", result);
    }

    [Fact]
    public void FillLeftovers_AvoidsDoubleArticle()
    {
        var result = NewsText.FillLeftovers("auditors at the {auditor} flagged issues", () => "9");
        Assert.DoesNotContain("the the", result);
        Assert.Contains("the company", result);
    }

    [Theory]
    [InlineData("buyback of $162MB announced", "buyback of $162M announced")]
    [InlineData("a $5.0BB deal", "a $5.0B deal")]
    [InlineData("a clean $7B contract", "a clean $7B contract")]
    public void FixDoubleUnits_CollapsesRedundantSuffix(string input, string expected)
        => Assert.Equal(expected, NewsText.FixDoubleUnits(input));

    [Fact]
    public void FillLeftovers_LeavesResolvedTextUntouched()
    {
        var s = "Acme beats estimates, shares surge";
        Assert.Equal(s, NewsText.FillLeftovers(s, () => "9"));
    }
}
