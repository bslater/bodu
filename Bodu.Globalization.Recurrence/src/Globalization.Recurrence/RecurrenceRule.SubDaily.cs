// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRule.SubDaily.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceRule
{
    /// <summary>The times of day a sub-daily rule's periods may occupy, built on first use.</summary>
    private TimeOfDayBitmap? _subDailyTimesOfDay;

    /// <summary>
    /// Gets the length, in ticks, of one period of a sub-daily rule.
    /// </summary>
    /// <value>One hour, minute or second, by <see cref="Frequency" />.</value>
    private long SubDailyUnitTicks =>
        Frequency switch
        {
            RecurrenceFrequency.Hourly => TimeSpan.TicksPerHour,
            RecurrenceFrequency.Minutely => TimeSpan.TicksPerMinute,
            _ => TimeSpan.TicksPerSecond,
        };

    /// <summary>
    /// Gets the times of day a sub-daily rule's periods may occupy.
    /// </summary>
    /// <value>
    /// A bitmap with one bit per period of a day, or <see cref="TimeOfDayBitmap.Every" /> when every time of day is
    /// allowed.
    /// </value>
    private TimeOfDayBitmap SubDailyTimesOfDay =>
        Volatile.Read(ref _subDailyTimesOfDay) ?? PublishSubDailyTimesOfDay();

    /// <summary>
    /// Produces the ordered occurrence stream of a sub-daily rule from a period index on.
    /// </summary>
    /// <param name="start">The series start the rule is anchored to.</param>
    /// <param name="firstPeriod">
    /// The zero-based index of the first period to generate. It is zero for a rule with <see cref="Count" />, which
    /// counts its occurrences from the first period.
    /// </param>
    /// <param name="endPeriod">The exclusive index of the period at which generation stops.</param>
    /// <returns>The ordered occurrences of the periods generated.</returns>
    /// <remarks>
    /// <para>
    /// A sub-daily period is one hour, minute or second, and the n-th begins n times <see cref="Interval" /> such units
    /// after the one that holds <paramref name="start" />. <c>BYMINUTE</c> and <c>BYSECOND</c> expand an hourly period
    /// and <c>BYSECOND</c> a minutely one, defaulting to the start's own minute and second. <c>BYHOUR</c>, and the
    /// finer parts the frequency does not expand, limit which periods produce occurrences, as do <c>BYMONTH</c>,
    /// <c>BYMONTHDAY</c>, <c>BYYEARDAY</c> and the <c>BYDAY</c> weekdays, whose ordinals have no meaning at these
    /// frequencies. <c>BYWEEKNO</c>, which RFC 5545 allows only with <c>FREQ=YEARLY</c>, is ignored, as it is for a
    /// daily rule. <c>BYSETPOS</c> selects within each period.
    /// </para>
    /// <para>
    /// A limit holds for a whole period, so the walk moves from a period a limit rejects straight to the next period
    /// that can pass: the next day or month for a date limit, and for a time limit the next period whose time of day
    /// the rule allows, found by solving a congruence rather than by stepping through the periods between. A rule whose
    /// interval never reaches an allowed time of day ends at once, and the walk ends at <see cref="Until" /> and at the
    /// end of the calendar.
    /// </para>
    /// </remarks>
    private IEnumerable<DateTime> EnumerateSubDaily(DateTime start, long firstPeriod, long endPeriod)
    {
        DateTimeKind kind = start.Kind;
        long unitTicks = SubDailyUnitTicks;
        int unitsPerDay = (int)(TimeSpan.TicksPerDay / unitTicks);
        long baseTicks = start.Ticks - (start.Ticks % unitTicks);
        long stepTicks = SubDailyStepTicks(unitTicks);
        long[] offsets = BuildSubDailyOffsets(start);
        TimeOfDayBitmap timesOfDay = SubDailyTimesOfDay;
        long untilTicks = Until is DateTime until ? until.Ticks : long.MaxValue;
        int emitted = 0;

        // A BYSETPOS that selects nothing from a period selects nothing from any.
        if (offsets.Length == 0)
        {
            yield break;
        }

        long period = firstPeriod;
        while (period < endPeriod && TryGetSubDailyPeriod(baseTicks, stepTicks, period, out long periodTicks))
        {
            // Every occurrence of this period and of the ones after it falls past UNTIL.
            if (periodTicks > untilTicks)
            {
                yield break;
            }

            if (TryGetResumeAfterRejectedDate(periodTicks, out long resumeTicks))
            {
                if (resumeTicks == long.MaxValue)
                {
                    yield break;
                }

                period = FirstSubDailyPeriodAtOrAfter(baseTicks, stepTicks, resumeTicks);
                continue;
            }

            int timeOfDay = (int)((periodTicks % TimeSpan.TicksPerDay) / unitTicks);
            if (!timesOfDay.Contains(timeOfDay))
            {
                long jump = PeriodsToAllowedTimeOfDay(timesOfDay, timeOfDay, unitsPerDay, forward: true);
                if (jump == 0)
                {
                    yield break;
                }

                period += jump;
                continue;
            }

            foreach (long offset in offsets)
            {
                long ticks = periodTicks + offset;
                if (ticks < start.Ticks)
                {
                    continue;
                }

                if (ticks > untilTicks)
                {
                    yield break;
                }

                yield return new DateTime(ticks, kind);
                emitted++;

                if (Count is int count && emitted >= count)
                {
                    yield break;
                }
            }

            period++;
        }
    }

    /// <summary>
    /// Returns the last occurrence of a sub-daily rule within a bound, walking back from the bound period by period.
    /// </summary>
    /// <param name="start">The series start the rule is anchored to.</param>
    /// <param name="bound">The wall-clock instant the search works back from.</param>
    /// <param name="isBeyond">Determines whether an occurrence lies beyond the bound.</param>
    /// <param name="isExcluded">
    /// Determines whether an occurrence is skipped, or <see langword="null" /> to skip none.
    /// </param>
    /// <returns>The previous occurrence, or <see langword="null" /> when none precedes the bound.</returns>
    /// <remarks>
    /// The walk mirrors <see cref="EnumerateSubDaily" /> in reverse, skipping back over the days and months the date
    /// limits reject and the periods whose time of day the rule does not allow, so its cost grows with the distance
    /// back to the answer rather than with the time elapsed since <paramref name="start" />. A rule with
    /// <see cref="Count" /> is enumerated from its start instead, because the occurrences before a period decide how
    /// many it may still produce.
    /// </remarks>
    private DateTime? FindPreviousSubDaily(
        DateTime start,
        DateTime bound,
        Func<DateTime, bool> isBeyond,
        Func<DateTime, bool>? isExcluded)
    {
        if (Count is not null)
        {
            return LastBefore(EnumerateSubDaily(start, 0, long.MaxValue), isBeyond, isExcluded);
        }

        DateTimeKind kind = start.Kind;
        long unitTicks = SubDailyUnitTicks;
        int unitsPerDay = (int)(TimeSpan.TicksPerDay / unitTicks);
        long baseTicks = start.Ticks - (start.Ticks % unitTicks);
        long stepTicks = SubDailyStepTicks(unitTicks);
        long[] offsets = BuildSubDailyOffsets(start);
        TimeOfDayBitmap timesOfDay = SubDailyTimesOfDay;
        long untilTicks = Until is DateTime until ? until.Ticks : long.MaxValue;

        // Nothing past UNTIL is ever produced, so a bound beyond it searches back from UNTIL.
        long boundTicks = Math.Min(bound.Ticks, untilTicks);
        if (boundTicks < baseTicks || offsets.Length == 0)
        {
            return null;
        }

        // The walk begins a period past the one that holds the bound; the occurrences beyond the bound are skipped.
        long lastPeriod = (DateTime.MaxValue.Ticks - baseTicks) / stepTicks;
        long period = Math.Min(((boundTicks - baseTicks) / stepTicks) + 1, lastPeriod);
        while (period >= 0)
        {
            long periodTicks = baseTicks + (period * stepTicks);

            if (TryGetBoundaryOfRejectedDate(periodTicks, out long boundaryTicks))
            {
                period = FirstSubDailyPeriodAtOrAfter(baseTicks, stepTicks, boundaryTicks) - 1;
                continue;
            }

            int timeOfDay = (int)((periodTicks % TimeSpan.TicksPerDay) / unitTicks);
            if (!timesOfDay.Contains(timeOfDay))
            {
                long jump = PeriodsToAllowedTimeOfDay(timesOfDay, timeOfDay, unitsPerDay, forward: false);
                if (jump == 0)
                {
                    return null;
                }

                period -= jump;
                continue;
            }

            for (int i = offsets.Length - 1; i >= 0; i--)
            {
                long ticks = periodTicks + offsets[i];

                // This candidate and every earlier one precede the start, so nothing within the bound remains.
                if (ticks < start.Ticks)
                {
                    return null;
                }

                if (ticks > untilTicks)
                {
                    continue;
                }

                var occurrence = new DateTime(ticks, kind);
                if (!isBeyond(occurrence) && (isExcluded is null || !isExcluded(occurrence)))
                {
                    return occurrence;
                }
            }

            period--;
        }

        return null;
    }

    /// <summary>
    /// Returns a sub-daily period index such that no earlier period holds an occurrence on or after an instant.
    /// </summary>
    /// <param name="start">The series start the rule is anchored to.</param>
    /// <param name="bound">The instant the skipped periods must all end before.</param>
    /// <returns>A non-negative period index.</returns>
    /// <remarks>
    /// A period's occurrences fall within its own hour, minute or second, so every period before the one that holds
    /// <paramref name="bound" /> ends by its start. The result is one period below that, so an error can only enumerate
    /// a period more.
    /// </remarks>
    private long FirstSubDailyPeriodReaching(DateTime start, DateTime bound)
    {
        long unitTicks = SubDailyUnitTicks;
        long baseTicks = start.Ticks - (start.Ticks % unitTicks);
        if (bound.Ticks <= baseTicks)
        {
            return 0;
        }

        return Math.Max(0, ((bound.Ticks - baseTicks) / SubDailyStepTicks(unitTicks)) - 1);
    }

    /// <summary>
    /// Returns the distance, in ticks, between the starts of consecutive sub-daily periods.
    /// </summary>
    /// <param name="unitTicks">The length of one period.</param>
    /// <returns>
    /// <see cref="Interval" /> periods, or <see cref="long.MaxValue" /> when that is longer than the calendar.
    /// </returns>
    private long SubDailyStepTicks(long unitTicks) =>
        Interval > long.MaxValue / unitTicks ? long.MaxValue : Interval * unitTicks;

    /// <summary>
    /// Computes the first tick of a sub-daily period.
    /// </summary>
    /// <param name="baseTicks">The first tick of the period that holds the series start.</param>
    /// <param name="stepTicks">The distance between the starts of consecutive periods.</param>
    /// <param name="period">The zero-based period index.</param>
    /// <param name="periodTicks">The first tick of the period on success.</param>
    /// <returns>
    /// <see langword="true" /> when the period begins within the calendar; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryGetSubDailyPeriod(long baseTicks, long stepTicks, long period, out long periodTicks)
    {
        if (period > (DateTime.MaxValue.Ticks - baseTicks) / stepTicks)
        {
            periodTicks = 0;
            return false;
        }

        periodTicks = baseTicks + (period * stepTicks);
        return true;
    }

    /// <summary>
    /// Returns the index of the first sub-daily period that begins at or after an instant.
    /// </summary>
    /// <param name="baseTicks">The first tick of the period that holds the series start.</param>
    /// <param name="stepTicks">The distance between the starts of consecutive periods.</param>
    /// <param name="ticks">The instant, in ticks.</param>
    /// <returns>
    /// The period index, which is zero when <paramref name="ticks" /> is at or before the first period.
    /// </returns>
    private static long FirstSubDailyPeriodAtOrAfter(long baseTicks, long stepTicks, long ticks)
    {
        if (ticks <= baseTicks)
        {
            return 0;
        }

        long distance = ticks - baseTicks;
        return (distance / stepTicks) + (distance % stepTicks == 0 ? 0 : 1);
    }

    /// <summary>
    /// Determines whether the date limits reject the day that holds an instant, and if so where the forward walk
    /// resumes.
    /// </summary>
    /// <param name="ticks">The instant, in ticks.</param>
    /// <param name="resumeTicks">
    /// The first tick of the next month, when <c>BYMONTH</c> rejects the month, or else of the next day; or
    /// <see cref="long.MaxValue" /> when the calendar ends first.
    /// </param>
    /// <returns><see langword="true" /> when the day is rejected; otherwise <see langword="false" />.</returns>
    private bool TryGetResumeAfterRejectedDate(long ticks, out long resumeTicks)
    {
        DateOnly day = DateOnly.FromDayNumber((int)(ticks / TimeSpan.TicksPerDay));
        if (!MonthAllowed(day.Month))
        {
            bool lastMonth = day.Year == DateOnly.MaxValue.Year && day.Month == 12;
            resumeTicks = lastMonth
                ? long.MaxValue
                : new DateOnly(day.Year, day.Month, 1).AddMonths(1).DayNumber * TimeSpan.TicksPerDay;
            return true;
        }

        if (!MonthDayAllowed(day) || !YearDayAllowed(day) || !WeekDayAllowed(day, honorOrdinal: false))
        {
            resumeTicks = day == DateOnly.MaxValue ? long.MaxValue : (day.DayNumber + 1L) * TimeSpan.TicksPerDay;
            return true;
        }

        resumeTicks = 0;
        return false;
    }

    /// <summary>
    /// Determines whether the date limits reject the day that holds an instant, and if so where the rejected span
    /// begins.
    /// </summary>
    /// <param name="ticks">The instant, in ticks.</param>
    /// <param name="boundaryTicks">
    /// The first tick of the month, when <c>BYMONTH</c> rejects it, or else of the day; the backward walk resumes at
    /// the period before it.
    /// </param>
    /// <returns><see langword="true" /> when the day is rejected; otherwise <see langword="false" />.</returns>
    private bool TryGetBoundaryOfRejectedDate(long ticks, out long boundaryTicks)
    {
        DateOnly day = DateOnly.FromDayNumber((int)(ticks / TimeSpan.TicksPerDay));
        if (!MonthAllowed(day.Month))
        {
            boundaryTicks = new DateOnly(day.Year, day.Month, 1).DayNumber * TimeSpan.TicksPerDay;
            return true;
        }

        if (!MonthDayAllowed(day) || !YearDayAllowed(day) || !WeekDayAllowed(day, honorOrdinal: false))
        {
            boundaryTicks = day.DayNumber * TimeSpan.TicksPerDay;
            return true;
        }

        boundaryTicks = 0;
        return false;
    }

    /// <summary>
    /// Builds the offsets, from the start of a sub-daily period, of the occurrences each period that passes the limits
    /// produces.
    /// </summary>
    /// <param name="start">The series start, whose minute and second supply the defaults.</param>
    /// <returns>The offsets in ticks, ascending and distinct, after the <c>BYSETPOS</c> selection.</returns>
    /// <remarks>
    /// Every offset falls within the period's own hour, minute or second, so a period that begins within the calendar
    /// ends within it, and every period produces the same offsets.
    /// </remarks>
    private long[] BuildSubDailyOffsets(DateTime start)
    {
        long[] offsets = ExpandSubDailyPeriod(start);
        return _bySetPos.Length == 0 ? offsets : SelectSubDailyOffsets(offsets);
    }

    /// <summary>
    /// Builds the offsets, from the start of a sub-daily period, of the instants the period expands to.
    /// </summary>
    /// <param name="start">The series start, whose minute and second supply the defaults.</param>
    /// <returns>The offsets in ticks, ascending and distinct.</returns>
    private long[] ExpandSubDailyPeriod(DateTime start)
    {
        if (Frequency == RecurrenceFrequency.Secondly)
        {
            return [0];
        }

        int[] seconds = _bySecond.Length > 0 ? _bySecond : [start.Second];
        int[] minutes = Frequency == RecurrenceFrequency.Hourly && _byMinute.Length > 0 ? _byMinute : [start.Minute];

        var set = new SortedSet<long>();
        foreach (int second in seconds)
        {
            // A BYSECOND value of 60 (leap second) collapses onto 59, which the calendar can represent.
            long secondTicks = Math.Min(second, 59) * TimeSpan.TicksPerSecond;
            if (Frequency == RecurrenceFrequency.Minutely)
            {
                set.Add(secondTicks);
                continue;
            }

            foreach (int minute in minutes)
            {
                set.Add((minute * TimeSpan.TicksPerMinute) + secondTicks);
            }
        }

        return [.. set];
    }

    /// <summary>
    /// Applies the <c>BYSETPOS</c> selection to the offsets a sub-daily period expands to.
    /// </summary>
    /// <param name="offsets">The period's offsets, ascending.</param>
    /// <returns>The selected offsets in ascending order, without duplicates.</returns>
    private long[] SelectSubDailyOffsets(long[] offsets)
    {
        var selected = new SortedSet<long>();
        foreach (int position in _bySetPos)
        {
            int index = position > 0 ? position - 1 : offsets.Length + position;
            if (index >= 0 && index < offsets.Length)
            {
                selected.Add(offsets[index]);
            }
        }

        return [.. selected];
    }

    /// <summary>
    /// Returns how many periods a sub-daily walk must move to reach a time of day the rule allows.
    /// </summary>
    /// <param name="timesOfDay">
    /// The rule's time-of-day bitmap, which does not allow <paramref name="timeOfDay" />.
    /// </param>
    /// <param name="timeOfDay">The current period's time of day, in periods since midnight.</param>
    /// <param name="unitsPerDay">The number of periods in a day.</param>
    /// <param name="forward"><see langword="true" /> to move forward; <see langword="false" /> to move back.</param>
    /// <returns>The smallest positive number of periods, or zero when no allowed time of day is ever reached.</returns>
    /// <remarks>
    /// Moving j periods changes the time of day by j times <see cref="Interval" /> modulo a day, so reaching an allowed
    /// time a whose distance from the current one is d needs j·Interval ≡ d (mod a day), which has a solution when the
    /// greatest common divisor g of the interval and the day divides d, and then exactly one j below the day over g.
    /// Every solution moves the walk at least d periods' worth of time, so the allowed times whose distance g divides
    /// are visited in order of distance, passing over the others a multiple of g at a time, and the search stops once d
    /// reaches the best move found.
    /// </remarks>
    private long PeriodsToAllowedTimeOfDay(TimeOfDayBitmap timesOfDay, int timeOfDay, int unitsPerDay, bool forward)
    {
        int stepWithinDay = (int)(Interval % unitsPerDay);
        int divisor = GreatestCommonDivisor(stepWithinDay, unitsPerDay);
        int cycle = unitsPerDay / divisor;
        long inverse = cycle == 1 ? 0 : ModularInverse(stepWithinDay / divisor, cycle);

        long bestPeriods = 0;
        long bestElapsed = long.MaxValue;
        int distance = timesOfDay.NextAllowedDistance(timeOfDay, divisor, forward);
        while (distance > 0 && distance < bestElapsed)
        {
            // Only a distance the divisor divides can be reached, so the search resumes at the next multiple of it
            // rather than at the next allowed time.
            int remainder = distance % divisor;
            if (remainder != 0)
            {
                distance = timesOfDay.NextAllowedDistance(timeOfDay, distance - remainder + divisor, forward);
                continue;
            }

            long periods = ((distance / divisor) * inverse) % cycle;
            long elapsed = periods * Interval;
            if (elapsed < bestElapsed)
            {
                bestElapsed = elapsed;
                bestPeriods = periods;
            }

            distance = timesOfDay.NextAllowedDistance(timeOfDay, distance + divisor, forward);
        }

        return bestPeriods;
    }

    /// <summary>
    /// Returns the greatest common divisor of a non-negative integer and a positive one.
    /// </summary>
    /// <param name="value">The non-negative integer.</param>
    /// <param name="modulus">The positive integer.</param>
    /// <returns>
    /// The greatest common divisor, which is <paramref name="modulus" /> when <paramref name="value" /> is zero.
    /// </returns>
    private static int GreatestCommonDivisor(int value, int modulus)
    {
        while (value != 0)
        {
            (value, modulus) = (modulus % value, value);
        }

        return modulus;
    }

    /// <summary>
    /// Returns the inverse of a value modulo a modulus it is coprime to.
    /// </summary>
    /// <param name="value">The value, coprime to <paramref name="modulus" />.</param>
    /// <param name="modulus">The modulus, greater than one.</param>
    /// <returns>The inverse, in the range zero to <paramref name="modulus" /> less one.</returns>
    private static long ModularInverse(long value, long modulus)
    {
        long previous = 0;
        long current = 1;
        long remainder = modulus;
        long next = value % modulus;
        while (next != 0)
        {
            long quotient = remainder / next;
            (previous, current) = (current, previous - (quotient * current));
            (remainder, next) = (next, remainder - (quotient * next));
        }

        return previous < 0 ? previous + modulus : previous;
    }

    /// <summary>
    /// Builds the rule's time-of-day bitmap and publishes it, keeping the first one published.
    /// </summary>
    /// <returns>The published bitmap.</returns>
    private TimeOfDayBitmap PublishSubDailyTimesOfDay()
    {
        TimeOfDayBitmap built = BuildSubDailyTimesOfDay();
        return Interlocked.CompareExchange(ref _subDailyTimesOfDay, built, null) ?? built;
    }

    /// <summary>
    /// Builds the bitmap of the times of day a sub-daily rule's periods may occupy.
    /// </summary>
    /// <returns>
    /// A bitmap with one bit per period of a day, or <see cref="TimeOfDayBitmap.Every" /> when the rule limits none.
    /// </returns>
    /// <remarks>
    /// <c>BYHOUR</c> limits every sub-daily frequency, <c>BYMINUTE</c> limits the minutely and secondly ones, and
    /// <c>BYSECOND</c> the secondly one; a part the frequency expands instead does not appear here.
    /// </remarks>
    private TimeOfDayBitmap BuildSubDailyTimesOfDay()
    {
        bool limitsMinute = Frequency != RecurrenceFrequency.Hourly && _byMinute.Length > 0;
        bool limitsSecond = Frequency == RecurrenceFrequency.Secondly && _bySecond.Length > 0;
        if (_byHour.Length == 0 && !limitsMinute && !limitsSecond)
        {
            return TimeOfDayBitmap.Every;
        }

        int unitsPerDay = (int)(TimeSpan.TicksPerDay / SubDailyUnitTicks);
        var bitmap = new TimeOfDayBitmap(unitsPerDay);
        int[] hours = _byHour.Length > 0 ? _byHour : EveryValue(24);
        int[] minutes = Frequency == RecurrenceFrequency.Hourly ? [0] : limitsMinute ? _byMinute : EveryValue(60);
        int[] seconds = Frequency != RecurrenceFrequency.Secondly ? [0] : limitsSecond ? _bySecond : EveryValue(60);

        foreach (int hour in hours)
        {
            foreach (int minute in minutes)
            {
                foreach (int second in seconds)
                {
                    int timeOfDay = Frequency switch
                    {
                        RecurrenceFrequency.Hourly => hour,
                        RecurrenceFrequency.Minutely => (hour * 60) + minute,
                        _ => (hour * 3600) + (minute * 60) + Math.Min(second, 59),
                    };

                    bitmap.Add(timeOfDay);
                }
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Returns every value from zero up to a bound.
    /// </summary>
    /// <param name="count">The number of values.</param>
    /// <returns>The values zero to <paramref name="count" /> less one.</returns>
    private static int[] EveryValue(int count) =>
        Enumerable.Range(0, count).ToArray();
}
