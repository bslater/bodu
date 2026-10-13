// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaPatternsTests.IsMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateSchemaPatternsTests
{
    /// <summary>
    /// Verifies that a value the schema accepts for a pattern, matched whole, is reported as matching.
    /// </summary>
    /// <param name="pattern">The name of the <see cref="NotableDateSchemaPattern" /> member.</param>
    /// <param name="value">The value.</param>
    [TestMethod]
    [DataRow("Identifier", "r")]
    [DataRow("Identifier", "new-year")]
    [DataRow("Identifier", "a1-b2-c3")]
    [DataRow("ResourceIdentifier", "test")]
    [DataRow("ResourceIdentifier", "bodu.calendar-au")]
    [DataRow("ResourceIdentifier", "a.b.c-d")]
    [DataRow("TerritoryCode", "AU")]
    [DataRow("TerritoryCode", "AU-NSW")]
    [DataRow("TerritoryCode", "GB-ENG")]
    [DataRow("TerritoryCode", "FR-75")]
    public void IsMatch_WhenWholeValueMatches_ShouldReturnTrue(string pattern, string value)
    {
        Assert.IsTrue(NotableDateSchemaPatterns.IsMatch(value, Enum.Parse<NotableDateSchemaPattern>(pattern)));
    }

    /// <summary>
    /// Verifies that a value the schema rejects for a pattern, an empty value or one followed by a line break included,
    /// is reported as not matching.
    /// </summary>
    /// <param name="pattern">The name of the <see cref="NotableDateSchemaPattern" /> member.</param>
    /// <param name="value">The value.</param>
    [TestMethod]
    [DataRow("Identifier", "")]
    [DataRow("Identifier", "New-Year")]
    [DataRow("Identifier", "1r")]
    [DataRow("Identifier", "r-")]
    [DataRow("Identifier", "r--1")]
    [DataRow("Identifier", "r.1")]
    [DataRow("Identifier", "new year")]
    [DataRow("Identifier", "r\n")]
    [DataRow("ResourceIdentifier", "")]
    [DataRow("ResourceIdentifier", "Test")]
    [DataRow("ResourceIdentifier", "test..case")]
    [DataRow("ResourceIdentifier", "test.")]
    [DataRow("TerritoryCode", "")]
    [DataRow("TerritoryCode", "au")]
    [DataRow("TerritoryCode", "AUS")]
    [DataRow("TerritoryCode", "AU-")]
    [DataRow("TerritoryCode", "AU-NEWSW")]
    [DataRow("TerritoryCode", "AU\n")]
    public void IsMatch_WhenValueBreaksPattern_ShouldReturnFalse(string pattern, string value)
    {
        Assert.IsFalse(NotableDateSchemaPatterns.IsMatch(value, Enum.Parse<NotableDateSchemaPattern>(pattern)));
    }
}
