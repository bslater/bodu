// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.Fixed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateRuleBuilderTests
{
    /// <summary>
    /// Verifies that a numeric month on a <c>Fixed</c> rule in a calendar other than Gregorian validates without an
    /// error, because it is written as the number the loader reads in that calendar rather than as an English month
    /// name, which the loader rejects there.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    [TestMethod]
    [DataRow(CalendarSystem.Hijri)]
    [DataRow(CalendarSystem.UmmAlQura)]
    [DataRow(CalendarSystem.Hebrew)]
    [DataRow(CalendarSystem.Persian)]
    [DataRow(CalendarSystem.ChineseLunisolar)]
    public void Fixed_WhenRuleCalendarIsNotGregorian_ShouldValidateWithoutErrors(CalendarSystem calendar)
    {
        NotableDateDocumentBuilder builder = RuleDocument(r => r.ForCalendar(calendar).Fixed(7, 15));

        IReadOnlyList<NotableDateValidationDiagnostic> errors = [.. builder.Validate().Where(d => d.Severity == NotableDateValidationSeverity.Error)];

        Assert.IsEmpty(errors, string.Join(" | ", errors.Select(d => d.Code + ": " + d.Message)));
    }

    /// <summary>
    /// Verifies that a numeric month on a <c>Fixed</c> rule in a calendar other than Gregorian is written to the
    /// document as its number.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenRuleCalendarIsNotGregorian_ShouldWriteTheMonthNumber()
    {
        XDocument document = XDocument.Parse(RuleDocument(r => r.ForCalendar(CalendarSystem.Hebrew).Fixed(7, 15)).ToXml());

        Assert.AreEqual("7", FixedMonths(document).Single());
    }

    /// <summary>
    /// Verifies that a numeric month on a <c>Fixed</c> rule in a calendar other than Gregorian builds a strategy for
    /// that numbered month of the calendar.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenRuleCalendarIsNotGregorian_ShouldBuildTheNumberedMonth()
    {
        NotableDateResource resource = BuildRule(r => r.ForCalendar(CalendarSystem.Hebrew).Fixed(7, 15));

        Assert.AreEqual((7, (string?)null), FixedMonth(resource));
    }

    /// <summary>
    /// Verifies that a numeric month on a <c>Fixed</c> rule whose calendar is set to one other than Gregorian after the
    /// strategy builds a strategy for that numbered month, as it does when the calendar is set first.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenCalendarIsSetAfterFixed_ShouldBuildTheNumberedMonth()
    {
        NotableDateResource resource = BuildRule(r => r.Fixed(7, 15).ForCalendar(CalendarSystem.Hebrew));

        Assert.AreEqual((7, (string?)null), FixedMonth(resource));
    }

    /// <summary>
    /// Verifies that a numeric month on a <c>Fixed</c> rule whose calendar is set back to Gregorian after the strategy
    /// is written as its English month name, as it is on a rule that was always Gregorian.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenCalendarIsSetToGregorianAfterFixed_ShouldWriteTheEnglishMonthName()
    {
        XDocument document = XDocument.Parse(RuleDocument(r => r.ForCalendar(CalendarSystem.Hebrew).Fixed(7, 15).ForCalendar(CalendarSystem.Gregorian)).ToXml());

        Assert.AreEqual("July", FixedMonths(document).Single());
    }

    /// <summary>
    /// Verifies that a numeric month on a Gregorian <c>Fixed</c> rule is written as its English month name.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenRuleCalendarIsGregorian_ShouldWriteTheEnglishMonthName()
    {
        XDocument document = XDocument.Parse(RuleDocument(r => r.Fixed(7, 15)).ToXml());

        Assert.AreEqual("July", FixedMonths(document).Single());
    }

    /// <summary>
    /// Verifies that month 13 on a <c>Fixed</c> rule in a calendar whose leap year has a thirteenth month, the Hebrew or
    /// the Chinese lunisolar, builds a strategy for the thirteenth month rather than throwing.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    [TestMethod]
    [DataRow(CalendarSystem.Hebrew)]
    [DataRow(CalendarSystem.ChineseLunisolar)]
    public void Fixed_WhenMonthIsThirteenInThirteenMonthCalendar_ShouldBuildMonthThirteen(CalendarSystem calendar)
    {
        NotableDateResource resource = BuildRule(r => r.ForCalendar(calendar).Fixed(13, 1));

        Assert.AreEqual((13, (string?)null), FixedMonth(resource));
    }

    /// <summary>
    /// Verifies that month 13 on a <c>Fixed</c> rule in a calendar whose year has twelve months, or in the Chinese
    /// lunisolar calendar for a month that skips the leap month, throws <see cref="ArgumentOutOfRangeException" />
    /// naming the <c>month</c> parameter, as the loader rejects that month, rather than writing a month that never
    /// occurs.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="skipLeapMonth">A value indicating whether the month skips a Chinese leap month.</param>
    [TestMethod]
    [DataRow(CalendarSystem.Hijri, false)]
    [DataRow(CalendarSystem.UmmAlQura, false)]
    [DataRow(CalendarSystem.Persian, false)]
    [DataRow(CalendarSystem.ChineseLunisolar, true)]
    public void Fixed_WhenMonthIsThirteenInTwelveMonthCalendar_ShouldThrowArgumentOutOfRangeException(CalendarSystem calendar, bool skipLeapMonth)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BuildRule(r => r.ForCalendar(calendar).Fixed(13, 1, skipLeapMonth));
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that month 13 on the <c>Fixed</c> end strategy of a rule in a calendar whose year has twelve months
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the <c>month</c> parameter, as on the rule's own
    /// strategy.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenEndStrategyMonthIsThirteenInTwelveMonthCalendar_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BuildRule(r => r.ForCalendar(CalendarSystem.Hijri).Fixed(12, 1).UntilDate(e => e.Fixed(13, 1)));
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that month 13 on a Gregorian <c>Fixed</c> rule, whether the calendar is left to its default or declared,
    /// throws <see cref="ArgumentOutOfRangeException" /> naming the <c>month</c> parameter.
    /// </summary>
    /// <param name="declareGregorian">
    /// <see langword="true" /> to declare the Gregorian calendar; <see langword="false" /> to leave it to its default.
    /// </param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Fixed_WhenMonthIsThirteenInGregorianCalendar_ShouldThrowArgumentOutOfRangeException(bool declareGregorian)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BuildRule(r =>
            {
                if (declareGregorian)
                    _ = r.ForCalendar(CalendarSystem.Gregorian);

                _ = r.Fixed(13, 1);
            });
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a month outside 1 to 13 on a <c>Fixed</c> rule in a calendar other than Gregorian throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the <c>month</c> parameter.
    /// </summary>
    /// <param name="month">The out-of-range month.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(14)]
    public void Fixed_WhenMonthIsOutsideRangeOfCalendarOtherThanGregorian_ShouldThrowArgumentOutOfRangeException(int month)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BuildRule(r => r.ForCalendar(CalendarSystem.Hebrew).Fixed(month, 1));
        });

        Assert.AreEqual("month", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a numeric month on the <c>Fixed</c> end strategy of a rule in a calendar other than Gregorian,
    /// which the loader reads in the rule's calendar, builds an end strategy for that numbered month.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenEndStrategyOfRuleInCalendarOtherThanGregorian_ShouldBuildTheNumberedMonth()
    {
        NotableDateResource resource = BuildRule(r => r.ForCalendar(CalendarSystem.Hebrew).Fixed(7, 15).UntilDate(e => e.Fixed(7, 22)));

        Assert.AreEqual((7, (string?)null), EndFixedMonth(resource));
    }

    /// <summary>
    /// Verifies that a numeric month on the <c>Fixed</c> end strategy of a rule whose calendar is set to one other than
    /// Gregorian after the duration builds an end strategy for that numbered month.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenCalendarIsSetAfterEndStrategy_ShouldBuildTheNumberedMonth()
    {
        NotableDateResource resource = BuildRule(r => r.Fixed(7, 15).UntilDate(e => e.Fixed(7, 22)).ForCalendar(CalendarSystem.Hebrew));

        Assert.AreEqual((7, (string?)null), EndFixedMonth(resource));
    }

    /// <summary>
    /// Verifies that month 13 on the <c>Fixed</c> end strategy of a rule in a calendar other than Gregorian builds an
    /// end strategy for the thirteenth month rather than throwing.
    /// </summary>
    [TestMethod]
    public void Fixed_WhenEndStrategyMonthIsThirteenInCalendarOtherThanGregorian_ShouldBuildMonthThirteen()
    {
        NotableDateResource resource = BuildRule(r => r.ForCalendar(CalendarSystem.Hebrew).Fixed(12, 1).UntilDate(e => e.Fixed(13, 1)));

        Assert.AreEqual((13, (string?)null), EndFixedMonth(resource));
    }

    /// <summary>
    /// Returns the <c>month</c> attribute of every <c>Fixed</c> element in a document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>The month attribute values, in document order.</returns>
    private static IEnumerable<string?> FixedMonths(XDocument document) =>
        document.Descendants(XName.Get("Fixed", "urn:bodu:globalization:calendar")).Select(e => (string?)e.Attribute("month"));

    /// <summary>
    /// Returns the month and month alias of the single rule's <c>Fixed</c> strategy.
    /// </summary>
    /// <param name="resource">The built resource.</param>
    /// <returns>The month and the month alias.</returns>
    private static (int Month, string? Alias) FixedMonth(NotableDateResource resource)
    {
        var strategy = (FixedDateStrategy)resource.NotableDates.Single().Rules.Single().Strategy!;
        return (strategy.Month, strategy.MonthAlias);
    }

    /// <summary>
    /// Returns the month and month alias of the single rule's <c>Fixed</c> end strategy.
    /// </summary>
    /// <param name="resource">The built resource.</param>
    /// <returns>The month and the month alias.</returns>
    private static (int Month, string? Alias) EndFixedMonth(NotableDateResource resource)
    {
        var duration = (CalculatedEndDateDurationDefinition)resource.NotableDates.Single().Rules.Single().Duration!;
        var strategy = (FixedDateStrategy)duration.EndStrategy;
        return (strategy.Month, strategy.MonthAlias);
    }
}
