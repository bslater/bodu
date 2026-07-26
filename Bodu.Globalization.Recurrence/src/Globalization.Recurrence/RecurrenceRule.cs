// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents an immutable RFC 5545 recurrence rule (<c>RRULE</c>): a base <see cref="Frequency" /> refined by an
/// interval, an optional bound (<see cref="Count" /> or <see cref="Until" />), and the <c>BY</c> rule parts, from which
/// the set of recurring instants relative to a start date is derived.
/// </summary>
/// <remarks>
/// <para>
/// A rule is constructed by parsing its textual form with <see cref="Parse(string)" /> or fluently with
/// <see cref="RecurrenceRuleBuilder" />, and is expanded into concrete occurrences by the <c>GetOccurrences</c> and
/// <c>GetNextOccurrence</c> methods, which take the series start date as their anchor. The rule itself carries no start
/// date; the same rule can be applied to different starts.
/// </para>
/// <para>
/// Occurrence enumeration currently supports the <see cref="RecurrenceFrequency.Daily" />,
/// <see cref="RecurrenceFrequency.Weekly" />, <see cref="RecurrenceFrequency.Monthly" />, and
/// <see cref="RecurrenceFrequency.Yearly" /> frequencies. A rule with a sub-daily frequency still parses and
/// round-trips, but enumerating it throws <see cref="NotSupportedException" /> until that follow-on lands.
/// </para>
/// </remarks>
/// <seealso cref="RecurrenceRuleBuilder" /> <seealso cref="RecurrenceSet" />
public sealed partial class RecurrenceRule : IEquatable<RecurrenceRule>
{
    /// <summary>The shared empty integer array used for absent rule parts.</summary>
    private static readonly int[] s_empty = [];

    /// <summary>The seconds selected by the <c>BYSECOND</c> rule part.</summary>
    private readonly int[] _bySecond;

    /// <summary>The minutes selected by the <c>BYMINUTE</c> rule part.</summary>
    private readonly int[] _byMinute;

    /// <summary>The hours selected by the <c>BYHOUR</c> rule part.</summary>
    private readonly int[] _byHour;

    /// <summary>The weekday entries selected by the <c>BYDAY</c> rule part.</summary>
    private readonly WeekDayNum[] _byDay;

    /// <summary>The month days selected by the <c>BYMONTHDAY</c> rule part.</summary>
    private readonly int[] _byMonthDay;

    /// <summary>The year days selected by the <c>BYYEARDAY</c> rule part.</summary>
    private readonly int[] _byYearDay;

    /// <summary>The week numbers selected by the <c>BYWEEKNO</c> rule part.</summary>
    private readonly int[] _byWeekNo;

    /// <summary>The months selected by the <c>BYMONTH</c> rule part.</summary>
    private readonly int[] _byMonth;

    /// <summary>The set positions selected by the <c>BYSETPOS</c> rule part.</summary>
    private readonly int[] _bySetPos;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurrenceRule" /> class from validated component values.
    /// </summary>
    /// <param name="frequency">The base recurrence period.</param>
    /// <param name="interval">The positive multiple of <paramref name="frequency" /> between occurrences.</param>
    /// <param name="count">
    /// The maximum number of occurrences, or <see langword="null" /> when unbounded by count.
    /// </param>
    /// <param name="until">
    /// The inclusive upper-bound instant, or <see langword="null" /> when unbounded by date.
    /// </param>
    /// <param name="weekStart">
    /// The day the week starts on, governing weekly interval and week-number arithmetic.
    /// </param>
    /// <param name="bySecond">The seconds filter, or an empty span when unset.</param>
    /// <param name="byMinute">The minutes filter, or an empty span when unset.</param>
    /// <param name="byHour">The hours filter, or an empty span when unset.</param>
    /// <param name="byDay">The weekday filter, or an empty span when unset.</param>
    /// <param name="byMonthDay">The month-day filter, or an empty span when unset.</param>
    /// <param name="byYearDay">The year-day filter, or an empty span when unset.</param>
    /// <param name="byWeekNo">The week-number filter, or an empty span when unset.</param>
    /// <param name="byMonth">The month filter, or an empty span when unset.</param>
    /// <param name="bySetPos">The set-position selector, or an empty span when unset.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="interval" /> is less than one, or <paramref name="count" /> is specified and less
    /// than one.
    /// </exception>
    internal RecurrenceRule(
        RecurrenceFrequency frequency,
        int interval,
        int? count,
        DateTime? until,
        DayOfWeek weekStart,
        ReadOnlySpan<int> bySecond,
        ReadOnlySpan<int> byMinute,
        ReadOnlySpan<int> byHour,
        ReadOnlySpan<WeekDayNum> byDay,
        ReadOnlySpan<int> byMonthDay,
        ReadOnlySpan<int> byYearDay,
        ReadOnlySpan<int> byWeekNo,
        ReadOnlySpan<int> byMonth,
        ReadOnlySpan<int> bySetPos)
    {
        RecurrenceThrowHelper.ThrowIfInvalidInterval(interval);
        RecurrenceThrowHelper.ThrowIfInvalidCount(count);

        Frequency = frequency;
        Interval = interval;
        Count = count;
        Until = until;
        WeekStart = weekStart;

        _bySecond = bySecond.IsEmpty ? s_empty : bySecond.ToArray();
        _byMinute = byMinute.IsEmpty ? s_empty : byMinute.ToArray();
        _byHour = byHour.IsEmpty ? s_empty : byHour.ToArray();
        _byDay = byDay.IsEmpty ? [] : byDay.ToArray();
        _byMonthDay = byMonthDay.IsEmpty ? s_empty : byMonthDay.ToArray();
        _byYearDay = byYearDay.IsEmpty ? s_empty : byYearDay.ToArray();
        _byWeekNo = byWeekNo.IsEmpty ? s_empty : byWeekNo.ToArray();
        _byMonth = byMonth.IsEmpty ? s_empty : byMonth.ToArray();
        _bySetPos = bySetPos.IsEmpty ? s_empty : bySetPos.ToArray();
    }

    /// <summary>
    /// Gets the base period at which the rule repeats.
    /// </summary>
    /// <value>The recurrence frequency, corresponding to the <c>FREQ</c> rule part.</value>
    public RecurrenceFrequency Frequency { get; }

    /// <summary>
    /// Gets the positive multiple of <see cref="Frequency" /> between successive occurrences.
    /// </summary>
    /// <value>The recurrence interval; <c>1</c> when the <c>INTERVAL</c> rule part is absent.</value>
    public int Interval { get; }

    /// <summary>
    /// Gets the maximum number of occurrences the rule produces.
    /// </summary>
    /// <value>
    /// The occurrence count from the <c>COUNT</c> rule part, or <see langword="null" /> when unbounded by count.
    /// </value>
    public int? Count { get; }

    /// <summary>
    /// Gets the inclusive upper bound beyond which no occurrence is produced.
    /// </summary>
    /// <value>The instant from the <c>UNTIL</c> rule part, or <see langword="null" /> when unbounded by date.</value>
    public DateTime? Until { get; }

    /// <summary>
    /// Gets the day on which the week starts for weekly-interval and week-number calculations.
    /// </summary>
    /// <value>The week-start day from the <c>WKST</c> rule part; <see cref="DayOfWeek.Monday" /> when absent.</value>
    public DayOfWeek WeekStart { get; }

    /// <summary>
    /// Gets the seconds selected by the <c>BYSECOND</c> rule part.
    /// </summary>
    /// <value>The selected seconds in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> BySecond => _bySecond;

    /// <summary>
    /// Gets the minutes selected by the <c>BYMINUTE</c> rule part.
    /// </summary>
    /// <value>The selected minutes in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> ByMinute => _byMinute;

    /// <summary>
    /// Gets the hours selected by the <c>BYHOUR</c> rule part.
    /// </summary>
    /// <value>The selected hours in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> ByHour => _byHour;

    /// <summary>
    /// Gets the weekday entries selected by the <c>BYDAY</c> rule part.
    /// </summary>
    /// <value>The selected weekday entries in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<WeekDayNum> ByDay => _byDay;

    /// <summary>
    /// Gets the month days selected by the <c>BYMONTHDAY</c> rule part.
    /// </summary>
    /// <value>The selected month days in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> ByMonthDay => _byMonthDay;

    /// <summary>
    /// Gets the year days selected by the <c>BYYEARDAY</c> rule part.
    /// </summary>
    /// <value>The selected year days in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> ByYearDay => _byYearDay;

    /// <summary>
    /// Gets the week numbers selected by the <c>BYWEEKNO</c> rule part.
    /// </summary>
    /// <value>The selected week numbers in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> ByWeekNo => _byWeekNo;

    /// <summary>
    /// Gets the months selected by the <c>BYMONTH</c> rule part.
    /// </summary>
    /// <value>The selected months in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> ByMonth => _byMonth;

    /// <summary>
    /// Gets the set positions selected by the <c>BYSETPOS</c> rule part.
    /// </summary>
    /// <value>The selected set positions in source order, or an empty list when the rule part is absent.</value>
    public IReadOnlyList<int> BySetPos => _bySetPos;

    /// <summary>
    /// Gets a value indicating whether the rule uses a sub-daily frequency whose occurrence enumeration is not yet
    /// supported.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="Frequency" /> is below <see cref="RecurrenceFrequency.Daily" />;
    /// otherwise <see langword="false" />.
    /// </value>
    internal bool IsSubDaily =>
        Frequency < RecurrenceFrequency.Daily;

    /// <summary>
    /// Determines whether this rule is equal to another rule by comparing every component.
    /// </summary>
    /// <param name="other">The rule to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="other" /> is non-null and every rule component is equal; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(RecurrenceRule? other) =>
        other is not null
        && Frequency == other.Frequency
        && Interval == other.Interval
        && Count == other.Count
        && Nullable.Equals(Until, other.Until)
        && WeekStart == other.WeekStart
        && _bySecond.AsSpan().SequenceEqual(other._bySecond)
        && _byMinute.AsSpan().SequenceEqual(other._byMinute)
        && _byHour.AsSpan().SequenceEqual(other._byHour)
        && _byDay.AsSpan().SequenceEqual(other._byDay)
        && _byMonthDay.AsSpan().SequenceEqual(other._byMonthDay)
        && _byYearDay.AsSpan().SequenceEqual(other._byYearDay)
        && _byWeekNo.AsSpan().SequenceEqual(other._byWeekNo)
        && _byMonth.AsSpan().SequenceEqual(other._byMonth)
        && _bySetPos.AsSpan().SequenceEqual(other._bySetPos);

    /// <summary>
    /// Determines whether this rule is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is a <see cref="RecurrenceRule" /> equal to this instance;
    /// otherwise <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        Equals(obj as RecurrenceRule);

    /// <summary>
    /// Returns a hash code derived from the rule's scalar components.
    /// </summary>
    /// <returns>A hash code for the current rule.</returns>
    /// <remarks>
    /// The hash combines the scalar components (<see cref="Frequency" />, <see cref="Interval" />, <see cref="Count" />,
    /// <see cref="Until" />, <see cref="WeekStart" />) and the rule-part lengths, which is a valid hash consistent with
    /// <see cref="Equals(RecurrenceRule)" /> while avoiding per-element enumeration.
    /// </remarks>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Frequency);
        hash.Add(Interval);
        hash.Add(Count);
        hash.Add(Until);
        hash.Add(WeekStart);
        hash.Add(_byDay.Length);
        hash.Add(_byMonthDay.Length);
        hash.Add(_byMonth.Length);
        hash.Add(_bySetPos.Length);
        return hash.ToHashCode();
    }
}
