// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronTokenReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Provides a literal reading of cron schedules that use the Quartz day tokens, kept as the oracle
/// <see cref="CronExpression" /> is held to. It lists each month's days and picks from them as each token's definition
/// reads, then walks the calendar a day at a time, with none of the engine's arithmetic or skips.
/// </summary>
/// <remarks>
/// The weekday nearest a day is read as the weekday of the same month at the least distance from it, which is the
/// Quartz rule stated another way: a Saturday moves to the Friday before it unless that leaves the month, and a Sunday
/// to the Monday after it unless that leaves the month. The two day fields combine as the engine documents: by union
/// when both are restricted, where a token is a restriction and <c>*</c> or <c>?</c> is not.
/// </remarks>
internal sealed class CronTokenReference
{
    /// <summary>The number of years the reference searches in each direction, the engine's own horizon.</summary>
    private const int HorizonYears = 12;

    /// <summary>The days each month selects, computed once per month.</summary>
    private readonly Dictionary<(int Year, int Month), HashSet<int>> _days = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CronTokenReference" /> class.
    /// </summary>
    /// <param name="seconds">The matching seconds, ascending.</param>
    /// <param name="minutes">The matching minutes, ascending.</param>
    /// <param name="hours">The matching hours, ascending.</param>
    /// <param name="months">The matching months.</param>
    /// <param name="dayOfMonth">The day-of-month field text: <c>*</c>, <c>?</c>, a token, or a list of days.</param>
    /// <param name="dayOfWeek">The day-of-week field text: <c>*</c>, <c>?</c>, a token, or a list of weekdays.</param>
    public CronTokenReference(int[] seconds, int[] minutes, int[] hours, int[] months, string dayOfMonth, string dayOfWeek)
    {
        Seconds = seconds;
        Minutes = minutes;
        Hours = hours;
        Months = [.. months];
        DayOfMonth = dayOfMonth;
        DayOfWeek = dayOfWeek;
    }

    /// <summary>Gets the matching seconds, ascending.</summary>
    /// <value>The seconds.</value>
    public int[] Seconds { get; }

    /// <summary>Gets the matching minutes, ascending.</summary>
    /// <value>The minutes.</value>
    public int[] Minutes { get; }

    /// <summary>Gets the matching hours, ascending.</summary>
    /// <value>The hours.</value>
    public int[] Hours { get; }

    /// <summary>Gets the matching months.</summary>
    /// <value>The months.</value>
    public HashSet<int> Months { get; }

    /// <summary>Gets the day-of-month field text.</summary>
    /// <value>The field text.</value>
    public string DayOfMonth { get; }

    /// <summary>Gets the day-of-week field text.</summary>
    /// <value>The field text.</value>
    public string DayOfWeek { get; }

    /// <summary>
    /// Returns the days of a month a day-of-month token selects.
    /// </summary>
    /// <param name="token">The token: <c>L</c>, <c>L-n</c>, <c>LW</c>, <c>L-nW</c> or <c>nW</c>, in either case.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <returns>The selected days, which are none when the token names no day of the month.</returns>
    public static IEnumerable<int> DaysOfMonthToken(string token, int year, int month)
    {
        int length = DateTime.DaysInMonth(year, month);
        string upper = token.ToUpperInvariant();
        bool nearestWeekday = upper.EndsWith('W');
        string body = nearestWeekday ? upper[..^1] : upper;

        int target = body.StartsWith('L')
            ? length - (body.Length == 1 ? 0 : int.Parse(body[2..], CultureInfo.InvariantCulture))
            : int.Parse(body, CultureInfo.InvariantCulture);

        if (target < 1 || target > length)
        {
            return [];
        }

        if (!nearestWeekday)
        {
            return [target];
        }

        return [Enumerable.Range(1, length)
            .Where(day => new DateTime(year, month, day).DayOfWeek is not (System.DayOfWeek.Saturday or System.DayOfWeek.Sunday))
            .MinBy(day => Math.Abs(day - target))];
    }

    /// <summary>
    /// Returns the days of a month a day-of-week token selects.
    /// </summary>
    /// <param name="token">The token: <c>dL</c> or <c>d#k</c>, with d a weekday number or name, in either case.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <returns>The selected days, which are none when the month has no k-th such weekday.</returns>
    public static IEnumerable<int> DaysOfWeekToken(string token, int year, int month)
    {
        string upper = token.ToUpperInvariant();
        int hash = upper.IndexOf('#', StringComparison.Ordinal);
        string weekdayText = hash >= 0 ? upper[..hash] : upper[..^1];
        int weekday = Weekday(weekdayText);

        int[] matching = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Where(day => (int)new DateTime(year, month, day).DayOfWeek == weekday)
            .ToArray();

        if (hash < 0)
        {
            return [matching[^1]];
        }

        int ordinal = upper[hash + 1] - '0';
        return ordinal <= matching.Length ? [matching[ordinal - 1]] : [];
    }

    /// <summary>
    /// Returns the first occurrence after an instant by walking forward a day at a time.
    /// </summary>
    /// <param name="after">The instant the occurrence must follow.</param>
    /// <param name="inclusive">Whether an occurrence equal to <paramref name="after" /> counts.</param>
    /// <returns>The occurrence, or <see langword="null" /> when none falls within the horizon.</returns>
    public DateTime? Next(DateTime after, bool inclusive)
    {
        int guardYear = after.Year + HorizonYears;
        for (DateTime day = after.Date; day.Year <= guardYear; day = day.AddDays(1))
        {
            if (Matches(day))
            {
                foreach (DateTime instant in Instants(day))
                {
                    if (inclusive ? instant >= after : instant > after)
                    {
                        return instant;
                    }
                }
            }

            if (day.Year == 9999 && day.Month == 12 && day.Day == 31)
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the last occurrence before an instant by walking back a day at a time.
    /// </summary>
    /// <param name="before">The instant the occurrence must precede.</param>
    /// <param name="inclusive">Whether an occurrence equal to <paramref name="before" /> counts.</param>
    /// <returns>The occurrence, or <see langword="null" /> when none falls within the horizon.</returns>
    public DateTime? Previous(DateTime before, bool inclusive)
    {
        int guardYear = before.Year - HorizonYears;
        for (DateTime day = before.Date; day.Year >= guardYear; day = day.AddDays(-1))
        {
            if (Matches(day))
            {
                foreach (DateTime instant in Instants(day).Reverse())
                {
                    if (inclusive ? instant <= before : instant < before)
                    {
                        return instant;
                    }
                }
            }

            if (day == DateTime.MinValue.Date)
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the number of a weekday given as a number from zero to seven or a three-letter name.
    /// </summary>
    /// <param name="text">The weekday text, in upper case.</param>
    /// <returns>The weekday, Sunday as zero.</returns>
    private static int Weekday(string text) =>
        text switch
        {
            "SUN" => 0,
            "MON" => 1,
            "TUE" => 2,
            "WED" => 3,
            "THU" => 4,
            "FRI" => 5,
            "SAT" => 6,
            _ => int.Parse(text, CultureInfo.InvariantCulture) % 7,
        };

    /// <summary>
    /// Determines whether a day field is restricted: anything other than <c>*</c> or <c>?</c>.
    /// </summary>
    /// <param name="field">The field text.</param>
    /// <returns><see langword="true" /> when the field restricts the days; otherwise <see langword="false" />.</returns>
    private static bool IsRestricted(string field) =>
        field is not ("*" or "?");

    /// <summary>
    /// Determines whether the schedule selects a day.
    /// </summary>
    /// <param name="day">Midnight of the day.</param>
    /// <returns><see langword="true" /> when the day is selected; otherwise <see langword="false" />.</returns>
    private bool Matches(DateTime day)
    {
        if (!Months.Contains(day.Month))
        {
            return false;
        }

        HashSet<int> days = MonthDays(day.Year, day.Month);
        return days.Contains(day.Day);
    }

    /// <summary>
    /// Returns the days of a month the two day fields select together.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <returns>The selected days.</returns>
    private HashSet<int> MonthDays(int year, int month)
    {
        if (_days.TryGetValue((year, month), out HashSet<int>? cached))
        {
            return cached;
        }

        int length = DateTime.DaysInMonth(year, month);
        var all = new HashSet<int>(Enumerable.Range(1, length));
        HashSet<int> byMonthDay = !IsRestricted(DayOfMonth)
            ? all
            : DayOfMonth.Any(char.IsAsciiLetter)
                ? [.. DaysOfMonthToken(DayOfMonth, year, month)]
                : [.. DayOfMonth.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).Where(d => d <= length)];
        HashSet<int> byWeekday = !IsRestricted(DayOfWeek)
            ? all
            : DayOfWeek.Contains('#', StringComparison.Ordinal) || DayOfWeek.EndsWith('L')
                ? [.. DaysOfWeekToken(DayOfWeek, year, month)]
                : [.. all.Where(d => DayOfWeek.Split(',').Select(Weekday).Contains((int)new DateTime(year, month, d).DayOfWeek))];

        HashSet<int> result = IsRestricted(DayOfMonth) && IsRestricted(DayOfWeek)
            ? [.. byMonthDay.Union(byWeekday)]
            : [.. byMonthDay.Intersect(byWeekday)];
        _days[(year, month)] = result;
        return result;
    }

    /// <summary>
    /// Returns a selected day's occurrences in ascending order.
    /// </summary>
    /// <param name="day">Midnight of the day.</param>
    /// <returns>The occurrences.</returns>
    private IEnumerable<DateTime> Instants(DateTime day) =>
        from hour in Hours
        from minute in Minutes
        from second in Seconds
        select day.AddHours(hour).AddMinutes(minute).AddSeconds(second);
}
