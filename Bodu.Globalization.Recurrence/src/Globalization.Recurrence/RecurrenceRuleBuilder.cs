// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Builds a <see cref="RecurrenceRule" /> fluently, as an alternative to parsing its textual form.
/// </summary>
/// <remarks>
/// <para>
/// The builder is mutable and single-threaded; each <c>By…</c> or <c>With…</c> method returns the same instance so
/// calls chain. Call <see cref="Build" /> to produce the immutable rule. A builder can be reused after
/// <see cref="Build" /> and each <c>By…</c> setter replaces any previously supplied values for that rule part.
/// </para>
/// <para>
/// <see cref="WithCount(int)" /> and <see cref="WithUntil(DateTime)" /> are mutually exclusive; supplying one clears
/// the other, matching the RFC 5545 rule that <c>COUNT</c> and <c>UNTIL</c> cannot both appear.
/// </para>
/// </remarks>
public sealed class RecurrenceRuleBuilder
{
    /// <summary>The base recurrence period.</summary>
    private readonly RecurrenceFrequency _frequency;

    /// <summary>The recurrence interval.</summary>
    private int _interval = 1;

    /// <summary>The occurrence count, or <see langword="null" /> when unset.</summary>
    private int? _count;

    /// <summary>The inclusive upper-bound instant, or <see langword="null" /> when unset.</summary>
    private DateTime? _until;

    /// <summary>The week-start day.</summary>
    private DayOfWeek _weekStart = DayOfWeek.Monday;

    /// <summary>The <c>BYSECOND</c> values.</summary>
    private int[] _bySecond = [];

    /// <summary>The <c>BYMINUTE</c> values.</summary>
    private int[] _byMinute = [];

    /// <summary>The <c>BYHOUR</c> values.</summary>
    private int[] _byHour = [];

    /// <summary>The <c>BYDAY</c> entries.</summary>
    private WeekDayNum[] _byDay = [];

    /// <summary>The <c>BYMONTHDAY</c> values.</summary>
    private int[] _byMonthDay = [];

    /// <summary>The <c>BYYEARDAY</c> values.</summary>
    private int[] _byYearDay = [];

    /// <summary>The <c>BYWEEKNO</c> values.</summary>
    private int[] _byWeekNo = [];

    /// <summary>The <c>BYMONTH</c> values.</summary>
    private int[] _byMonth = [];

    /// <summary>The <c>BYSETPOS</c> values.</summary>
    private int[] _bySetPos = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrenceRuleBuilder" /> class for the specified frequency.
    /// </summary>
    /// <param name="frequency">The base recurrence period of the rule being built.</param>
    public RecurrenceRuleBuilder(RecurrenceFrequency frequency)
    {
        _frequency = frequency;
    }

    /// <summary>
    /// Sets the interval — the positive multiple of the frequency between occurrences.
    /// </summary>
    /// <param name="interval">The recurrence interval.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="interval" /> is less than one.
    /// </exception>
    public RecurrenceRuleBuilder WithInterval(int interval)
    {
        RecurrenceThrowHelper.ThrowIfInvalidInterval(interval);
        _interval = interval;
        return this;
    }

    /// <summary>
    /// Sets the occurrence count and clears any previously set <c>UNTIL</c> bound.
    /// </summary>
    /// <param name="count">The maximum number of occurrences.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is less than one.
    /// </exception>
    public RecurrenceRuleBuilder WithCount(int count)
    {
        RecurrenceThrowHelper.ThrowIfInvalidCount(count);
        _count = count;
        _until = null;
        return this;
    }

    /// <summary>
    /// Sets the inclusive upper-bound instant and clears any previously set <c>COUNT</c> bound.
    /// </summary>
    /// <param name="until">The instant beyond which no occurrence is produced.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    public RecurrenceRuleBuilder WithUntil(DateTime until)
    {
        _until = until;
        _count = null;
        return this;
    }

    /// <summary>
    /// Sets the day the week starts on, governing weekly-interval and week-number arithmetic.
    /// </summary>
    /// <param name="weekStart">The week-start day.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    public RecurrenceRuleBuilder WithWeekStart(DayOfWeek weekStart)
    {
        _weekStart = weekStart;
        return this;
    }

    /// <summary>
    /// Sets the <c>BYSECOND</c> rule part.
    /// </summary>
    /// <param name="seconds">The seconds to select (0–60).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 0 to 60.</exception>
    public RecurrenceRuleBuilder BySecond(params int[] seconds)
    {
        _bySecond = ValidateUInt(seconds, 0, 60, nameof(RecurrenceRule.BySecond));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMINUTE</c> rule part.
    /// </summary>
    /// <param name="minutes">The minutes to select (0–59).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 0 to 59.</exception>
    public RecurrenceRuleBuilder ByMinute(params int[] minutes)
    {
        _byMinute = ValidateUInt(minutes, 0, 59, nameof(RecurrenceRule.ByMinute));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYHOUR</c> rule part.
    /// </summary>
    /// <param name="hours">The hours to select (0–23).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 0 to 23.</exception>
    public RecurrenceRuleBuilder ByHour(params int[] hours)
    {
        _byHour = ValidateUInt(hours, 0, 23, nameof(RecurrenceRule.ByHour));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYDAY</c> rule part from weekday entries.
    /// </summary>
    /// <param name="days">The weekday entries to select.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    public RecurrenceRuleBuilder ByDay(params WeekDayNum[] days)
    {
        ArgumentNullException.ThrowIfNull(days);
        _byDay = (WeekDayNum[])days.Clone();
        return this;
    }

    /// <summary>
    /// Sets the <c>BYDAY</c> rule part from plain weekdays, each with no positional ordinal.
    /// </summary>
    /// <param name="days">The weekdays to select on every occurrence of that day within the period.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    public RecurrenceRuleBuilder ByDay(params DayOfWeek[] days)
    {
        ArgumentNullException.ThrowIfNull(days);
        _byDay = Array.ConvertAll(days, day => new WeekDayNum(0, day));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMONTHDAY</c> rule part.
    /// </summary>
    /// <param name="monthDays">The month days to select (1–31 or -31 to -1).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is zero or outside ±31.</exception>
    public RecurrenceRuleBuilder ByMonthDay(params int[] monthDays)
    {
        _byMonthDay = ValidateSigned(monthDays, 1, 31, nameof(RecurrenceRule.ByMonthDay));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYYEARDAY</c> rule part.
    /// </summary>
    /// <param name="yearDays">The year days to select (1–366 or -366 to -1).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is zero or outside ±366.</exception>
    public RecurrenceRuleBuilder ByYearDay(params int[] yearDays)
    {
        _byYearDay = ValidateSigned(yearDays, 1, 366, nameof(RecurrenceRule.ByYearDay));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYWEEKNO</c> rule part.
    /// </summary>
    /// <param name="weekNumbers">The week numbers to select (1–53 or -53 to -1).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is zero or outside ±53.</exception>
    public RecurrenceRuleBuilder ByWeekNo(params int[] weekNumbers)
    {
        _byWeekNo = ValidateSigned(weekNumbers, 1, 53, nameof(RecurrenceRule.ByWeekNo));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMONTH</c> rule part.
    /// </summary>
    /// <param name="months">The months to select (1–12).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 1 to 12.</exception>
    public RecurrenceRuleBuilder ByMonth(params int[] months)
    {
        _byMonth = ValidateUInt(months, 1, 12, nameof(RecurrenceRule.ByMonth));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYSETPOS</c> rule part.
    /// </summary>
    /// <param name="setPositions">The set positions to select (1–366 or -366 to -1).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is zero or outside ±366.</exception>
    public RecurrenceRuleBuilder BySetPos(params int[] setPositions)
    {
        _bySetPos = ValidateSigned(setPositions, 1, 366, nameof(RecurrenceRule.BySetPos));
        return this;
    }

    /// <summary>
    /// Builds the immutable <see cref="RecurrenceRule" /> from the configured components.
    /// </summary>
    /// <returns>The constructed rule.</returns>
    public RecurrenceRule Build() =>
        new(
            _frequency, _interval, _count, _until, _weekStart,
            _bySecond, _byMinute, _byHour, _byDay,
            _byMonthDay, _byYearDay, _byWeekNo, _byMonth, _bySetPos);

    /// <summary>
    /// Validates that every value lies within an inclusive non-negative range and returns a defensive copy.
    /// </summary>
    /// <param name="values">The values to validate.</param>
    /// <param name="lo">The inclusive lower bound.</param>
    /// <param name="hi">The inclusive upper bound.</param>
    /// <param name="part">The rule-part name reported on failure.</param>
    /// <returns>A copy of <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="values" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range.</exception>
    private static int[] ValidateUInt(int[] values, int lo, int hi, string part)
    {
        ArgumentNullException.ThrowIfNull(values, part);
        foreach (int value in values)
        {
            if (value < lo || value > hi)
            {
                throw new ArgumentOutOfRangeException(
                    part,
                    value,
                    string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Arg_OutOfRange_RecurrenceRulePart, part));
            }
        }

        return (int[])values.Clone();
    }

    /// <summary>
    /// Validates that every value is non-zero with a magnitude in an inclusive range and returns a defensive copy.
    /// </summary>
    /// <param name="values">The values to validate.</param>
    /// <param name="magLo">The inclusive lower bound on the magnitude.</param>
    /// <param name="magHi">The inclusive upper bound on the magnitude.</param>
    /// <param name="part">The rule-part name reported on failure.</param>
    /// <returns>A copy of <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="values" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a value is zero or its magnitude is out of range.
    /// </exception>
    private static int[] ValidateSigned(int[] values, int magLo, int magHi, string part)
    {
        ArgumentNullException.ThrowIfNull(values, part);
        foreach (int value in values)
        {
            int magnitude = Math.Abs(value);
            if (value == 0 || magnitude < magLo || magnitude > magHi)
            {
                throw new ArgumentOutOfRangeException(
                    part,
                    value,
                    string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Arg_OutOfRange_RecurrenceRulePart, part));
            }
        }

        return (int[])values.Clone();
    }
}
