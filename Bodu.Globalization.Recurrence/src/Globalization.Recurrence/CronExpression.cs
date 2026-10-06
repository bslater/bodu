// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents a parsed Vixie-style cron expression and computes the occurrences that satisfy it.
/// </summary>
/// <remarks>
/// <para>
/// A cron expression matches instants whose second (in the six-field <see cref="CronFormat.WithSeconds" /> layout),
/// minute, hour, day-of-month, month, and day-of-week fields are each members of the corresponding field set. Each
/// field supports <c>*</c>, single values, ranges (<c>a-b</c>), steps (<c>*/n</c>, <c>a-b/n</c>), and comma-separated
/// lists, with three-letter month (<c>JAN</c>-<c>DEC</c>) and weekday (<c>SUN</c>-<c>SAT</c>) names. The macros
/// <c>@yearly</c> / <c>@annually</c>, <c>@monthly</c>, <c>@weekly</c>, <c>@daily</c> / <c>@midnight</c>, and
/// <c>@hourly</c> are also recognized.
/// </para>
/// <para>
/// The day fields also accept the Quartz tokens, each standing for the whole field. In the day-of-month field <c>L</c>
/// is the last day of the month and <c>L-n</c> the day n days before it, up to thirty; <c>LW</c> and <c>L-nW</c> are
/// the weekday nearest either; and <c>nW</c> is the weekday nearest the n-th, without leaving the month. In the
/// day-of-week field <c>dL</c> is the month's last weekday d and <c>d#k</c> its k-th, from one to five, with d a number
/// or a name as elsewhere in the field. Either day field accepts <c>?</c> for <c>*</c>. A token that names no day of a
/// month, such as <c>30W</c> in February or a fifth Tuesday a month lacks, selects nothing that month.
/// </para>
/// <para>
/// When both the day-of-month and day-of-week fields are restricted, an instant matches if it satisfies either field,
/// following the traditional Vixie cron rule; a token counts as a restriction and <c>?</c>, like <c>*</c>, does not.
/// Quartz itself would require the instant to satisfy both. Weekdays keep the Vixie numbering, in which both zero and
/// seven are Sunday, rather than the Quartz numbering from one.
/// </para>
/// <para>
/// Every occurrence answer is a pure function of the arguments: no API reads the wall clock or consults the machine
/// time zone. The <see cref="DateTimeOffset" /> overloads interpret the wall-clock time in the argument's own offset
/// and return occurrences carrying that offset; daylight-saving transitions are the caller's concern - a host that
/// wants a local-time schedule across a transition re-derives the offset on each evaluation.
/// </para>
/// <para>
/// Occurrence searches are bounded by a twelve-year horizon in each direction, which covers the largest possible gap
/// between occurrences of any satisfiable expression (a February 29th schedule crossing a non-leap century year); an
/// expression that can never match, such as February 30th, answers <see langword="null" /> at the horizon rather than
/// scanning unboundedly.
/// </para>
/// </remarks>
public sealed partial class CronExpression : IEquatable<CronExpression>
{
    /// <summary>The number of years the occurrence search scans before giving up. The largest gap between two consecutive occurrences of any satisfiable expression is eight years - a February 29th expression crossing a non-leap century year such as 2100 (2096 → 2104) - so twelve years covers every real schedule with margin while still bounding the search for an expression that can never match (for example February 30th).</summary>
    private const int SearchHorizonYears = 12;

    /// <summary>The matching seconds (every second for the five-field layout).</summary>
    private readonly SecondSet _seconds;

    /// <summary>The matching minutes.</summary>
    private readonly MinuteSet _minutes;

    /// <summary>The matching hours.</summary>
    private readonly HourSet _hours;

    /// <summary>The matching days of the month; empty when the field holds a Quartz token.</summary>
    private readonly DayOfMonthSet _daysOfMonth;

    /// <summary>The matching months.</summary>
    private readonly MonthSet _months;

    /// <summary>The matching days of the week; empty when the field holds a Quartz token.</summary>
    private readonly DayOfWeekSet _daysOfWeek;

    /// <summary>Indicates whether the day-of-month field is restricted (not <c>*</c>).</summary>
    private readonly bool _domRestricted;

    /// <summary>Indicates whether the day-of-week field is restricted (not <c>*</c>).</summary>
    private readonly bool _dowRestricted;

    /// <summary>The Quartz token the day-of-month field holds in place of values, or <see langword="null" />.</summary>
    private readonly DayOfMonthToken? _dayOfMonthToken;

    /// <summary>The Quartz token the day-of-week field holds in place of values, or <see langword="null" />.</summary>
    private readonly DayOfWeekToken? _dayOfWeekToken;

    /// <summary>Indicates whether either day field holds a Quartz token, which the searches must then consult.</summary>
    private readonly bool _hasDayToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronExpression" /> class from parsed field sets.
    /// </summary>
    /// <param name="format">The field layout the expression was parsed as.</param>
    /// <param name="seconds">The matching seconds set.</param>
    /// <param name="minutes">The matching minutes set.</param>
    /// <param name="hours">The matching hours set.</param>
    /// <param name="daysOfMonth">The matching day-of-month set.</param>
    /// <param name="months">The matching months set.</param>
    /// <param name="daysOfWeek">The matching day-of-week set.</param>
    /// <param name="domRestricted">Whether the day-of-month field is restricted.</param>
    /// <param name="dowRestricted">Whether the day-of-week field is restricted.</param>
    /// <param name="dayOfMonthToken">The Quartz token the day-of-month field holds, or <see langword="null" />.</param>
    /// <param name="dayOfWeekToken">The Quartz token the day-of-week field holds, or <see langword="null" />.</param>
    private CronExpression(
        CronFormat format,
        SecondSet seconds,
        MinuteSet minutes,
        HourSet hours,
        DayOfMonthSet daysOfMonth,
        MonthSet months,
        DayOfWeekSet daysOfWeek,
        bool domRestricted,
        bool dowRestricted,
        DayOfMonthToken? dayOfMonthToken,
        DayOfWeekToken? dayOfWeekToken)
    {
        Format = format;
        _seconds = seconds;
        _minutes = minutes;
        _hours = hours;
        _daysOfMonth = daysOfMonth;
        _months = months;
        _daysOfWeek = daysOfWeek;
        _domRestricted = domRestricted;
        _dowRestricted = dowRestricted;
        _dayOfMonthToken = dayOfMonthToken;
        _dayOfWeekToken = dayOfWeekToken;
        _hasDayToken = dayOfMonthToken is not null || dayOfWeekToken is not null;
    }

    /// <summary>
    /// Gets the field layout the expression was parsed as.
    /// </summary>
    /// <value>The cron field layout.</value>
    public CronFormat Format { get; }

    /// <summary>
    /// Returns the first instant matching the expression that falls after the specified instant.
    /// </summary>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>
    /// The next matching instant preserving the <see cref="DateTime.Kind" /> of <paramref name="after" />, or
    /// <see langword="null" /> when none occurs within the search horizon.
    /// </returns>
    public DateTime? GetNextOccurrence(DateTime after, bool inclusive = false)
    {
        if (_hasDayToken)
        {
            return FindNextWithTokens(after, inclusive);
        }

        DateTime candidate = Floor(after);
        bool satisfies = inclusive ? candidate >= after : candidate > after;
        if (!satisfies)
        {
            candidate = candidate.Add(Unit);
        }

        int guardYear = after.Year + SearchHorizonYears;
        while (candidate.Year <= guardYear)
        {
            if (!Selects(_months.ToUInt64(), candidate.Month - 1))
            {
                candidate = StartOfMonth(candidate).AddMonths(1);
                continue;
            }

            if (!DayMatches(candidate))
            {
                candidate = StartOfDay(candidate).AddDays(1);
                continue;
            }

            if (!Selects(_hours.ToUInt64(), candidate.Hour))
            {
                candidate = StartOfHour(candidate).AddHours(1);
                continue;
            }

            if (!Selects(_minutes.ToUInt64(), candidate.Minute))
            {
                candidate = StartOfMinute(candidate).AddMinutes(1);
                continue;
            }

            if (Format == CronFormat.WithSeconds && !Selects(_seconds.ToUInt64(), candidate.Second))
            {
                candidate = candidate.AddSeconds(1);
                continue;
            }

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Returns the last instant matching the expression that falls before the specified instant.
    /// </summary>
    /// <param name="before">The instant the returned occurrence must precede.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="before" />; otherwise the
    /// occurrence must be strictly earlier.
    /// </param>
    /// <returns>
    /// The previous matching instant preserving the <see cref="DateTime.Kind" /> of <paramref name="before" />, or
    /// <see langword="null" /> when none occurs within the search horizon.
    /// </returns>
    public DateTime? GetPreviousOccurrence(DateTime before, bool inclusive = false)
    {
        if (_hasDayToken)
        {
            return FindPreviousWithTokens(before, inclusive);
        }

        DateTime candidate = Floor(before);
        TimeSpan unit = Unit;
        if (!inclusive && candidate == before)
        {
            candidate = candidate.Subtract(unit);
        }

        int guardYear = before.Year - SearchHorizonYears;
        while (candidate.Year >= guardYear)
        {
            if (!Selects(_months.ToUInt64(), candidate.Month - 1))
            {
                candidate = StartOfMonth(candidate).Subtract(unit);
                continue;
            }

            if (!DayMatches(candidate))
            {
                candidate = StartOfDay(candidate).Subtract(unit);
                continue;
            }

            if (!Selects(_hours.ToUInt64(), candidate.Hour))
            {
                candidate = StartOfHour(candidate).Subtract(unit);
                continue;
            }

            if (!Selects(_minutes.ToUInt64(), candidate.Minute))
            {
                candidate = StartOfMinute(candidate).Subtract(unit);
                continue;
            }

            if (Format == CronFormat.WithSeconds && !Selects(_seconds.ToUInt64(), candidate.Second))
            {
                candidate = candidate.AddSeconds(-1);
                continue;
            }

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Returns the first instant matching the expression that falls after the specified instant, preserving its offset.
    /// </summary>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>
    /// The next matching instant carrying the offset of <paramref name="after" />, or <see langword="null" />.
    /// </returns>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset after, bool inclusive = false)
    {
        DateTime? next = GetNextOccurrence(DateTime.SpecifyKind(after.DateTime, DateTimeKind.Unspecified), inclusive);
        return next is null ? null : new DateTimeOffset(next.Value, after.Offset);
    }

    /// <summary>
    /// Returns the last instant matching the expression that falls before the specified instant, preserving its offset.
    /// </summary>
    /// <param name="before">The instant the returned occurrence must precede.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="before" />; otherwise the
    /// occurrence must be strictly earlier.
    /// </param>
    /// <returns>
    /// The previous matching instant carrying the offset of <paramref name="before" />, or <see langword="null" />.
    /// </returns>
    public DateTimeOffset? GetPreviousOccurrence(DateTimeOffset before, bool inclusive = false)
    {
        DateTime? previous = GetPreviousOccurrence(DateTime.SpecifyKind(before.DateTime, DateTimeKind.Unspecified), inclusive);
        return previous is null ? null : new DateTimeOffset(previous.Value, before.Offset);
    }

    /// <summary>
    /// Determines whether this expression matches the same instants as another expression.
    /// </summary>
    /// <param name="other">The expression to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="other" /> is non-null and every field set, together with the
    /// day-field combination mode and any Quartz day token, is equal; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The day fields' restricted-ness participates through <see cref="DaysCombineByUnion" />, because that is what
    /// selects the union or intersection branch in <see cref="DayMatches" />: <c>0 0 */2 * MON</c> and
    /// <c>0 0 1-31/2 * MON</c> carry identical field sets yet select instants two weeks apart, so they are not equal
    /// values.
    /// </para>
    /// <para>
    /// Only the combination mode is compared, not each flag, because a single restricted day field is not observable:
    /// <c>* * 1-31 * *</c> restricts the day-of-month while <c>* * * * *</c> does not, yet both fall to the
    /// intersection branch and select every day, so the two remain equal.
    /// </para>
    /// </remarks>
    public bool Equals(CronExpression? other) =>
        other is not null
        && Format == other.Format
        && DaysCombineByUnion == other.DaysCombineByUnion
        && _dayOfMonthToken == other._dayOfMonthToken
        && _dayOfWeekToken == other._dayOfWeekToken
        && _seconds == other._seconds
        && _minutes == other._minutes
        && _hours == other._hours
        && _daysOfMonth == other._daysOfMonth
        && _months == other._months
        && _daysOfWeek == other._daysOfWeek;

    /// <summary>
    /// Determines whether this expression is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is a <see cref="CronExpression" /> equal to this instance;
    /// otherwise <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        Equals(obj as CronExpression);

    /// <summary>
    /// Returns a hash code for the expression.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(CronExpression)" />.</returns>
    /// <remarks>
    /// Every field set contributes its values, matching the fields <see cref="Equals(CronExpression)" /> compares, as
    /// does the day-field combination mode. Mixing only each field's count would satisfy the equality contract but
    /// collapse the common case - a schedule selecting one value per field, such as <c>0 2 * * *</c> - onto a single
    /// bucket.
    /// </remarks>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Format);
        hash.Add(DaysCombineByUnion);
        hash.Add(_dayOfMonthToken);
        hash.Add(_dayOfWeekToken);
        hash.Add(_seconds);
        hash.Add(_minutes);
        hash.Add(_hours);
        hash.Add(_daysOfMonth);
        hash.Add(_months);
        hash.Add(_daysOfWeek);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Gets the resolution unit of the expression: one second for the six-field layout, otherwise one minute.
    /// </summary>
    /// <value>The stepping unit.</value>
    private TimeSpan Unit =>
        Format == CronFormat.WithSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets a value indicating whether the two day fields combine by union rather than by intersection.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when both day fields are restricted, in which case a day matches if it satisfies either
    /// field; otherwise <see langword="false" />, in which case it must satisfy both.
    /// </value>
    /// <remarks>
    /// This is the only way either restriction flag is observable, so it - rather than the two flags - is what
    /// <see cref="Equals(CronExpression)" /> compares and <see cref="GetHashCode" /> mixes.
    /// </remarks>
    private bool DaysCombineByUnion =>
        _domRestricted && _dowRestricted;

    /// <summary>
    /// Determines whether a calendar value set's bitmap selects the value at an offset in the set's domain.
    /// </summary>
    /// <param name="bitmap">
    /// The set's bitmap, from its <c>ToUInt64</c> method: bit n stands for the domain's least value plus n.
    /// </param>
    /// <param name="offset">The value's offset from the least value of the set's domain.</param>
    /// <returns>
    /// <see langword="true" /> when the bitmap selects the value; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The searches test the fields of every candidate instant, and a candidate's month, day, weekday, hour, minute and
    /// second always lie in their sets' domains. Testing the bitmaps directly drops the range check that a set's
    /// <c>Contains</c> makes for an arbitrary value; with it, the searches for an expression without a token ran up to
    /// 6% slower than they had over the boolean masks the sets replaced.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Selects(ulong bitmap, int offset) =>
        ((bitmap >> offset) & 1) != 0;

    /// <summary>
    /// Determines whether the day component of <paramref name="candidate" /> matches the day-of-month and day-of-week
    /// field sets under the Vixie combination rule.
    /// </summary>
    /// <param name="candidate">The instant to test.</param>
    /// <returns><see langword="true" /> when the day matches; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// Both search loops test the day on every step, so the method is kept inlined into them: left as a call, it cost
    /// an expression without a token up to a tenth of its query time. It reads the two bitmaps for the reason
    /// <see cref="Selects" /> gives, and combines their bits without branching on either.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool DayMatches(DateTime candidate)
    {
        // Bit n of each bitmap stands for its domain's least value plus n, so bit 0 is the 1st of the month or Sunday.
        ulong domMatch = _daysOfMonth.ToUInt64() >> (candidate.Day - 1);
        ulong dowMatch = _daysOfWeek.ToUInt64() >> (int)candidate.DayOfWeek;

        // Both field sets always apply; the restriction flags select only how they combine. Vixie takes the union
        // when neither day field begins with '*', and the intersection otherwise - so a stepped star such as "*/2"
        // still narrows the days it matches even though it does not make the field "restricted".
        return ((DaysCombineByUnion ? domMatch | dowMatch : domMatch & dowMatch) & 1) != 0;
    }

    /// <summary>
    /// Determines whether the day component of <paramref name="candidate" /> matches day fields at least one of which
    /// holds a Quartz token, under the Vixie combination rule.
    /// </summary>
    /// <param name="candidate">The instant to test.</param>
    /// <returns><see langword="true" /> when the day matches; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// A field that holds a token decides the day through it; the other field, if it holds values, through its set.
    /// </remarks>
    private bool TokenDayMatches(DateTime candidate)
    {
        bool domMatch = _dayOfMonthToken is { } dayOfMonthToken
            ? dayOfMonthToken.Matches(candidate)
            : Selects(_daysOfMonth.ToUInt64(), candidate.Day - 1);
        bool dowMatch = _dayOfWeekToken is { } dayOfWeekToken
            ? dayOfWeekToken.Matches(candidate)
            : Selects(_daysOfWeek.ToUInt64(), (int)candidate.DayOfWeek);

        return DaysCombineByUnion
            ? domMatch || dowMatch
            : domMatch && dowMatch;
    }

    /// <summary>
    /// Returns the first instant matching an expression that holds a Quartz day token and falls after the specified
    /// instant.
    /// </summary>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">Whether an occurrence equal to <paramref name="after" /> counts.</param>
    /// <returns>
    /// The next matching instant, or <see langword="null" /> when none occurs within the search horizon.
    /// </returns>
    /// <remarks>
    /// This is the search in <see cref="GetNextOccurrence(DateTime, bool)" /> with days tested by
    /// <see cref="TokenDayMatches" />. It is kept out of that method's loop, which tests the day on every step, minutes
    /// included: a call left in the loop, even one never made, cost an expression without a token a fifth of its query
    /// time.
    /// </remarks>
    private DateTime? FindNextWithTokens(DateTime after, bool inclusive)
    {
        DateTime candidate = Floor(after);
        bool satisfies = inclusive ? candidate >= after : candidate > after;
        if (!satisfies)
        {
            candidate = candidate.Add(Unit);
        }

        int guardYear = after.Year + SearchHorizonYears;
        while (candidate.Year <= guardYear)
        {
            if (!Selects(_months.ToUInt64(), candidate.Month - 1))
            {
                candidate = StartOfMonth(candidate).AddMonths(1);
                continue;
            }

            if (!TokenDayMatches(candidate))
            {
                candidate = StartOfDay(candidate).AddDays(1);
                continue;
            }

            if (!Selects(_hours.ToUInt64(), candidate.Hour))
            {
                candidate = StartOfHour(candidate).AddHours(1);
                continue;
            }

            if (!Selects(_minutes.ToUInt64(), candidate.Minute))
            {
                candidate = StartOfMinute(candidate).AddMinutes(1);
                continue;
            }

            if (Format == CronFormat.WithSeconds && !Selects(_seconds.ToUInt64(), candidate.Second))
            {
                candidate = candidate.AddSeconds(1);
                continue;
            }

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Returns the last instant matching an expression that holds a Quartz day token and falls before the specified
    /// instant.
    /// </summary>
    /// <param name="before">The instant the returned occurrence must precede.</param>
    /// <param name="inclusive">Whether an occurrence equal to <paramref name="before" /> counts.</param>
    /// <returns>
    /// The previous matching instant, or <see langword="null" /> when none occurs within the search horizon.
    /// </returns>
    /// <remarks>
    /// This is the search in <see cref="GetPreviousOccurrence(DateTime, bool)" /> with days tested by
    /// <see cref="TokenDayMatches" />, kept apart for the reason <see cref="FindNextWithTokens" /> gives.
    /// </remarks>
    private DateTime? FindPreviousWithTokens(DateTime before, bool inclusive)
    {
        DateTime candidate = Floor(before);
        TimeSpan unit = Unit;
        if (!inclusive && candidate == before)
        {
            candidate = candidate.Subtract(unit);
        }

        int guardYear = before.Year - SearchHorizonYears;
        while (candidate.Year >= guardYear)
        {
            if (!Selects(_months.ToUInt64(), candidate.Month - 1))
            {
                candidate = StartOfMonth(candidate).Subtract(unit);
                continue;
            }

            if (!TokenDayMatches(candidate))
            {
                candidate = StartOfDay(candidate).Subtract(unit);
                continue;
            }

            if (!Selects(_hours.ToUInt64(), candidate.Hour))
            {
                candidate = StartOfHour(candidate).Subtract(unit);
                continue;
            }

            if (!Selects(_minutes.ToUInt64(), candidate.Minute))
            {
                candidate = StartOfMinute(candidate).Subtract(unit);
                continue;
            }

            if (Format == CronFormat.WithSeconds && !Selects(_seconds.ToUInt64(), candidate.Second))
            {
                candidate = candidate.AddSeconds(-1);
                continue;
            }

            return candidate;
        }

        return null;
    }

    /// <summary>
    /// Truncates an instant to the expression's resolution.
    /// </summary>
    /// <param name="value">The instant to truncate.</param>
    /// <returns>The instant with sub-resolution components zeroed.</returns>
    private DateTime Floor(DateTime value) =>
        Format == CronFormat.WithSeconds
            ? new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind)
            : new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);

    /// <summary>
    /// Returns the first instant of the month containing <paramref name="value" />.
    /// </summary>
    /// <param name="value">A date within the target month.</param>
    /// <returns>The first instant of the month.</returns>
    private static DateTime StartOfMonth(DateTime value) =>
        new(value.Year, value.Month, 1, 0, 0, 0, value.Kind);

    /// <summary>
    /// Returns midnight of the day containing <paramref name="value" />.
    /// </summary>
    /// <param name="value">A date within the target day.</param>
    /// <returns>Midnight of the day.</returns>
    private static DateTime StartOfDay(DateTime value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, value.Kind);

    /// <summary>
    /// Returns the start of the hour containing <paramref name="value" />.
    /// </summary>
    /// <param name="value">An instant within the target hour.</param>
    /// <returns>The start of the hour.</returns>
    private static DateTime StartOfHour(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Kind);

    /// <summary>
    /// Returns the start of the minute containing <paramref name="value" />.
    /// </summary>
    /// <param name="value">An instant within the target minute.</param>
    /// <returns>The start of the minute.</returns>
    private static DateTime StartOfMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
}
