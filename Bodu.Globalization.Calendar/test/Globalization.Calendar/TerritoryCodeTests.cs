// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the parsing, canonicalisation, containment, and list-splitting semantics of
/// <see cref="TerritoryCode" />.
/// </summary>
[TestClass]
public sealed class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that <see cref="TerritoryCode.TryParse(string?, out TerritoryCode)" /> returns
    /// <see langword="true" /> and normalises the result to upper case for every well-formed
    /// input — covering country-only, country-subdivision, mixed case, surrounding whitespace,
    /// and three-character subdivision codes (e.g. <c>AU-NSW</c>, <c>US-CA</c>).
    /// </summary>
    [DataRow("AU", "AU", null)]
    [DataRow("au", "AU", null)]
    [DataRow("  au  ", "AU", null)]
    [DataRow("AU-NSW", "AU", "NSW")]
    [DataRow("au-nsw", "AU", "NSW")]
    [DataRow("  au-nsw  ", "AU", "NSW")]
    [DataRow("US-CA", "US", "CA")]
    [DataRow("DE-1", "DE", "1")]
    [DataRow("FR-IDF", "FR", "IDF")]
    [TestMethod]
    public void TryParse_WhenInputIsValid_ShouldReturnCanonicalForm(string input, string expectedCountry, string? expectedSubdivision)
    {
        Assert.IsTrue(TerritoryCode.TryParse(input, out var result));
        Assert.AreEqual(expectedCountry, result.Country);
        Assert.AreEqual(expectedSubdivision, result.Subdivision);
        Assert.AreEqual(expectedSubdivision is not null, result.HasSubdivision);
    }

    /// <summary>
    /// Verifies that malformed inputs — wrong-length country codes, invalid characters,
    /// empty/whitespace/null inputs, and malformed subdivisions — all cause
    /// <see cref="TerritoryCode.TryParse(string?, out TerritoryCode)" /> to return
    /// <see langword="false" />.
    /// </summary>
    [DataRow("")]
    [DataRow(null!)]
    [DataRow("   ")]
    [DataRow("A")]
    [DataRow("AUS")]
    [DataRow("AU!")]
    [DataRow("A1")]
    [DataRow("12")]
    [DataRow("AU-")]
    [DataRow("AU-TOOLONG")]
    [DataRow("-NSW")]
    [DataRow("BAD!")]
    [DataRow("A-B-C")]
    [DataRow("AU-!")]
    [DataRow("AU-NS!")]
    [TestMethod]
    public void TryParse_WhenInputIsInvalid_ShouldReturnFalse(string? input)
    {
        Assert.IsFalse(TerritoryCode.TryParse(input, out _));
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.Parse(string)" /> returns the same canonical form
    /// as <see cref="TerritoryCode.TryParse(string?, out TerritoryCode)" /> for valid input.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsValid_ShouldReturnCanonicalForm()
    {
        var parsed = TerritoryCode.Parse("au-nsw");

        Assert.AreEqual("AU", parsed.Country);
        Assert.AreEqual("NSW", parsed.Subdivision);
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.Parse(string)" /> throws
    /// <see cref="FormatException" /> for invalid input, and that the raw input is surfaced in
    /// the message.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsInvalid_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() => TerritoryCode.Parse("BAD!"));

        Assert.IsTrue(ex.Message.Contains("BAD!", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies the full containment matrix for <see cref="TerritoryCode.Contains(TerritoryCode)" /> —
    /// a country contains itself and its subdivisions, a subdivision contains only itself, and
    /// unrelated territories never contain each other.
    /// </summary>
    [DataRow("AU", "AU", true)]
    [DataRow("AU", "AU-NSW", true)]
    [DataRow("AU", "AU-QLD", true)]
    [DataRow("AU-NSW", "AU-NSW", true)]
    [DataRow("AU-NSW", "AU", false)]
    [DataRow("AU-NSW", "AU-QLD", false)]
    [DataRow("AU", "NZ", false)]
    [DataRow("AU", "NZ-AUK", false)]
    [DataRow("AU-NSW", "NZ-AUK", false)]
    [DataRow("au", "AU-NSW", true)]
    [DataRow("AU-nsw", "au-NSW", true)]
    [TestMethod]
    public void Contains_TruthTable(string left, string right, bool expected)
    {
        var leftTerritory = TerritoryCode.Parse(left);
        var rightTerritory = TerritoryCode.Parse(right);

        Assert.AreEqual(expected, leftTerritory.Contains(rightTerritory));
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.ParseList(string?)" /> splits a comma-separated
    /// list, trims entries, drops invalid ones, and returns canonical forms.
    /// </summary>
    [TestMethod]
    public void ParseList_WhenMixedValidAndInvalid_ShouldReturnValidEntriesOnly()
    {
        var list = TerritoryCode.ParseList("AU, NZ, BAD!, US-CA");

        Assert.AreEqual(3, list.Count);
        Assert.AreEqual("AU", list[0].ToString());
        Assert.AreEqual("NZ", list[1].ToString());
        Assert.AreEqual("US-CA", list[2].ToString());
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.ParseList(string?)" /> returns an empty sequence
    /// for null, empty, or whitespace input.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow("   ")]
    [TestMethod]
    public void ParseList_WhenNullOrWhitespace_ShouldReturnEmpty(string? input)
    {
        IReadOnlyList<TerritoryCode> list = TerritoryCode.ParseList(input);

        Assert.AreEqual(0, list.Count);
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.ToString" /> round-trips canonical forms for both
    /// country-only and country-subdivision representations.
    /// </summary>
    [DataRow("AU", "AU")]
    [DataRow("au", "AU")]
    [DataRow("au-nsw", "AU-NSW")]
    [DataRow("AU-NSW", "AU-NSW")]
    [DataRow("us-ca", "US-CA")]
    [TestMethod]
    public void ToString_ShouldReturnCanonicalForm(string input, string expected)
    {
        var parsed = TerritoryCode.Parse(input);

        Assert.AreEqual(expected, parsed.ToString());
    }
}
