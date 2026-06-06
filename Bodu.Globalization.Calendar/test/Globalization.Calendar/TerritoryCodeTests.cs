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

        Assert.AreEqual("AU", code.Country);
        Assert.IsNull(code.Subdivision);
        Assert.IsFalse(code.IsSubdivision);
        Assert.IsFalse(code.IsEmpty);
    }

    /// <summary>
    /// Verifies that a subdivision code decomposes into its country and subdivision parts.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSubdivision_ShouldDecomposeToCountryAndSubdivision()
    {
        var code = TerritoryCode.Parse("AU-NSW");

        Assert.AreEqual("AU", code.Country);
        Assert.AreEqual("NSW", code.Subdivision);
        Assert.IsTrue(code.IsSubdivision);
        Assert.AreEqual("AU", code.Parent.ToString());
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
        Assert.IsFalse(TerritoryCode.TryParse("nope!", out TerritoryCode result));
        Assert.IsTrue(result.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="TerritoryCode.TryParse" /> reports failure for <see langword="null" /> and empty input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenNullOrEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse(TerritoryCode.TryParse(null, out _));
        Assert.IsFalse(TerritoryCode.TryParse(string.Empty, out _));
    }

    /// <summary>
    /// Verifies that a country contains itself and its subdivisions, but a subdivision contains neither its parent nor a
    /// sibling, and unrelated countries never contain each other.
    /// </summary>
    [TestMethod]
    public void Contains_ShouldFollowParentChildSemantics()
    {
        var au = TerritoryCode.Parse("AU");
        var nsw = TerritoryCode.Parse("AU-NSW");
        var vic = TerritoryCode.Parse("AU-VIC");
        var us = TerritoryCode.Parse("US");

        Assert.IsTrue(au.Contains(au));
        Assert.IsTrue(au.Contains(nsw));
        Assert.IsTrue(nsw.Contains(nsw));

        Assert.IsFalse(nsw.Contains(au));
        Assert.IsFalse(nsw.Contains(vic));
        Assert.IsFalse(au.Contains(us));
        Assert.IsFalse(default(TerritoryCode).Contains(au));
        Assert.IsFalse(au.Contains(default));
    }

    /// <summary>
    /// Verifies that equality is case-insensitive after normalization and that equal codes share a hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameCodeDifferentCase_ShouldBeEqual()
    {
        var a = TerritoryCode.Parse("au-nsw");
        var b = TerritoryCode.Parse("AU-NSW");

        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.AreNotEqual(a, TerritoryCode.Parse("AU-VIC"));
    }

    /// <summary>
    /// Verifies that the default value renders as the empty string and is empty.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldBeEmpty()
    {
        TerritoryCode code = default;

        Assert.AreEqual(string.Empty, code.ToString());
        Assert.IsTrue(code.IsEmpty);
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
    /// conversion and resolves a subdivision-scoped occurrence.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPassedTerritoryCode_ShouldResolveThroughStringSurface()
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

        Assert.HasCount(1, service.Resolve(new DateOnly(2025, 1, 26), TerritoryCode.Parse("AU-NSW")));
        Assert.IsEmpty(service.Resolve(new DateOnly(2025, 1, 26), TerritoryCode.Parse("AU-VIC")));
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
}
