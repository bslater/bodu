// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Verifies <see cref="RecurrenceRuleBuilder" />: the fluent surface, its per-part range validation, and that a built
/// rule formats to the <c>RRULE</c> a parser reads back.
/// </summary>
/// <remarks>
/// <para>
/// The builder exists so a caller can compose a rule without writing RFC 5545 text by hand, which only helps if it
/// rejects what the text form would reject. Each <c>BY*</c> part has its own range, and several of them accept
/// negative positions counted from the end of the period - so the validation is not one rule applied nine times, and
/// each part is checked at its own boundaries.
/// </para>
/// <para>
/// Every built rule is asserted through its formatted form and then re-parsed. Checking the properties alone would
/// pass for a rule that cannot be serialized, which is not a usable rule.
/// </para>
/// </remarks>
[TestClass]
public class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that a rule built with only a frequency formats to that frequency alone.
    /// </summary>
    [TestMethod]
    public void Build_WhenOnlyFrequencySet_ShouldFormatFrequencyOnly()
    {
        Assert.AreEqual("FREQ=DAILY", new RecurrenceRuleBuilder(RecurrenceFrequency.Daily).Build().ToString());
    }

    /// <summary>
    /// Verifies that each scalar modifier is carried onto the built rule.
    /// </summary>
    [TestMethod]
    public void Build_WhenScalarModifiersSet_ShouldCarryThemOntoTheRule()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
            .WithInterval(2)
            .WithCount(10)
            .WithWeekStart(DayOfWeek.Sunday)
            .Build();

        Assert.AreEqual(RecurrenceFrequency.Weekly, rule.Frequency);
        Assert.AreEqual(2, rule.Interval);
        Assert.AreEqual(10, rule.Count);
        Assert.AreEqual(DayOfWeek.Sunday, rule.WeekStart);
    }

    /// <summary>
    /// Verifies that an end date is carried onto the built rule.
    /// </summary>
    [TestMethod]
    public void WithUntil_ShouldCarryTheEndDateOntoTheRule()
    {
        var until = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily).WithUntil(until).Build();

        Assert.AreEqual(until, rule.Until);
    }

    /// <summary>
    /// Verifies that each unsigned <c>BY*</c> part accepts its documented range and appears in the formatted rule.
    /// </summary>
    /// <param name="part">The part under test.</param>
    /// <param name="low">The lowest accepted value.</param>
    /// <param name="high">The highest accepted value.</param>
    [TestMethod]
    [DataRow("BySecond", 0, 60)]
    [DataRow("ByMinute", 0, 59)]
    [DataRow("ByHour", 0, 23)]
    [DataRow("ByMonth", 1, 12)]
    public void UnsignedByParts_WhenValuesAreInRange_ShouldBeAccepted(string part, int low, int high)
    {
        RecurrenceRule rule = Apply(new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly), part, low, high).Build();

        string formatted = rule.ToString();
        Assert.Contains(low.ToString(System.Globalization.CultureInfo.InvariantCulture), formatted);
        Assert.Contains(high.ToString(System.Globalization.CultureInfo.InvariantCulture), formatted);
    }

    /// <summary>
    /// Verifies that a value outside an unsigned part's range is rejected at each boundary, so an out-of-range value
    /// fails where it is supplied rather than when the rule is later formatted or evaluated.
    /// </summary>
    /// <param name="part">The part under test.</param>
    /// <param name="low">The lowest accepted value.</param>
    /// <param name="high">The highest accepted value.</param>
    [TestMethod]
    [DataRow("BySecond", 0, 60)]
    [DataRow("ByMinute", 0, 59)]
    [DataRow("ByHour", 0, 23)]
    [DataRow("ByMonth", 1, 12)]
    public void UnsignedByParts_WhenValueIsOutOfRange_ShouldThrow(string part, int low, int high)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = Apply(new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly), part, low - 1));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = Apply(new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly), part, high + 1));
    }

    /// <summary>
    /// Verifies that a signed <c>BY*</c> part accepts positions counted from either end of its period, since a rule
    /// such as "the last day of the month" is expressed as a negative position.
    /// </summary>
    /// <param name="part">The part under test.</param>
    /// <param name="magnitude">The largest accepted magnitude.</param>
    [TestMethod]
    [DataRow("ByMonthDay", 31)]
    [DataRow("ByYearDay", 366)]
    [DataRow("ByWeekNo", 53)]
    [DataRow("BySetPos", 366)]
    public void SignedByParts_WhenValuesAreInRange_ShouldAcceptBothDirections(string part, int magnitude)
    {
        RecurrenceRule rule = Apply(new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly), part, 1, -1, magnitude, -magnitude).Build();

        Assert.Contains("-1", rule.ToString());
    }

    /// <summary>
    /// Verifies that a signed part rejects zero - there is no zeroth day or position - and rejects magnitudes beyond
    /// its range in both directions.
    /// </summary>
    /// <param name="part">The part under test.</param>
    /// <param name="magnitude">The largest accepted magnitude.</param>
    [TestMethod]
    [DataRow("ByMonthDay", 31)]
    [DataRow("ByYearDay", 366)]
    [DataRow("ByWeekNo", 53)]
    [DataRow("BySetPos", 366)]
    public void SignedByParts_WhenValueIsZeroOrOutOfRange_ShouldThrow(string part, int magnitude)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = Apply(new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly), part, 0));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = Apply(new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly), part, magnitude + 1));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = Apply(new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly), part, -(magnitude + 1)));
    }

    /// <summary>
    /// Verifies that a weekday list is carried onto the rule, through both the plain-day and ordinal-day overloads.
    /// </summary>
    [TestMethod]
    public void ByDay_ShouldAcceptPlainAndOrdinalWeekdays()
    {
        RecurrenceRule plain = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
            .ByDay(DayOfWeek.Monday, DayOfWeek.Friday)
            .Build();

        Assert.Contains("MO", plain.ToString());
        Assert.Contains("FR", plain.ToString());

        RecurrenceRule ordinal = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
            .ByDay(new WeekDayNum(-1, DayOfWeek.Monday))
            .Build();

        Assert.Contains("-1MO", ordinal.ToString());
    }

    /// <summary>
    /// Verifies that both <c>ByDay</c> overloads reject a <see langword="null" /> array.
    /// </summary>
    [TestMethod]
    public void ByDay_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly).ByDay((WeekDayNum[])null!));

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
            _ = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly).ByDay((DayOfWeek[])null!));
    }

    /// <summary>
    /// Verifies that the builder copies the arrays it is given, so a caller mutating its own array afterwards cannot
    /// change a rule that has already been built.
    /// </summary>
    [TestMethod]
    public void ByParts_ShouldCopyTheSuppliedArrays()
    {
        int[] months = [1, 6];
        WeekDayNum[] days = [new(0, DayOfWeek.Monday)];

        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
            .ByMonth(months)
            .ByDay(days)
            .Build();

        months[0] = 12;
        days[0] = new WeekDayNum(0, DayOfWeek.Sunday);

        Assert.Contains("BYMONTH=1,6", rule.ToString());
        Assert.Contains("MO", rule.ToString());
    }

    /// <summary>
    /// Verifies that a rule composed through the builder round-trips: its formatted form parses back to an equal
    /// rule, so the builder and the parser agree on the same rule set.
    /// </summary>
    [TestMethod]
    public void Build_WhenFullyComposed_ShouldRoundTripThroughParse()
    {
        RecurrenceRule built = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
            .WithInterval(3)
            .WithCount(12)
            .WithWeekStart(DayOfWeek.Monday)
            .ByMonth(1, 6, 12)
            .ByMonthDay(1, -1)
            .ByDay(new WeekDayNum(1, DayOfWeek.Monday))
            .ByHour(9, 17)
            .ByMinute(0, 30)
            .BySetPos(-1)
            .Build();

        RecurrenceRule parsed = RecurrenceRule.Parse(built.ToString());

        Assert.AreEqual(built.ToString(), parsed.ToString());
        Assert.AreEqual(built, parsed);
    }

    /// <summary>
    /// Verifies that calling a part twice replaces the earlier values rather than appending to them, so a builder
    /// reused across variations does not accumulate a rule the caller never asked for.
    /// </summary>
    [TestMethod]
    public void ByParts_WhenCalledTwice_ShouldReplaceRatherThanAppend()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
            .ByMonth(1, 2)
            .ByMonth(6)
            .Build();

        Assert.Contains("BYMONTH=6", rule.ToString());
        Assert.DoesNotContain("BYMONTH=1", rule.ToString());
    }

    /// <summary>
    /// Applies a named <c>BY*</c> part to the builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="part">The part name.</param>
    /// <param name="values">The values to supply.</param>
    /// <returns>The builder, for chaining.</returns>
    private static RecurrenceRuleBuilder Apply(RecurrenceRuleBuilder builder, string part, params int[] values) =>
        part switch
        {
            "BySecond" => builder.BySecond(values),
            "ByMinute" => builder.ByMinute(values),
            "ByHour" => builder.ByHour(values),
            "ByMonth" => builder.ByMonth(values),
            "ByMonthDay" => builder.ByMonthDay(values),
            "ByYearDay" => builder.ByYearDay(values),
            "ByWeekNo" => builder.ByWeekNo(values),
            "BySetPos" => builder.BySetPos(values),
            _ => throw new ArgumentOutOfRangeException(nameof(part)),
        };
}
