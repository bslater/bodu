// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that a representative rule parses into the expected component values.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Parse_WhenTypicalRule_ShouldPopulateComponents()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE,FR;COUNT=10;WKST=SU");

        Assert.AreEqual(RecurrenceFrequency.Weekly, rule.Frequency);
        Assert.AreEqual(2, rule.Interval);
        Assert.AreEqual(10, rule.Count);
        Assert.AreEqual(DayOfWeek.Sunday, rule.WeekStart);
        Assert.AreEqual(3, rule.ByDay.Count);
    }

    /// <summary>
    /// Verifies that an optional <c>RRULE:</c> prefix is accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRRulePrefixPresent_ShouldParse()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("RRULE:FREQ=DAILY;INTERVAL=3");

        Assert.AreEqual(RecurrenceFrequency.Daily, rule.Frequency);
        Assert.AreEqual(3, rule.Interval);
    }

    /// <summary>
    /// Verifies that a signed ordinal in a <c>BYDAY</c> entry parses into the weekday ordinal.
    /// </summary>
    [TestMethod]
    public void Parse_WhenByDayHasOrdinal_ShouldParseOrdinal()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYDAY=-1FR");

        Assert.AreEqual(1, rule.ByDay.Count);
        Assert.AreEqual(new WeekDayNum(-1, DayOfWeek.Friday), rule.ByDay[0]);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRule.Parse(string)" /> throws
    /// <see cref="ArgumentNullException" /> for a null input.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RecurrenceRule.Parse(null!);
        });
    }

    /// <summary>
    /// Verifies that a malformed rule throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMalformed_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = RecurrenceRule.Parse("INTERVAL=2");
        });
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRule.TryParse(string, out RecurrenceRule)" /> reports failure without
    /// throwing for malformed input.
    /// </summary>
    /// <param name="text">The malformed rule text.</param>
    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow("INTERVAL=2", DisplayName = "no FREQ")]
    [DataRow("FREQ=NEVER", DisplayName = "unknown FREQ")]
    [DataRow("FREQ=DAILY;INTERVAL=0", DisplayName = "zero interval")]
    [DataRow("FREQ=DAILY;COUNT=0", DisplayName = "zero count")]
    [DataRow("FREQ=DAILY;COUNT=1;UNTIL=20260101", DisplayName = "count and until")]
    [DataRow("FREQ=MONTHLY;BYMONTHDAY=32", DisplayName = "month day out of range")]
    [DataRow("FREQ=YEARLY;BYMONTH=13", DisplayName = "month out of range")]
    [DataRow("FREQ=DAILY;FOO=1", DisplayName = "unknown part")]
    [DataRow("FREQ=DAILY;FREQ=WEEKLY", DisplayName = "duplicate FREQ")]
    public void TryParse_WhenMalformed_ShouldReturnFalse(string text)
    {
        bool parsed = RecurrenceRule.TryParse(text, out RecurrenceRule? result);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that a null input reports failure without throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenNull_ShouldReturnFalse()
    {
        bool parsed = RecurrenceRule.TryParse(null, out RecurrenceRule? result);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
    }
}
