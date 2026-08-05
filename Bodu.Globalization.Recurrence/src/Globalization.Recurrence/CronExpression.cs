// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents a parsed Vixie-style cron expression and computes the occurrences that satisfy it.
/// </summary>
/// <remarks>
/// <para>
/// A cron expression matches instants whose second (in the six-field <see cref="CronFormat.WithSeconds" /> layout),
/// minute, hour, day-of-month, month, and day-of-week fields are each members of the corresponding field set. Each
/// field supports <c>*</c>, single values, ranges (<c>a-b</c>), steps (<c>*/n</c>, <c>a-b/n</c>), and comma-separated
/// lists, with three-letter month (<c>JAN</c>–<c>DEC</c>) and weekday (<c>SUN</c>–<c>SAT</c>) names. The macros
/// <c>@yearly</c> / <c>@annually</c>, <c>@monthly</c>, <c>@weekly</c>, <c>@daily</c> / <c>@midnight</c>, and
/// <c>@hourly</c> are also recognized.
/// </para>
/// <para>
/// When both the day-of-month and day-of-week fields are restricted, an instant matches if it satisfies either field,
/// following the traditional Vixie cron rule. The Quartz extensions <c>L</c>, <c>W</c>, <c>#</c>, and <c>?</c> are not
/// yet supported.
/// </para>
/// <para>
/// Every occurrence answer is a pure function of the arguments: no API reads the wall clock or consults the machine
/// time zone. The <see cref="DateTimeOffset" /> overloads interpret the wall-clock time in the argument's own offset
/// and return occurrences carrying that offset; daylight-saving transitions are the caller's concern — a host that
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
    /// <summary>The number of years the occurrence search scans before giving up. The largest gap between two consecutive occurrences of any satisfiable expression is eight years — a February 29th expression crossing a non-leap century year such as 2100 (2096 → 2104) — so twelve years covers every real schedule with margin while still bounding the search for an expression that can never match (for example February 30th).</summary>
    private const int SearchHorizonYears = 12;

    /// <summary>The set of matching seconds, indexed 0–59 (all set for the five-field layout).</summary>
    private readonly bool[] _seconds;

    /// <summary>The set of matching minutes, indexed 0–59.</summary>
    private readonly bool[] _minutes;

    /// <summary>The set of matching hours, indexed 0–23.</summary>
    private readonly bool[] _hours;

    /// <summary>The set of matching days of the month, indexed 1–31.</summary>
    private readonly bool[] _daysOfMonth;

    /// <summary>The set of matching months, indexed 1–12.</summary>
    private readonly bool[] _months;

    /// <summary>The set of matching days of the week, indexed 0 (Sunday) – 6 (Saturday).</summary>
    private readonly bool[] _daysOfWeek;

    /// <summary>Indicates whether the day-of-month field is restricted (not <c>*</c>).</summary>
    private readonly bool _domRestricted;

    /// <summary>Indicates whether the day-of-week field is restricted (not <c>*</c>).</summary>
    private readonly bool _dowRestricted;

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
    private CronExpression(
        CronFormat format,
        bool[] seconds,
        bool[] minutes,
        bool[] hours,
        bool[] daysOfMonth,
        bool[] months,
        bool[] daysOfWeek,
        bool domRestricted,
        bool dowRestricted)
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
        DateTime candidate = Floor(after);
        bool satisfies = inclusive ? candidate >= after : candidate > after;
        if (!satisfies)
        {
            candidate = candidate.Add(Unit);
        }

        int guardYear = after.Year + SearchHorizonYears;
        while (candidate.Year <= guardYear)
        {
            if (!_months[candidate.Month])
            {
                candidate = StartOfMonth(candidate).AddMonths(1);
                continue;
            }

            if (!DayMatches(candidate))
            {
                candidate = StartOfDay(candidate).AddDays(1);
                continue;
            }

            if (!_hours[candidate.Hour])
            {
                candidate = StartOfHour(candidate).AddHours(1);
                continue;
            }

            if (!_minutes[candidate.Minute])
            {
                candidate = StartOfMinute(candidate).AddMinutes(1);
                continue;
            }

            if (Format == CronFormat.WithSeconds && !_seconds[candidate.Second])
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
        DateTime candidate = Floor(before);
        if (!inclusive && candidate == before)
        {
            candidate = candidate.Subtract(Unit);
        }

        int guardYear = before.Year - SearchHorizonYears;
        while (candidate.Year >= guardYear)
        {
            if (!_months[candidate.Month])
            {
                candidate = StartOfMonth(candidate).Subtract(Unit);
                continue;
            }

            if (!DayMatches(candidate))
            {
                candidate = StartOfDay(candidate).Subtract(Unit);
                continue;
            }

            if (!_hours[candidate.Hour])
            {
                candidate = StartOfHour(candidate).Subtract(Unit);
                continue;
            }

            if (!_minutes[candidate.Minute])
            {
                candidate = StartOfMinute(candidate).Subtract(Unit);
                continue;
            }

            if (Format == CronFormat.WithSeconds && !_seconds[candidate.Second])
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
    /// <see langword="true" /> when <paramref name="other" /> is non-null and every field set is equal; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(CronExpression? other) =>
        other is not null
        && Format == other.Format
        && _seconds.AsSpan().SequenceEqual(other._seconds)
        && _minutes.AsSpan().SequenceEqual(other._minutes)
        && _hours.AsSpan().SequenceEqual(other._hours)
        && _daysOfMonth.AsSpan().SequenceEqual(other._daysOfMonth)
        && _months.AsSpan().SequenceEqual(other._months)
        && _daysOfWeek.AsSpan().SequenceEqual(other._daysOfWeek);

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
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(Format);
        hash.Add(CountSet(_minutes));
        hash.Add(CountSet(_hours));
        hash.Add(CountSet(_daysOfMonth));
        hash.Add(CountSet(_months));
        hash.Add(CountSet(_daysOfWeek));
        return hash.ToHashCode();
    }

    /// <summary>
    /// Gets the resolution unit of the expression: one second for the six-field layout, otherwise one minute.
    /// </summary>
    /// <value>The stepping unit.</value>
    private TimeSpan Unit =>
        Format == CronFormat.WithSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);

    /// <summary>
    /// Counts the set members of a field mask.
    /// </summary>
    /// <param name="mask">The field mask.</param>
    /// <returns>The number of set entries.</returns>
    private static int CountSet(bool[] mask)
    {
        int count = 0;
        foreach (bool bit in mask)
        {
            if (bit)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Determines whether the day component of <paramref name="candidate" /> matches the day-of-month and day-of-week
    /// fields under the Vixie combination rule.
    /// </summary>
    /// <param name="candidate">The instant to test.</param>
    /// <returns><see langword="true" /> when the day matches; otherwise <see langword="false" />.</returns>
    private bool DayMatches(DateTime candidate)
    {
        bool domMatch = _daysOfMonth[candidate.Day];
        bool dowMatch = _daysOfWeek[(int)candidate.DayOfWeek];

        // Both field masks always apply; the restriction flags select only how they combine. Vixie takes the union
        // when neither day field begins with '*', and the intersection otherwise — so a stepped star such as "*/2"
        // still narrows the days it matches even though it does not make the field "restricted".
        return _domRestricted && _dowRestricted
            ? domMatch || dowMatch
            : domMatch && dowMatch;
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
