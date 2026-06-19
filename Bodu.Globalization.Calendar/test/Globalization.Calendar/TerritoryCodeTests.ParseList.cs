// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCodeTests.ParseList.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class TerritoryCodeTests
{
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
