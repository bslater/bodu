// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
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
}
