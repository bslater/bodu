// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies <see cref="TerritoryCode" /> parsing, normalization, decomposition, containment, equality, conversions, and
/// interoperation with the string-based resolution surface.
/// </summary>
[TestClass]
public sealed class TerritoryCodeTests
{
    /// <summary>
    /// Verifies that a bare country parses to a country-only code.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCountryOnly_ShouldDecomposeToCountry()
    {
        var code = TerritoryCode.Parse("AU");

        Assert.AreEqual(
            ("AU", (string?)null, false, false),
            (code.Country, code.Subdivision, code.IsSubdivision, code.IsEmpty));
    }

    /// <summary>
    /// Verifies that a subdivision code decomposes into its country and subdivision parts.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSubdivision_ShouldDecomposeToCountryAndSubdivision()
    {
        var code = TerritoryCode.Parse("AU-NSW");

        Assert.AreEqual(
            ("AU", (string?)"NSW", true, "AU"),
            (code.Country, code.Subdivision, code.IsSubdivision, code.Parent.ToString()));
    }

    /// <summary>
    /// Verifies that parsing normalizes the country and subdivision to upper case.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLowerCase_ShouldNormalizeToUpper()
    {
        var code = TerritoryCode.Parse("au-nsw");

        Assert.AreEqual("AU-NSW", code.ToString());
    }

    /// <summary>
    /// Verifies that malformed codes throw <see cref="FormatException" /> naming the offending value.
    /// </summary>
    [DataRow("A")]
    [DataRow("AUS")]
    [DataRow("A1")]
    [DataRow("AU-")]
    [DataRow("AU-NSWX")]
    [DataRow("AU-NSW-X")]
    [DataRow("-NSW")]
    [TestMethod]
    public void Parse_WhenMalformed_ShouldThrowFormatException(string value)
    {
        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = TerritoryCode.Parse(value);
        });

        Assert.IsTrue(ex.Message.Contains(value, StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that parsing a <see langword="null" /> code throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = TerritoryCode.Parse(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.TryParse" /> reports failure and yields the default for invalid input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInvalid_ShouldReturnFalseAndDefault()
    {
        bool parsed = TerritoryCode.TryParse("nope!", out TerritoryCode result);

        Assert.AreEqual((false, true), (parsed, result.IsEmpty));
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.TryParse" /> reports failure for <see langword="null" /> and empty input.
    /// </summary>
    /// <param name="value">The null or empty input under test.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void TryParse_WhenNullOrEmpty_ShouldReturnFalse(string? value)
    {
        Assert.IsFalse(TerritoryCode.TryParse(value, out _));
    }

    /// <summary>
    /// Verifies that a country contains itself and its subdivisions, but a subdivision contains neither its parent nor a
    /// sibling, unrelated countries never contain each other, and the empty code neither contains nor is contained by any
    /// code. A <see langword="null" /> argument denotes the empty (default) <see cref="TerritoryCode" />.
    /// </summary>
    /// <param name="container">The containing code, or <see langword="null" /> for the default code.</param>
    /// <param name="contained">The candidate contained code, or <see langword="null" /> for the default code.</param>
    /// <param name="expected">The expected containment result.</param>
    [TestMethod]
    [DataRow("AU", "AU", true)]           // a country contains itself
    [DataRow("AU", "AU-NSW", true)]       // a country contains its subdivision
    [DataRow("AU-NSW", "AU-NSW", true)]   // a subdivision contains itself
    [DataRow("AU-NSW", "AU", false)]      // a subdivision does not contain its parent
    [DataRow("AU-NSW", "AU-VIC", false)]  // siblings do not contain each other
    [DataRow("AU", "US", false)]          // unrelated countries
    [DataRow(null, "AU", false)]          // the default code contains nothing
    [DataRow("AU", null, false)]          // nothing contains the default code
    public void Contains_WhenCheckedAgainstParentChild_ShouldFollowSemantics(string? container, string? contained, bool expected)
    {
        TerritoryCode containerCode = container is null ? default : TerritoryCode.Parse(container);
        TerritoryCode containedCode = contained is null ? default : TerritoryCode.Parse(contained);

        Assert.AreEqual(expected, containerCode.Contains(containedCode));
    }

    /// <summary>
    /// Verifies that two codes differing only in case are equal after normalization, are not unequal, and share a hash
    /// code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameCodeDifferentCase_ShouldBeEqual()
    {
        var a = TerritoryCode.Parse("au-nsw");
        var b = TerritoryCode.Parse("AU-NSW");

        Assert.AreEqual(
            (true, false, true),
            (a == b, a != b, a.GetHashCode() == b.GetHashCode()));
    }

    /// <summary>
    /// Verifies that two codes differing in subdivision are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDifferentSubdivision_ShouldNotBeEqual()
    {
        Assert.AreNotEqual(TerritoryCode.Parse("AU-NSW"), TerritoryCode.Parse("AU-VIC"));
    }

    /// <summary>
    /// Verifies that the default value renders as the empty string and is empty.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldBeEmpty()
    {
        TerritoryCode code = default;

        Assert.AreEqual((string.Empty, true), (code.ToString(), code.IsEmpty));
    }

    /// <summary>
    /// Verifies that the explicit string conversion parses and the implicit conversion yields the canonical form.
    /// </summary>
    [TestMethod]
    public void Conversions_ShouldRoundTripThroughCanonicalForm()
    {
        var code = (TerritoryCode)"au-nsw";
        string canonical = code;

        Assert.AreEqual("AU-NSW", canonical);
    }

    /// <summary>
    /// Verifies that a <see cref="TerritoryCode" /> flows into the string-based resolution surface via its implicit
    /// conversion, resolving a subdivision-scoped occurrence for the matching subdivision and nothing for a sibling.
    /// </summary>
    /// <param name="territory">The subdivision code passed to the resolver.</param>
    /// <param name="expectedCount">The expected number of resolved occurrences.</param>
    [TestMethod]
    [DataRow("AU-NSW", 1)]  // the scoped subdivision resolves the occurrence
    [DataRow("AU-VIC", 0)]  // a sibling subdivision resolves nothing
    public void Resolve_WhenPassedTerritoryCode_ShouldResolveThroughStringSurface(string territory, int expectedCount)
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.territory-interop">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="state-holiday" displayName="State Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Applicability><Territory code="AU-NSW" /></Applicability><Strategy><Fixed month="January" day="26" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        Assert.HasCount(expectedCount, service.Resolve(new DateOnly(2025, 1, 26), TerritoryCode.Parse(territory)));
    }

    /// <summary>
    /// Verifies that a comma-separated list parses every entry in source order, normalizing case.
    /// </summary>
    [TestMethod]
    public void ParseList_WhenCommaSeparated_ShouldParseAllInOrder()
    {
        IReadOnlyList<TerritoryCode> codes = TerritoryCode.ParseList("AU, au-nsw ,US-CA");

        CollectionAssert.AreEqual(
            new[] { "AU", "AU-NSW", "US-CA" },
            codes.Select(c => c.ToString()).ToArray());
    }

    /// <summary>
    /// Verifies that a <see langword="null" />, empty, or white-space input yields an empty list.
    /// </summary>
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [TestMethod]
    public void ParseList_WhenNullEmptyOrWhitespace_ShouldReturnEmpty(string? value)
    {
        Assert.IsEmpty(TerritoryCode.ParseList(value));
    }

    /// <summary>
    /// Verifies that blank entries between commas are ignored.
    /// </summary>
    [TestMethod]
    public void ParseList_WhenBlankEntries_ShouldIgnoreThem()
    {
        IReadOnlyList<TerritoryCode> codes = TerritoryCode.ParseList("AU,, ,AU-NSW");

        Assert.HasCount(2, codes);
    }

    /// <summary>
    /// Verifies that any malformed non-blank entry throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ParseList_WhenAnyEntryInvalid_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = TerritoryCode.ParseList("AU, not-a-code");
        });
    }

    /// <summary>
    /// Verifies that the boxed <see cref="object.Equals(object)" /> override matches an equal code and rejects a
    /// non-<see cref="TerritoryCode" /> object.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedAsObject_ShouldMatchValue()
    {
        var code = TerritoryCode.Parse("AU-NSW");

        Assert.IsTrue(code.Equals((object)TerritoryCode.Parse("AU-NSW")));
        Assert.IsFalse(code.Equals((object)"AU-NSW"));
    }

    /// <summary>
    /// Verifies that a subdivision containing a non-alphanumeric character is rejected by <c>TryParse</c>.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenSubdivisionHasInvalidCharacter_ShouldReturnFalse() =>
        Assert.IsFalse(TerritoryCode.TryParse("US-1$", out _));
}
