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
    /// Sets the interval - the positive multiple of the frequency between occurrences.
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="weekStart" /> is not a defined <see cref="DayOfWeek" /> value.
    /// </exception>
    public RecurrenceRuleBuilder WithWeekStart(DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);

        _weekStart = weekStart;
        return this;
    }

    /// <summary>
    /// Sets the <c>BYSECOND</c> rule part.
    /// </summary>
    /// <param name="seconds">The seconds to select (0-60).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 0 to 60.</exception>
    public RecurrenceRuleBuilder BySecond(params int[] seconds)
    {
        _bySecond = ValidateUInt(seconds, 0, 60, nameof(RecurrenceRule.BySecond));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYSECOND</c> rule part from a set of seconds, written in ascending order.
    /// </summary>
    /// <param name="seconds">The seconds to select.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="seconds" /> is empty.</exception>
    /// <remarks>
    /// A <see cref="SecondSet" /> holds the seconds 0 to 59, so the leap second 60, which <c>BYSECOND</c> also allows,
    /// is set through <see cref="BySecond(int[])" />.
    /// </remarks>
    public RecurrenceRuleBuilder BySecond(SecondSet seconds)
    {
        ThrowIfEmptySet(seconds.Count, nameof(RecurrenceRule.BySecond));

        _bySecond = [.. seconds];
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMINUTE</c> rule part.
    /// </summary>
    /// <param name="minutes">The minutes to select (0-59).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 0 to 59.</exception>
    public RecurrenceRuleBuilder ByMinute(params int[] minutes)
    {
        _byMinute = ValidateUInt(minutes, 0, 59, nameof(RecurrenceRule.ByMinute));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMINUTE</c> rule part from a set of minutes, written in ascending order.
    /// </summary>
    /// <param name="minutes">The minutes to select.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minutes" /> is empty.</exception>
    public RecurrenceRuleBuilder ByMinute(MinuteSet minutes)
    {
        ThrowIfEmptySet(minutes.Count, nameof(RecurrenceRule.ByMinute));

        _byMinute = [.. minutes];
        return this;
    }

    /// <summary>
    /// Sets the <c>BYHOUR</c> rule part.
    /// </summary>
    /// <param name="hours">The hours to select (0-23).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 0 to 23.</exception>
    public RecurrenceRuleBuilder ByHour(params int[] hours)
    {
        _byHour = ValidateUInt(hours, 0, 23, nameof(RecurrenceRule.ByHour));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYHOUR</c> rule part from a set of hours, written in ascending order.
    /// </summary>
    /// <param name="hours">The hours to select.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="hours" /> is empty.</exception>
    public RecurrenceRuleBuilder ByHour(HourSet hours)
    {
        ThrowIfEmptySet(hours.Count, nameof(RecurrenceRule.ByHour));

        _byHour = [.. hours];
        return this;
    }

    /// <summary>
    /// Sets the <c>BYDAY</c> rule part from weekday entries.
    /// </summary>
    /// <param name="days">The weekday entries to select.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="days" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when an entry's day is not a defined <see cref="DayOfWeek" /> value, or its ordinal is neither zero nor
    /// within ±1 to ±53.
    /// </exception>
    public RecurrenceRuleBuilder ByDay(params WeekDayNum[] days)
    {
        ThrowHelper.ThrowIfNull(days);
        foreach (WeekDayNum entry in days)
            ValidateWeekDay(entry.Day, entry.Ordinal, nameof(RecurrenceRule.ByDay));

        _byDay = (WeekDayNum[])days.Clone();
        return this;
    }

    /// <summary>
    /// Sets the <c>BYDAY</c> rule part from plain weekdays, each with no positional ordinal.
    /// </summary>
    /// <param name="days">The weekdays to select on every occurrence of that day within the period.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="days" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a day is not a defined <see cref="DayOfWeek" /> value.
    /// </exception>
    public RecurrenceRuleBuilder ByDay(params DayOfWeek[] days)
    {
        ThrowHelper.ThrowIfNull(days);
        foreach (DayOfWeek day in days)
            ValidateWeekDay(day, 0, nameof(RecurrenceRule.ByDay));

        _byDay = Array.ConvertAll(days, day => new WeekDayNum(0, day));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYDAY</c> rule part from a set of days of the week, each with no positional ordinal.
    /// </summary>
    /// <param name="days">The days of the week to select on every occurrence of the day within the period.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="days" /> is empty.</exception>
    /// <remarks>
    /// The days are written Monday first, the order RFC 5545's default week start gives them, so
    /// <see cref="DayOfWeekSet.Weekdays" /> builds <c>BYDAY=MO,TU,WE,TH,FR</c>.
    /// </remarks>
    public RecurrenceRuleBuilder ByDay(DayOfWeekSet days)
    {
        ThrowIfEmptySet(days.Count, nameof(RecurrenceRule.ByDay));

        var entries = new WeekDayNum[days.Count];
        int index = 0;
        for (int offset = 1; offset <= 7; offset++)
        {
            var day = (DayOfWeek)(offset % 7);
            if (days.Contains(day))
            {
                entries[index++] = new WeekDayNum(0, day);
            }
        }

        _byDay = entries;
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMONTHDAY</c> rule part.
    /// </summary>
    /// <param name="monthDays">The month days to select (1-31 or -31 to -1).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is zero or outside ±31.</exception>
    public RecurrenceRuleBuilder ByMonthDay(params int[] monthDays)
    {
        _byMonthDay = ValidateSigned(monthDays, 1, 31, nameof(RecurrenceRule.ByMonthDay));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMONTHDAY</c> rule part from a set of days of the month, written in ascending order.
    /// </summary>
    /// <param name="monthDays">The days of the month to select, counted from the first.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="monthDays" /> is empty.</exception>
    /// <remarks>
    /// A <see cref="DayOfMonthSet" /> holds the days 1 to 31, so days counted from the end of the month, such as -1 for
    /// the last day, are set through <see cref="ByMonthDay(int[])" />.
    /// </remarks>
    public RecurrenceRuleBuilder ByMonthDay(DayOfMonthSet monthDays)
    {
        ThrowIfEmptySet(monthDays.Count, nameof(RecurrenceRule.ByMonthDay));

        _byMonthDay = [.. monthDays];
        return this;
    }

    /// <summary>
    /// Sets the <c>BYYEARDAY</c> rule part.
    /// </summary>
    /// <param name="yearDays">The year days to select (1-366 or -366 to -1).</param>
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
    /// <param name="weekNumbers">The week numbers to select (1-53 or -53 to -1).</param>
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
    /// <param name="months">The months to select (1-12).</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the range 1 to 12.</exception>
    public RecurrenceRuleBuilder ByMonth(params int[] months)
    {
        _byMonth = ValidateUInt(months, 1, 12, nameof(RecurrenceRule.ByMonth));
        return this;
    }

    /// <summary>
    /// Sets the <c>BYMONTH</c> rule part from a set of months, written in ascending order.
    /// </summary>
    /// <param name="months">The months to select.</param>
    /// <returns>The same builder instance so calls can be chained.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="months" /> is empty.</exception>
    public RecurrenceRuleBuilder ByMonth(MonthSet months)
    {
        ThrowIfEmptySet(months.Count, nameof(RecurrenceRule.ByMonth));

        _byMonth = [.. months];
        return this;
    }

    /// <summary>
    /// Sets the <c>BYSETPOS</c> rule part.
    /// </summary>
    /// <param name="setPositions">The set positions to select (1-366 or -366 to -1).</param>
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
            _frequency,
            _interval,
            _count,
            _until,
            _weekStart,
            _bySecond,
            _byMinute,
            _byHour,
            _byDay,
            _byMonthDay,
            _byYearDay,
            _byWeekNo,
            _byMonth,
            _bySetPos);

    /// <summary>
    /// Rejects an empty set for a rule part: a rule part that is present must select at least one value.
    /// </summary>
    /// <param name="count">The number of values the set selects.</param>
    /// <param name="part">The rule-part name reported on failure.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="count" /> is zero.</exception>
    private static void ThrowIfEmptySet(int count, string part)
    {
        if (count == 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Arg_Invalid_RecurrenceRulePartEmptySet, part), part);
    }

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
        ThrowHelper.ThrowIfNull(values, part);
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
    /// Validates one <c>BYDAY</c> entry: a defined weekday with an ordinal of zero, which selects every occurrence of
    /// the day, or of ±1 to ±53.
    /// </summary>
    /// <param name="day">The entry's weekday.</param>
    /// <param name="ordinal">The entry's ordinal.</param>
    /// <param name="part">The rule-part name reported on failure.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="day" /> is not a defined <see cref="DayOfWeek" /> value or
    /// <paramref name="ordinal" /> is outside -53 to 53.
    /// </exception>
    private static void ValidateWeekDay(DayOfWeek day, int ordinal, string part)
    {
        if (day is < DayOfWeek.Sunday or > DayOfWeek.Saturday)
        {
            throw new ArgumentOutOfRangeException(
                part,
                day,
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Arg_OutOfRange_RecurrenceRulePart, part));
        }

        // The bounds are compared directly rather than through the magnitude, since Math.Abs throws for int.MinValue.
        if (ordinal is < -53 or > 53)
        {
            throw new ArgumentOutOfRangeException(
                part,
                ordinal,
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Arg_OutOfRange_RecurrenceRulePart, part));
        }
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
        ThrowHelper.ThrowIfNull(values, part);
        foreach (int value in values)
        {
            // The magnitude is taken in 64 bits, since Math.Abs throws for int.MinValue.
            long magnitude = Math.Abs((long)value);
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
