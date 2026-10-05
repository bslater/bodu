// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SubDailyReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Provides a literal reading of RFC 5545's sub-daily recurrence rules, kept as the oracle the sub-daily engine is held
/// to. It visits every period in order and applies each rule part to each candidate instant as the specification
/// states it, with none of the engine's skips over rejected days, jumps between allowed times of day, or backward walks.
/// </summary>
/// <remarks>
/// The reading matches the engine's documented choices where RFC 5545 leaves room: the ordinal of a <c>BYDAY</c> value
/// is ignored, <c>BYWEEKNO</c> is ignored, and a <c>BYSECOND</c> value of 60 stands for second 59.
/// </remarks>
internal static class SubDailyReference
{
    /// <summary>
    /// Enumerates a sub-daily rule's occurrences from its start, period by period, up to a horizon.
    /// </summary>
    /// <param name="rule">The sub-daily rule.</param>
    /// <param name="start">The series start.</param>
    /// <param name="horizon">The latest instant enumerated.</param>
    /// <param name="maximum">The number of occurrences after which the enumeration stops.</param>
    /// <param name="complete">
    /// The latest instant up to which the result is the whole stream: <paramref name="horizon" />, the last occurrence
    /// when <paramref name="maximum" /> stops the enumeration first, or <see cref="DateTime.MaxValue" /> when the rule has
    /// no further occurrences.
    /// </param>
    /// <returns>The ascending occurrences no later than <paramref name="complete" />.</returns>
    public static List<DateTime> Enumerate(
        RecurrenceRule rule,
        DateTime start,
        DateTime horizon,
        int maximum,
        out DateTime complete)
    {
        long unit = rule.Frequency switch
        {
            RecurrenceFrequency.Hourly => TimeSpan.TicksPerHour,
            RecurrenceFrequency.Minutely => TimeSpan.TicksPerMinute,
            _ => TimeSpan.TicksPerSecond,
        };

        long step = rule.Interval > DateTime.MaxValue.Ticks / unit ? long.MaxValue : rule.Interval * unit;
        long periodTicks = start.Ticks - (start.Ticks % unit);
        var stream = new List<DateTime>();

        while (periodTicks <= horizon.Ticks)
        {
            if (rule.Until is DateTime until && periodTicks > until.Ticks)
            {
                complete = DateTime.MaxValue;
                return stream;
            }

            foreach (DateTime candidate in Candidates(rule, start, new DateTime(periodTicks, start.Kind)))
            {
                if (candidate < start)
                {
                    continue;
                }

                if (candidate > horizon)
                {
                    complete = horizon;
                    return stream;
                }

                if (rule.Until is DateTime last && candidate > last)
                {
                    complete = DateTime.MaxValue;
                    return stream;
                }

                stream.Add(candidate);
                if (stream.Count == rule.Count)
                {
                    complete = DateTime.MaxValue;
                    return stream;
                }

                if (stream.Count == maximum)
                {
                    complete = candidate;
                    return stream;
                }
            }

            if (periodTicks > DateTime.MaxValue.Ticks - step)
            {
                complete = DateTime.MaxValue;
                return stream;
            }

            periodTicks += step;
        }

        complete = horizon;
        return stream;
    }

    /// <summary>
    /// Returns the instants a period produces: its expansion, less the instants a limit rejects, as selected by
    /// <c>BYSETPOS</c>.
    /// </summary>
    /// <param name="rule">The sub-daily rule.</param>
    /// <param name="start">The series start, whose minute and second the expansion defaults to.</param>
    /// <param name="period">The first instant of the period.</param>
    /// <returns>The instants in ascending order.</returns>
    private static List<DateTime> Candidates(RecurrenceRule rule, DateTime start, DateTime period)
    {
        // Every instant of a period shares its date and hour, so a period whose first instant a date or hour limit
        // rejects produces nothing; checking it first only saves expanding the period.
        if (!PassesDateAndHourLimits(rule, period))
        {
            return [];
        }

        int[] seconds = rule.BySecond.Count > 0 ? rule.BySecond.Select(s => Math.Min(s, 59)).ToArray() : [start.Second];
        int[] minutes = rule.ByMinute.Count > 0 ? [.. rule.ByMinute] : [start.Minute];

        IEnumerable<DateTime> expansion = rule.Frequency switch
        {
            RecurrenceFrequency.Hourly =>
                from minute in minutes
                from second in seconds
                select period.AddMinutes(minute).AddSeconds(second),
            RecurrenceFrequency.Minutely => seconds.Select(second => period.AddSeconds(second)),
            _ => [period],
        };

        List<DateTime> set = expansion.Where(instant => PassesLimits(rule, instant)).Distinct().Order().ToList();
        if (rule.BySetPos.Count == 0)
        {
            return set;
        }

        var selected = new SortedSet<DateTime>();
        foreach (int position in rule.BySetPos)
        {
            int index = position > 0 ? position - 1 : set.Count + position;
            if (index >= 0 && index < set.Count)
            {
                selected.Add(set[index]);
            }
        }

        return [.. selected];
    }

    /// <summary>
    /// Determines whether an instant satisfies every part of a sub-daily rule that limits rather than expands.
    /// </summary>
    /// <param name="rule">The sub-daily rule.</param>
    /// <param name="instant">The candidate instant.</param>
    /// <returns><see langword="true" /> when no limit rejects the instant; otherwise <see langword="false" />.</returns>
    private static bool PassesLimits(RecurrenceRule rule, DateTime instant)
    {
        bool minute = rule.Frequency == RecurrenceFrequency.Hourly
            || rule.ByMinute.Count == 0
            || rule.ByMinute.Contains(instant.Minute);
        bool second = rule.Frequency != RecurrenceFrequency.Secondly
            || rule.BySecond.Count == 0
            || rule.BySecond.Any(s => Math.Min(s, 59) == instant.Second);

        return PassesDateAndHourLimits(rule, instant) && minute && second;
    }

    /// <summary>
    /// Determines whether an instant satisfies a sub-daily rule's date limits and its <c>BYHOUR</c> limit.
    /// </summary>
    /// <param name="rule">The sub-daily rule.</param>
    /// <param name="instant">The candidate instant.</param>
    /// <returns><see langword="true" /> when none of those limits rejects the instant; otherwise <see langword="false" />.</returns>
    private static bool PassesDateAndHourLimits(RecurrenceRule rule, DateTime instant)
    {
        int daysInMonth = DateTime.DaysInMonth(instant.Year, instant.Month);
        int daysInYear = DateTime.IsLeapYear(instant.Year) ? 366 : 365;

        bool month = rule.ByMonth.Count == 0 || rule.ByMonth.Contains(instant.Month);
        bool monthDay = rule.ByMonthDay.Count == 0
            || rule.ByMonthDay.Any(d => instant.Day == (d > 0 ? d : daysInMonth + d + 1));
        bool yearDay = rule.ByYearDay.Count == 0
            || rule.ByYearDay.Any(d => instant.DayOfYear == (d > 0 ? d : daysInYear + d + 1));
        bool weekDay = rule.ByDay.Count == 0 || rule.ByDay.Any(d => d.Day == instant.DayOfWeek);
        bool hour = rule.ByHour.Count == 0 || rule.ByHour.Contains(instant.Hour);

        return month && monthDay && yearDay && weekDay && hour;
    }
}
