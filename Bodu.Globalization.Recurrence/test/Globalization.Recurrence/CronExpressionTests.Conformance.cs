// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.Conformance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies the Vixie day-of-month / day-of-week UNION rule: when both day fields are restricted, an instant
    /// matches if either field matches (not the AND semantics some libraries use).
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenBothDayFieldsRestricted_ShouldUseUnion()
    {
        CronExpression cron = CronExpression.Parse("0 0 1 * MON");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2020, 1, 1, 12, 0, 0));

        // Vixie union → the next Monday (the 6th); an AND engine would return 2020-06-01.
        Assert.AreEqual(new DateTime(2020, 1, 6, 0, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a day-of-month expression skips months that lack that day rather than clamping.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenDayThirtyOne_ShouldSkipShortMonths()
    {
        CronExpression cron = CronExpression.Parse("0 0 31 * *");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2020, 1, 31, 0, 0, 1));

        Assert.AreEqual(new DateTime(2020, 3, 31, 0, 0, 0), next);
    }

    /// <summary>
    /// Verifies that the previous-occurrence search for the 31st lands on the prior month that has a 31st.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenDayThirtyOne_ShouldSkipShortMonths()
    {
        CronExpression cron = CronExpression.Parse("0 0 31 * *");

        DateTime? previous = cron.GetPreviousOccurrence(new DateTime(2020, 1, 31, 0, 0, 0));

        Assert.AreEqual(new DateTime(2019, 12, 31, 0, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that the previous-occurrence search for February 29th lands on the prior leap year.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenLeapDay_ShouldReturnPriorLeapYear()
    {
        CronExpression cron = CronExpression.Parse("0 0 29 2 *");

        DateTime? previous = cron.GetPreviousOccurrence(new DateTime(2021, 3, 1, 0, 0, 0));

        Assert.AreEqual(new DateTime(2020, 2, 29, 0, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that an impossible date (February 30th) yields no occurrence and the search terminates.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenImpossibleDate_ShouldReturnNull()
    {
        CronExpression cron = CronExpression.Parse("0 0 30 2 *");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 0, 0));

        Assert.IsNull(next);
    }

    /// <summary>
    /// Verifies that a non-divisor step stops at the range end and rolls to the next unit rather than wrapping.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenNonDivisorStep_ShouldNotWrap()
    {
        CronExpression cron = CronExpression.Parse("0-59/13 * * * *");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 52, 0));

        // Minutes fire at 0,13,26,39,52; after 52 the next is the top of the following hour, not minute 5.
        Assert.AreEqual(new DateTime(2020, 1, 1, 1, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a start-value step (<c>a/n</c>) begins at the start value and steps to the field maximum.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenStartValueStep_ShouldBeginAtStart()
    {
        CronExpression cron = CronExpression.Parse("5/15 * * * *");

        Assert.AreEqual(new DateTime(2020, 1, 1, 0, 5, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 0, 0)));
        Assert.AreEqual(new DateTime(2019, 12, 31, 23, 50, 0), cron.GetPreviousOccurrence(new DateTime(2020, 1, 1, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that advancing a higher field resets the lower fields to their minimum (no carried seconds/minutes).
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenHigherFieldAdvances_ShouldResetLowerFields()
    {
        CronExpression cron = CronExpression.Parse("*/30 * 23,0,1,2 * * 1-5", CronFormat.WithSeconds);

        DateTime? next = cron.GetNextOccurrence(new DateTime(2020, 1, 2, 14, 40, 25));

        // Thursday; the hour rolls to 23 and seconds/minutes reset to 0 (not 23:00:30).
        Assert.AreEqual(new DateTime(2020, 1, 2, 23, 0, 0), next);
    }

    /// <summary>
    /// Verifies the inclusive and exclusive boundary behavior of the next and previous searches at an exact match.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenExactMatch_ShouldHonorInclusiveFlag()
    {
        CronExpression cron = CronExpression.Parse("0 0 * * *");
        var midnight = new DateTime(2020, 1, 1, 0, 0, 0);

        Assert.AreEqual(new DateTime(2020, 1, 2), cron.GetNextOccurrence(midnight, inclusive: false));
        Assert.AreEqual(midnight, cron.GetNextOccurrence(midnight, inclusive: true));
        Assert.AreEqual(new DateTime(2019, 12, 31), cron.GetPreviousOccurrence(midnight, inclusive: false));
        Assert.AreEqual(midnight, cron.GetPreviousOccurrence(midnight, inclusive: true));
    }

    /// <summary>
    /// Verifies that a step in the day-of-week field selects Sunday and the stepped weekdays (Vixie semantics).
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenWeekdayStep_ShouldSelectSundayAndFriday()
    {
        CronExpression cron = CronExpression.Parse("0 0 * * */5");

        // */5 over 0-6 selects 0 (Sunday) and 5 (Friday); the next after Wed 2020-01-01 is Fri 2020-01-03.
        Assert.AreEqual(new DateTime(2020, 1, 3, 0, 0, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that a reversed range is rejected, matching Vixie/cronie behavior.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenReversedRange_ShouldReturnFalse()
    {
        bool parsed = CronExpression.TryParse("0 0 * * 5-1", out CronExpression? result);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that the standard macros expand to their canonical five-field equivalents, and that <c>@weekly</c> is
    /// Sunday.
    /// </summary>
    /// <param name="macro">The macro token.</param>
    /// <param name="equivalent">The equivalent five-field expression.</param>
    [TestMethod]
    [DataRow("@yearly", "0 0 1 1 *")]
    [DataRow("@annually", "0 0 1 1 *")]
    [DataRow("@monthly", "0 0 1 * *")]
    [DataRow("@weekly", "0 0 * * 0")]
    [DataRow("@daily", "0 0 * * *")]
    [DataRow("@midnight", "0 0 * * *")]
    [DataRow("@hourly", "0 * * * *")]
    public void Parse_WhenMacro_ShouldEqualCanonicalExpression(string macro, string equivalent)
    {
        Assert.AreEqual(CronExpression.Parse(equivalent), CronExpression.Parse(macro));
    }
}
