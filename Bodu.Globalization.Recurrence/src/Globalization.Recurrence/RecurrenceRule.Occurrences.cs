// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRule.Occurrences.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceRule
{
    /// <summary>
    /// Enumerates the occurrences of the rule anchored at the specified start instant.
    /// </summary>
    /// <param name="start">The series start (<c>DTSTART</c>) the rule is anchored to.</param>
    /// <returns>
    /// The occurrences in ascending chronological order, each preserving the <see cref="DateTime.Kind" /> of
    /// <paramref name="start" />. The sequence is bounded when the rule declares <see cref="Count" /> or
    /// <see cref="Until" />, and otherwise continues to the end of the representable calendar; use
    /// <see cref="Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> or the windowed overload to bound it.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the rule uses a sub-daily frequency.</exception>
    /// <remarks>
    /// Whether an instant is an occurrence depends only on the rule and <paramref name="start" />, never on any query
    /// window, so the windowed overload and this method agree on membership. The start instant is itself emitted only
    /// when it satisfies the rule.
    /// </remarks>
    public IEnumerable<DateTime> GetOccurrences(DateTime start) =>
        Enumerate(start);

    /// <summary>
    /// Enumerates the occurrences of the rule that fall within an inclusive window.
    /// </summary>
    /// <param name="start">The series start (<c>DTSTART</c>) the rule is anchored to.</param>
    /// <param name="from">The inclusive lower bound of the window.</param>
    /// <param name="to">The inclusive upper bound of the window.</param>
    /// <returns>The occurrences within <c>[from, to]</c> in ascending chronological order.</returns>
    /// <exception cref="NotSupportedException">Thrown when the rule uses a sub-daily frequency.</exception>
    public IEnumerable<DateTime> GetOccurrences(DateTime start, DateTime from, DateTime to)
    {
        foreach (DateTime occurrence in Enumerate(start))
        {
            if (occurrence > to)
            {
                yield break;
            }

            if (occurrence >= from)
            {
                yield return occurrence;
            }
        }
    }

    /// <summary>
    /// Returns the first occurrence of the rule that falls after the specified instant.
    /// </summary>
    /// <param name="start">The series start (<c>DTSTART</c>) the rule is anchored to.</param>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>The next occurrence, or <see langword="null" /> when the rule produces none.</returns>
    /// <exception cref="NotSupportedException">Thrown when the rule uses a sub-daily frequency.</exception>
    public DateTime? GetNextOccurrence(DateTime start, DateTime after, bool inclusive = false)
    {
        foreach (DateTime occurrence in Enumerate(start))
        {
            if (occurrence > after || (inclusive && occurrence == after))
            {
                return occurrence;
            }
        }

        return null;
    }

    /// <summary>
    /// Enumerates the occurrences of the rule anchored at the specified start, preserving its UTC offset.
    /// </summary>
    /// <param name="start">The series start (<c>DTSTART</c>) the rule is anchored to.</param>
    /// <returns>
    /// The occurrences in ascending chronological order, each carrying the offset of <paramref name="start" />.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when the rule uses a sub-daily frequency.</exception>
    /// <remarks>
    /// Expansion is performed on the wall-clock time of <paramref name="start" /> and the fixed offset is reattached to
    /// each result; daylight-saving transitions are not modeled (the rule carries no time zone).
    /// </remarks>
    public IEnumerable<DateTimeOffset> GetOccurrences(DateTimeOffset start)
    {
        TimeSpan offset = start.Offset;
        foreach (DateTime occurrence in Enumerate(DateTime.SpecifyKind(start.DateTime, DateTimeKind.Unspecified)))
        {
            yield return new DateTimeOffset(occurrence, offset);
        }
    }

    /// <summary>
    /// Enumerates the occurrences of the rule that fall within an inclusive window, preserving the start's offset.
    /// </summary>
    /// <param name="start">The series start (<c>DTSTART</c>) the rule is anchored to.</param>
    /// <param name="from">The inclusive lower bound of the window.</param>
    /// <param name="to">The inclusive upper bound of the window.</param>
    /// <returns>The occurrences within <c>[from, to]</c> in ascending chronological order.</returns>
    /// <exception cref="NotSupportedException">Thrown when the rule uses a sub-daily frequency.</exception>
    public IEnumerable<DateTimeOffset> GetOccurrences(DateTimeOffset start, DateTimeOffset from, DateTimeOffset to)
    {
        foreach (DateTimeOffset occurrence in GetOccurrences(start))
        {
            if (occurrence > to)
            {
                yield break;
            }

            if (occurrence >= from)
            {
                yield return occurrence;
            }
        }
    }

    /// <summary>
    /// Returns the first occurrence of the rule that falls after the specified instant, preserving the start's offset.
    /// </summary>
    /// <param name="start">The series start (<c>DTSTART</c>) the rule is anchored to.</param>
    /// <param name="after">The instant the returned occurrence must follow.</param>
    /// <param name="inclusive">
    /// <see langword="true" /> to allow an occurrence exactly equal to <paramref name="after" />; otherwise the
    /// occurrence must be strictly later.
    /// </param>
    /// <returns>The next occurrence, or <see langword="null" /> when the rule produces none.</returns>
    /// <exception cref="NotSupportedException">Thrown when the rule uses a sub-daily frequency.</exception>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset start, DateTimeOffset after, bool inclusive = false)
    {
        foreach (DateTimeOffset occurrence in GetOccurrences(start))
        {
            if (occurrence > after || (inclusive && occurrence == after))
            {
                return occurrence;
            }
        }

        return null;
    }

    /// <summary>
    /// Produces the ordered occurrence stream, applying the per-period <c>BY</c> expansion, <c>BYSETPOS</c> selection,
    /// and the <c>COUNT</c> / <c>UNTIL</c> bounds.
    /// </summary>
    /// <param name="start">The series start the rule is anchored to.</param>
    /// <returns>The ordered occurrences.</returns>
    /// <exception cref="NotSupportedException">Thrown when the rule uses a sub-daily frequency.</exception>
    private IEnumerable<DateTime> Enumerate(DateTime start)
    {
        if (IsSubDaily)
        {
            throw new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Op_NotSupported_SubDailyFrequency, Frequency));
        }

        return EnumerateCore(start);
    }

    /// <summary>
    /// Implements the period-by-period occurrence generation for the supported daily-and-coarser frequencies.
    /// </summary>
    /// <param name="start">The series start the rule is anchored to.</param>
    /// <returns>The ordered occurrences.</returns>
    private IEnumerable<DateTime> EnumerateCore(DateTime start)
    {
        DateTimeKind kind = start.Kind;
        TimeSpan[] times = BuildTimeSet(start);
        DateOnly startDate = DateOnly.FromDateTime(start);
        int emitted = 0;
        int period = 0;

        while (true)
        {
            if (!TryBuildPeriodStart(startDate, period, out DateOnly anchor))
            {
                yield break;
            }

            List<DateOnly> days = BuildPeriodDays(anchor, startDate);
            days.Sort();

            List<DateTime> instants = new(days.Count * times.Length);
            foreach (DateOnly day in days)
            {
                foreach (TimeSpan time in times)
                {
                    instants.Add(new DateTime(day.Year, day.Month, day.Day, time.Hours, time.Minutes, time.Seconds, kind));
                }
            }

            instants.Sort();
            IReadOnlyList<DateTime> selected = _bySetPos.Length == 0 ? instants : ApplySetPos(instants);

            foreach (DateTime instant in selected)
            {
                if (instant < start)
                {
                    continue;
                }

                if (Until is DateTime until && instant > NormalizeUntil(until, kind))
                {
                    yield break;
                }

                yield return instant;
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
    /// Computes the anchor date of the <paramref name="period" />-th frequency period relative to the start date.
    /// </summary>
    /// <param name="startDate">The date component of the series start.</param>
    /// <param name="period">The zero-based period index.</param>
    /// <param name="anchor">The computed anchor date on success.</param>
    /// <returns><see langword="true" /> when the anchor is representable; otherwise <see langword="false" />.</returns>
    private bool TryBuildPeriodStart(DateOnly startDate, int period, out DateOnly anchor)
    {
        anchor = default;
        try
        {
            switch (Frequency)
            {
                case RecurrenceFrequency.Daily:
                    long dayNumber = (long)startDate.DayNumber + ((long)period * Interval);
                    if (dayNumber > DateOnly.MaxValue.DayNumber)
                    {
                        return false;
                    }

                    anchor = DateOnly.FromDayNumber((int)dayNumber);
                    return true;

                case RecurrenceFrequency.Weekly:
                    long weekDay = (long)startDate.DayNumber + ((long)period * Interval * 7);
                    if (weekDay > DateOnly.MaxValue.DayNumber)
                    {
                        return false;
                    }

                    anchor = DateOnly.FromDayNumber((int)weekDay);
                    return true;

                case RecurrenceFrequency.Monthly:
                    anchor = startDate.AddMonths(period * Interval);
                    return true;

                default:
                    int year = startDate.Year + (period * Interval);
                    if (year > DateOnly.MaxValue.Year)
                    {
                        return false;
                    }

                    anchor = new DateOnly(year, 1, 1);
                    return true;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds the set of candidate dates within the frequency period anchored at <paramref name="anchor" />.
    /// </summary>
    /// <param name="anchor">The anchor date of the period.</param>
    /// <param name="startDate">The date component of the series start, used for defaults.</param>
    /// <returns>The candidate dates, unsorted and possibly empty.</returns>
    private List<DateOnly> BuildPeriodDays(DateOnly anchor, DateOnly startDate) =>
        Frequency switch
        {
            RecurrenceFrequency.Daily => BuildDailyDays(anchor),
            RecurrenceFrequency.Weekly => BuildWeeklyDays(anchor, startDate),
            RecurrenceFrequency.Monthly => BuildMonthlyDays(anchor.Year, anchor.Month, startDate),
            _ => BuildYearlyDays(anchor.Year, startDate),
        };

    /// <summary>
    /// Builds the candidate day for a daily period, applying the month, month-day, and weekday limits.
    /// </summary>
    /// <param name="anchor">The single day of the daily period.</param>
    /// <returns>A list containing the day when it satisfies every limit; otherwise an empty list.</returns>
    private List<DateOnly> BuildDailyDays(DateOnly anchor)
    {
        if (!MonthAllowed(anchor.Month)
            || !MonthDayAllowed(anchor)
            || !YearDayAllowed(anchor)
            || !WeekDayAllowed(anchor))
        {
            return [];
        }

        return [anchor];
    }

    /// <summary>
    /// Builds the candidate days for a weekly period, expanding <c>BYDAY</c> across the week and applying the month
    /// limit.
    /// </summary>
    /// <param name="anchor">A date within the target week.</param>
    /// <param name="startDate">The date component of the series start, used when <c>BYDAY</c> is absent.</param>
    /// <returns>The candidate days within the week.</returns>
    private List<DateOnly> BuildWeeklyDays(DateOnly anchor, DateOnly startDate)
    {
        DateOnly weekStart = StartOfWeek(anchor);
        var result = new List<DateOnly>();
        DayOfWeek[] weekdays = _byDay.Length > 0
            ? Array.ConvertAll(_byDay, entry => entry.Day)
            : [startDate.DayOfWeek];

        for (int offset = 0; offset < 7; offset++)
        {
            DateOnly day = weekStart.AddDays(offset);
            if (Array.IndexOf(weekdays, day.DayOfWeek) >= 0 && MonthAllowed(day.Month))
            {
                result.Add(day);
            }
        }

        return result;
    }

    /// <summary>
    /// Builds the candidate days for a monthly period from <c>BYMONTHDAY</c> and <c>BYDAY</c>, applying the month
    /// limit.
    /// </summary>
    /// <param name="year">The period year.</param>
    /// <param name="month">The period month.</param>
    /// <param name="startDate">The date component of the series start, used when no day rule is present.</param>
    /// <returns>The candidate days within the month.</returns>
    private List<DateOnly> BuildMonthlyDays(int year, int month, DateOnly startDate)
    {
        if (!MonthAllowed(month))
        {
            return [];
        }

        bool hasMonthDay = _byMonthDay.Length > 0;
        bool hasDay = _byDay.Length > 0;
        var result = new List<DateOnly>();

        if (hasMonthDay)
        {
            foreach (int monthDay in _byMonthDay)
            {
                if (TryResolveMonthDay(year, month, monthDay, out DateOnly day) && (!hasDay || WeekDayAllowed(day)))
                {
                    result.Add(day);
                }
            }
        }
        else if (hasDay)
        {
            foreach (WeekDayNum entry in _byDay)
            {
                AddWeekdayOccurrencesInMonth(year, month, entry, result);
            }
        }
        else if (TryResolveMonthDay(year, month, startDate.Day, out DateOnly defaultDay))
        {
            result.Add(defaultDay);
        }

        return result;
    }

    /// <summary>
    /// Builds the candidate days for a yearly period, composing the year-level <c>BY</c> rules.
    /// </summary>
    /// <param name="year">The period year.</param>
    /// <param name="startDate">The date component of the series start, used for defaults.</param>
    /// <returns>The candidate days within the year.</returns>
    private List<DateOnly> BuildYearlyDays(int year, DateOnly startDate)
    {
        var result = new List<DateOnly>();
        int[] months = _byMonth.Length > 0 ? _byMonth : null!;

        if (_byYearDay.Length > 0)
        {
            foreach (int yearDay in _byYearDay)
            {
                if (TryResolveYearDay(year, yearDay, out DateOnly day)
                    && MonthAllowed(day.Month)
                    && WeekDayAllowed(day))
                {
                    result.Add(day);
                }
            }
        }
        else if (_byWeekNo.Length > 0)
        {
            AddWeekNumberDays(year, startDate.DayOfWeek, result);
        }
        else if (_byMonthDay.Length > 0)
        {
            foreach (int month in months ?? Enumerable.Range(1, 12).ToArray())
            {
                foreach (int monthDay in _byMonthDay)
                {
                    if (TryResolveMonthDay(year, month, monthDay, out DateOnly day) && WeekDayAllowed(day))
                    {
                        result.Add(day);
                    }
                }
            }
        }
        else if (_byDay.Length > 0 && months is not null)
        {
            foreach (int month in months)
            {
                foreach (WeekDayNum entry in _byDay)
                {
                    AddWeekdayOccurrencesInMonth(year, month, entry, result);
                }
            }
        }
        else if (_byDay.Length > 0)
        {
            foreach (WeekDayNum entry in _byDay)
            {
                AddWeekdayOccurrencesInYear(year, entry, result);
            }
        }
        else if (months is not null)
        {
            foreach (int month in months)
            {
                if (TryResolveMonthDay(year, month, startDate.Day, out DateOnly day))
                {
                    result.Add(day);
                }
            }
        }
        else if (TryResolveMonthDay(year, startDate.Month, startDate.Day, out DateOnly single))
        {
            result.Add(single);
        }

        return result;
    }

    /// <summary>
    /// Applies the <c>BYSETPOS</c> selection to the sorted candidate instants of a period.
    /// </summary>
    /// <param name="instants">The sorted candidate instants of the period.</param>
    /// <returns>The selected instants in ascending order, without duplicates.</returns>
    private List<DateTime> ApplySetPos(List<DateTime> instants)
    {
        var selected = new SortedSet<DateTime>();
        foreach (int pos in _bySetPos)
        {
            int index = pos > 0 ? pos - 1 : instants.Count + pos;
            if (index >= 0 && index < instants.Count)
            {
                selected.Add(instants[index]);
            }
        }

        return [.. selected];
    }

    /// <summary>
    /// Builds the sorted set of time-of-day values for each occurrence day, expanding the <c>BYHOUR</c> /
    /// <c>BYMINUTE</c> / <c>BYSECOND</c> rule parts over the start's time components.
    /// </summary>
    /// <param name="start">The series start whose time components supply the defaults.</param>
    /// <returns>The sorted, distinct times of day.</returns>
    private TimeSpan[] BuildTimeSet(DateTime start)
    {
        int[] hours = _byHour.Length > 0 ? _byHour : [start.Hour];
        int[] minutes = _byMinute.Length > 0 ? _byMinute : [start.Minute];
        int[] seconds = _bySecond.Length > 0 ? _bySecond : [start.Second];

        var set = new SortedSet<TimeSpan>();
        foreach (int hour in hours)
        {
            foreach (int minute in minutes)
            {
                foreach (int second in seconds)
                {
                    // A BYSECOND value of 60 (leap second) collapses onto 59, which the calendar can represent.
                    set.Add(new TimeSpan(hour, minute, Math.Min(second, 59)));
                }
            }
        }

        return [.. set];
    }

    /// <summary>
    /// Determines whether the month satisfies the <c>BYMONTH</c> limit.
    /// </summary>
    /// <param name="month">The month to test.</param>
    /// <returns><see langword="true" /> when the month is allowed; otherwise <see langword="false" />.</returns>
    private bool MonthAllowed(int month) =>
        _byMonth.Length == 0 || Array.IndexOf(_byMonth, month) >= 0;

    /// <summary>
    /// Determines whether the date satisfies the <c>BYMONTHDAY</c> limit.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> when the day is allowed; otherwise <see langword="false" />.</returns>
    private bool MonthDayAllowed(DateOnly date)
    {
        if (_byMonthDay.Length == 0)
        {
            return true;
        }

        int fromEnd = date.Day - (DateTime.DaysInMonth(date.Year, date.Month) + 1);
        return Array.IndexOf(_byMonthDay, date.Day) >= 0 || Array.IndexOf(_byMonthDay, fromEnd) >= 0;
    }

    /// <summary>
    /// Determines whether the date satisfies the <c>BYYEARDAY</c> limit.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> when the year day is allowed; otherwise <see langword="false" />.</returns>
    private bool YearDayAllowed(DateOnly date)
    {
        if (_byYearDay.Length == 0)
        {
            return true;
        }

        int daysInYear = DateTime.IsLeapYear(date.Year) ? 366 : 365;
        int dayOfYear = date.DayOfYear;
        int fromEnd = dayOfYear - (daysInYear + 1);
        return Array.IndexOf(_byYearDay, dayOfYear) >= 0 || Array.IndexOf(_byYearDay, fromEnd) >= 0;
    }

    /// <summary>
    /// Determines whether the date's weekday satisfies the <c>BYDAY</c> limit, ignoring positional ordinals.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> when the weekday is allowed; otherwise <see langword="false" />.</returns>
    private bool WeekDayAllowed(DateOnly date)
    {
        if (_byDay.Length == 0)
        {
            return true;
        }

        foreach (WeekDayNum entry in _byDay)
        {
            if (entry.Day == date.DayOfWeek)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds every date in the month whose weekday and ordinal match the <c>BYDAY</c> entry.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="entry">The weekday entry.</param>
    /// <param name="result">The list the matches are appended to.</param>
    private static void AddWeekdayOccurrencesInMonth(int year, int month, WeekDayNum entry, List<DateOnly> result)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        var matches = new List<DateOnly>();
        for (int day = 1; day <= daysInMonth; day++)
        {
            var candidate = new DateOnly(year, month, day);
            if (candidate.DayOfWeek == entry.Day)
            {
                matches.Add(candidate);
            }
        }

        AddOrdinal(matches, entry.Ordinal, result);
    }

    /// <summary>
    /// Adds every date in the year whose weekday and ordinal match the <c>BYDAY</c> entry.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="entry">The weekday entry.</param>
    /// <param name="result">The list the matches are appended to.</param>
    private static void AddWeekdayOccurrencesInYear(int year, WeekDayNum entry, List<DateOnly> result)
    {
        var matches = new List<DateOnly>();
        var day = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        while (day <= end)
        {
            if (day.DayOfWeek == entry.Day)
            {
                matches.Add(day);
            }

            day = day.AddDays(1);
        }

        AddOrdinal(matches, entry.Ordinal, result);
    }

    /// <summary>
    /// Adds the ordinal-selected members of <paramref name="matches" /> to <paramref name="result" />.
    /// </summary>
    /// <param name="matches">The ordered candidate dates sharing a weekday.</param>
    /// <param name="ordinal">The ordinal selector; zero selects all.</param>
    /// <param name="result">The list the selection is appended to.</param>
    private static void AddOrdinal(List<DateOnly> matches, int ordinal, List<DateOnly> result)
    {
        if (ordinal == 0)
        {
            result.AddRange(matches);
            return;
        }

        int index = ordinal > 0 ? ordinal - 1 : matches.Count + ordinal;
        if (index >= 0 && index < matches.Count)
        {
            result.Add(matches[index]);
        }
    }

    /// <summary>
    /// Adds the days of the ISO-8601 week numbers named by <c>BYWEEKNO</c> that match the <c>BYDAY</c> weekdays.
    /// </summary>
    /// <param name="year">The period year.</param>
    /// <param name="defaultWeekday">The weekday used when <c>BYDAY</c> is absent (the start's weekday).</param>
    /// <param name="result">The list the matches are appended to.</param>
    /// <remarks>
    /// Week numbering follows ISO 8601 (Monday-based); the <c>WKST</c> rule part is not applied to it. When
    /// <c>BYDAY</c> is absent, the weekday is inherited from the series start rather than defaulting to Monday.
    /// </remarks>
    private void AddWeekNumberDays(int year, DayOfWeek defaultWeekday, List<DateOnly> result)
    {
        DayOfWeek[] weekdays = _byDay.Length > 0
            ? Array.ConvertAll(_byDay, entry => entry.Day)
            : [defaultWeekday];

        int weeksInYear = ISOWeek.GetWeeksInYear(year);
        foreach (int weekNo in _byWeekNo)
        {
            int resolved = weekNo > 0 ? weekNo : weeksInYear + weekNo + 1;
            if (resolved < 1 || resolved > weeksInYear)
            {
                continue;
            }

            foreach (DayOfWeek weekday in weekdays)
            {
                DateTime day = ISOWeek.ToDateTime(year, resolved, weekday);
                if (day.Year == year && MonthAllowed(day.Month))
                {
                    result.Add(DateOnly.FromDateTime(day));
                }
            }
        }
    }

    /// <summary>
    /// Resolves a signed <c>BYMONTHDAY</c> value to a date within the given month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="monthDay">The signed month day (positive from the start, negative from the end).</param>
    /// <param name="date">The resolved date on success.</param>
    /// <returns><see langword="true" /> when the day exists in the month; otherwise <see langword="false" />.</returns>
    private static bool TryResolveMonthDay(int year, int month, int monthDay, out DateOnly date)
    {
        date = default;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int day = monthDay > 0 ? monthDay : daysInMonth + monthDay + 1;
        if (day < 1 || day > daysInMonth)
        {
            return false;
        }

        date = new DateOnly(year, month, day);
        return true;
    }

    /// <summary>
    /// Resolves a signed <c>BYYEARDAY</c> value to a date within the given year.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="yearDay">The signed year day (positive from the start, negative from the end).</param>
    /// <param name="date">The resolved date on success.</param>
    /// <returns><see langword="true" /> when the day exists in the year; otherwise <see langword="false" />.</returns>
    private static bool TryResolveYearDay(int year, int yearDay, out DateOnly date)
    {
        date = default;
        int daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        int day = yearDay > 0 ? yearDay : daysInYear + yearDay + 1;
        if (day < 1 || day > daysInYear)
        {
            return false;
        }

        date = new DateOnly(year, 1, 1).AddDays(day - 1);
        return true;
    }

    /// <summary>
    /// Returns the start-of-week date containing <paramref name="date" />, using <see cref="WeekStart" />.
    /// </summary>
    /// <param name="date">A date within the target week.</param>
    /// <returns>The first day of the week containing <paramref name="date" />.</returns>
    private DateOnly StartOfWeek(DateOnly date)
    {
        int diff = ((int)date.DayOfWeek - (int)WeekStart + 7) % 7;
        return date.AddDays(-diff);
    }

    /// <summary>
    /// Normalizes the rule's <c>UNTIL</c> bound to a value comparable with occurrences of the given kind.
    /// </summary>
    /// <param name="until">The rule's <c>UNTIL</c> value.</param>
    /// <param name="kind">The kind of the occurrences being produced.</param>
    /// <returns>An <c>UNTIL</c> value with a matching kind.</returns>
    private static DateTime NormalizeUntil(DateTime until, DateTimeKind kind) =>
        DateTime.SpecifyKind(until, kind);
}
