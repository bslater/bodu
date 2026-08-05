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
    /// Verifies that the previous-occurrence search is equally bounded: an expression that can never match answers
    /// <see langword="null" /> backward as well as forward.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenImpossibleDate_ShouldReturnNull()
    {
        CronExpression cron = CronExpression.Parse("0 0 30 2 *");

        DateTime? previous = cron.GetPreviousOccurrence(new DateTime(2020, 1, 1, 0, 0, 0));

        Assert.IsNull(previous);
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
    /// Verifies that the numeric weekday <c>7</c> is treated as Sunday.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenWeekdaySeven_ShouldMatchSunday()
    {
        CronExpression cron = CronExpression.Parse("0 0 * * 7");

        Assert.AreEqual(new DateTime(2020, 1, 5, 0, 0, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that named weekday and month ranges resolve to the correct numeric fields.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenNamedRanges_ShouldResolveFields()
    {
        Assert.AreEqual(
            new DateTime(2020, 1, 6, 9, 0, 0),
            CronExpression.Parse("0 9 * * MON-FRI").GetNextOccurrence(new DateTime(2020, 1, 4, 0, 0, 0)));
        Assert.AreEqual(
            new DateTime(2021, 1, 1, 0, 0, 0),
            CronExpression.Parse("0 0 1 JAN-MAR *").GetNextOccurrence(new DateTime(2020, 4, 1, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that a day-of-month range step selects the stepped days within the range.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenDayOfMonthRangeStep_ShouldSelectSteppedDays()
    {
        CronExpression cron = CronExpression.Parse("0 0 1-31/10 * *");

        Assert.AreEqual(new DateTime(2020, 1, 21, 0, 0, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 15, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that the previous-occurrence search crosses a year boundary.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenAcrossYearBoundary_ShouldReturnPriorYear()
    {
        CronExpression cron = CronExpression.Parse("0 0 1 1 *");

        Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0), cron.GetPreviousOccurrence(new DateTime(2020, 6, 1, 0, 0, 0)));
        Assert.AreEqual(new DateTime(2019, 1, 1, 0, 0, 0), cron.GetPreviousOccurrence(new DateTime(2020, 1, 1, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that a six-field seconds step matches at the stepped second.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenSecondsStep_ShouldMatchSteppedSecond()
    {
        CronExpression cron = CronExpression.Parse("*/20 * * * * *", CronFormat.WithSeconds);

        Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 20), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 0, 5)));
    }

    /// <summary>
    /// Verifies that a February 29th expression crosses a non-leap century year (2100), which yields an eight-year
    /// gap, rather than falling short of the next occurrence.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenLeapDayAcrossNonLeapCentury_ShouldSpanEightYears()
    {
        CronExpression cron = CronExpression.Parse("0 0 29 2 *");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2096, 3, 1, 0, 0, 0));

        // 2100 is not a leap year, so the next February 29th after 2096 is 2104.
        Assert.AreEqual(new DateTime(2104, 2, 29, 0, 0, 0), next);
    }

    /// <summary>
    /// Verifies that comma-list field values are treated as a set, so their written order does not affect the result.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenListOutOfOrder_ShouldTreatAsSortedSet()
    {
        CronExpression cron = CronExpression.Parse("0 20,10 * * *");

        Assert.AreEqual(new DateTime(2020, 1, 1, 10, 0, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 9, 0, 0)));
        Assert.AreEqual(new DateTime(2020, 1, 1, 20, 0, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 10, 30, 0)));
    }

    /// <summary>
    /// Verifies that a range step includes both the range lower bound and the last stepped value.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenRangeStepIncludesBounds_ShouldMatchZeroAndFiftyNine()
    {
        CronExpression cron = CronExpression.Parse("0-59/59 * * * *");

        Assert.AreEqual(new DateTime(2020, 1, 1, 0, 59, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 30, 0)));
        Assert.AreEqual(new DateTime(2020, 1, 1, 1, 0, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 59, 0)));
    }

    /// <summary>
    /// Verifies that a step larger than the field range collapses to the field minimum.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenStepExceedsFieldRange_ShouldCollapseToMinimum()
    {
        CronExpression cron = CronExpression.Parse("*/90 * * * *");

        Assert.AreEqual(new DateTime(2020, 1, 1, 1, 0, 0), cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 5, 0)));
    }

    /// <summary>
    /// Verifies that a day-of-week range whose upper bound is seven includes Sunday.
    /// </summary>
    [TestMethod]
    public void Parse_WhenWeekdayRangeToSeven_ShouldIncludeSunday()
    {
        Assert.AreEqual(CronExpression.Parse("0 0 * * 0,3,4,5,6"), CronExpression.Parse("0 0 * * 3-7"));
    }

    /// <summary>
    /// Verifies that mixed-case month and weekday names combine with the day union rule.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenMixedCaseNames_ShouldResolveAndUnion()
    {
        CronExpression cron = CronExpression.Parse("0 1 15 JUL mon,Wed,FRi");

        // July only; day-of-month 15 OR a Mon/Wed/Fri. 2020-07-01 is a Wednesday, matching via the union earlier than the 15th.
        Assert.AreEqual(new DateTime(2020, 7, 1, 1, 0, 0), cron.GetNextOccurrence(new DateTime(2019, 11, 14, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that month and weekday names containing the letters <c>L</c> or <c>W</c> (for example <c>JUL</c> and
    /// <c>WED</c>) are not mistaken for Quartz extension tokens.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNameContainsQuartzLetter_ShouldNotBeRejected()
    {
        Assert.AreEqual(CronExpression.Parse("0 0 1 7 *"), CronExpression.Parse("0 0 1 JUL *"));
        Assert.AreEqual(CronExpression.Parse("0 0 * * 3"), CronExpression.Parse("0 0 * * WED"));
    }

    /// <summary>
    /// Verifies that a day-of-month expression pinned to a month lacking that day never matches.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenImpossibleDayPinnedToMonth_ShouldReturnNull()
    {
        CronExpression cron = CronExpression.Parse("0 0 31 11 *");

        Assert.IsNull(cron.GetNextOccurrence(new DateTime(2020, 1, 1, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that the previous-occurrence search skips backward over a month lacking the target day.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenDayThirtyOneBackwardOverFebruary_ShouldReturnJanuary()
    {
        CronExpression cron = CronExpression.Parse("0 0 31 * *");

        Assert.AreEqual(new DateTime(2024, 1, 31, 0, 0, 0), cron.GetPreviousOccurrence(new DateTime(2024, 3, 15, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that surrounding whitespace and tab separators are tolerated.
    /// </summary>
    [TestMethod]
    public void Parse_WhenWhitespaceAndTabs_ShouldParseIdentically()
    {
        CronExpression expected = CronExpression.Parse("0 0 * * 1");

        Assert.AreEqual(expected, CronExpression.Parse("  0 0 * * 1  "));
        Assert.AreEqual(expected, CronExpression.Parse("0\t0\t*\t*\t1"));
    }

    /// <summary>
    /// Verifies that zero is rejected in the one-based day-of-month and month fields, and that a reversed weekday range
    /// (including one whose upper bound is seven) is rejected.
    /// </summary>
    /// <param name="expression">The invalid cron expression.</param>
    [TestMethod]
    [DataRow("0 0 0 1 *", DisplayName = "day-of-month zero")]
    [DataRow("0 0 1 0 *", DisplayName = "month zero")]
    [DataRow("0 0 * * 7-3", DisplayName = "reversed weekday range with seven")]
    public void TryParse_WhenFieldOutOfRange_ShouldReturnFalse(string expression)
    {
        bool parsed = CronExpression.TryParse(expression, out CronExpression? result);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
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
