// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalWeekQuarterProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;

namespace Bodu.Extensions;

/// <summary>
/// Provides quarter boundary logic for a week-based retail fiscal calendar using a configurable
/// <see cref="FiscalWeekPattern" /> (5–4–4, 4–5–4, or 4–4–5 week distribution).
/// </summary>
/// <remarks>
/// <para>
/// This provider describes a recurring fiscal calendar rule. Each quarter consists of exactly 13 weeks, divided into
/// three fiscal periods according to the <see cref="FiscalWeekPattern" /> supplied at construction. Quarters are
/// defined as contiguous 13-week blocks measured from the fiscal year start:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>Q1</term>
/// <description>Weeks 1–13</description>
/// </item>
/// <item>
/// <term>Q2</term>
/// <description>Weeks 14–26</description>
/// </item>
/// <item>
/// <term>Q3</term>
/// <description>Weeks 27–39</description>
/// </item>
/// <item>
/// <term>Q4</term>
/// <description>Weeks 40–52 (or 40–53 in a 53-week year)</description>
/// </item>
/// </list>
/// <para>
/// The <see cref="FiscalWeekPattern" /> controls how the 13 weeks within each quarter are divided into three fiscal
/// periods (fiscal months). It does not affect quarter start or end boundaries, but is exposed via the
/// <see cref="Pattern" /> property for consumers that require intra-quarter period logic.
/// </para>
/// <para>
/// A fiscal year may contain a 53rd week when the span between the computed fiscal year start and the equivalent start
/// in the following year exceeds 364 days. In a 53-week year, the extra week is always appended to Q4.
/// </para>
/// <para>
/// The fiscal week start day is governed by the <see cref="DayOfWeek" /> supplied to the constructor. Year-specific
/// values — the fiscal year start date, whether a given year contains 53 weeks, and quarter boundaries — are computed
/// on demand from either an explicit <c>fiscalYear</c> argument or from the input date itself.
/// </para>
/// </remarks>
public sealed class FiscalWeekQuarterProvider
    : IQuarterDefinitionProvider
{
    /// <summary>The fiscal anchor-month sentinel (13) that denotes January of the following calendar year.</summary>
    private const int JanuaryOfNextYearSentinel = 13;

    /// <summary>The calendar month (1-13) that anchors the fiscal year start.</summary>
    private readonly int _anchorMonth;

    /// <summary>The day of week on which each fiscal week begins.</summary>
    private readonly DayOfWeek _anchorDayOfWeek;

    /// <summary>Indicates whether the fiscal year starts on the nearest <see cref="_anchorDayOfWeek" /> rather than the first.</summary>
    private readonly bool _useNearestDayOfWeek;

    /// <summary>The fiscal week pattern that governs how weeks are grouped into quarters.</summary>
    private readonly FiscalWeekPattern _pattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="FiscalWeekQuarterProvider" /> class using the specified anchor
    /// month and alignment options.
    /// </summary>
    /// <param name="month">The calendar month (1–12) of the fiscal year anchor.</param>
    /// <param name="dayOfWeek">
    /// The day of the week on which each fiscal week begins. Common values are <see cref="DayOfWeek.Sunday" /> and
    /// <see cref="DayOfWeek.Saturday" />. Defaults to <see cref="DayOfWeek.Saturday" />.
    /// </param>
    /// <param name="isFiscalYearEnd">
    /// When <see langword="true" />, <paramref name="month" /> identifies the fiscal year's closing month, and the
    /// actual fiscal start month is the one that follows. When <see langword="false" />, <paramref name="month" />
    /// identifies the fiscal year's opening month directly. Defaults to <see langword="true" />.
    /// </param>
    /// <param name="useNearestDayOfWeek">
    /// <see langword="true" /> to align the fiscal year start to the occurrence of <paramref name="dayOfWeek" />
    /// nearest to the computed anchor date; <see langword="false" /> to align it to the occurrence of
    /// <paramref name="dayOfWeek" /> on or before the computed anchor date. Defaults to <see langword="true" />.
    /// </param>
    /// <param name="pattern">
    /// The week distribution pattern applied to the three fiscal periods within each quarter. Defaults to
    /// <see cref="FiscalWeekPattern.Weeks445" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// The fiscal year start for a given <c>fiscalYear</c> is derived from the first day of the anchor month in that
    /// year, then aligned to the configured fiscal week start day using one of two strategies:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// When <paramref name="useNearestDayOfWeek" /> is <see langword="true" />, the start date is aligned to the
    /// occurrence of <paramref name="dayOfWeek" /> nearest to the computed anchor date.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// When <paramref name="useNearestDayOfWeek" /> is <see langword="false" />, the start date is aligned to the
    /// occurrence of <paramref name="dayOfWeek" /> on or before the computed anchor date.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// When <paramref name="isFiscalYearEnd" /> is <see langword="true" />, the anchor is the first day of the month
    /// following <paramref name="month" />. The fiscal year still begins on the occurrence of
    /// <paramref name="dayOfWeek" /> selected by <paramref name="useNearestDayOfWeek" />, so the fiscal week boundary
    /// always coincides with <paramref name="dayOfWeek" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="month" /> is not in the range 1–12, -or- <paramref name="dayOfWeek" /> is not a
    /// defined <see cref="DayOfWeek" /> value, -or- <paramref name="pattern" /> is not a defined
    /// <see cref="FiscalWeekPattern" /> value.
    /// </exception>
    public FiscalWeekQuarterProvider(
        int month,
        DayOfWeek dayOfWeek = DayOfWeek.Saturday,
        bool isFiscalYearEnd = true,
        bool useNearestDayOfWeek = true,
        FiscalWeekPattern pattern = FiscalWeekPattern.Weeks445)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(pattern);

        _pattern = pattern;

        // When the anchor names the fiscal year's closing month, the year opens the following month. A December end
        // month therefore yields month 13 (JanuaryOfNextYearSentinel). That is a deliberate sentinel for "January of
        // the next calendar year": the cumulative day-count tables consumed by GetDateTicks/GetDayNumberUnchecked carry
        // a thirteenth entry (DaysToMonth365[12] == 365), so GetDateTicks(year, 13, 1) resolves to 1 January of the
        // following year without a separate year-rollover branch.
        _anchorMonth = month + (isFiscalYearEnd ? 1 : 0);
        Debug.Assert(_anchorMonth is >= 1 and <= JanuaryOfNextYearSentinel, "The fiscal anchor month must fall within 1-13.");

        _anchorDayOfWeek = dayOfWeek;
        _useNearestDayOfWeek = useNearestDayOfWeek;
    }

    /// <summary>
    /// Gets the week distribution pattern applied to the three fiscal periods within each quarter.
    /// </summary>
    /// <value>
    /// One of the <see cref="FiscalWeekPattern" /> values that describes how the 13 weeks of each quarter are divided
    /// into fiscal periods.
    /// </value>
    public FiscalWeekPattern Pattern => _pattern;

    /// <inheritdoc />
    public bool Is53WeekFiscalYear(int fiscalYear) =>
        ComputeIs53WeekYear(
            GetFiscalYearStartTicks(fiscalYear),
            fiscalYear,
            _anchorMonth,
            _anchorDayOfWeek,
            _useNearestDayOfWeek);

    /// <inheritdoc />
    public int GetWeeksInFiscalYear(int fiscalYear) =>
        Is53WeekFiscalYear(fiscalYear) ? 53 : 52;

    /// <inheritdoc />
    public int GetFiscalYear(DateTime dateTime) => GetFiscalYearFor(dateTime);

    /// <inheritdoc />
    public int GetFiscalYear(DateOnly dateOnly) => GetFiscalYearFor(dateOnly.ToDateTime(TimeOnly.MinValue));

    /// <inheritdoc />
    public int GetQuarter(DateTime dateTime)
    {
        (_, long startTicks, _) = ResolveFiscalContext(dateTime);
        return ComputeQuarter(dateTime.Ticks, startTicks);
    }

    /// <inheritdoc />
    public int GetQuarter(DateOnly dateOnly) =>
        GetQuarter(dateOnly.ToDateTime(TimeOnly.MinValue));

    /// <inheritdoc />
    public DateTime GetQuarterEnd(DateTime dateTime)
    {
        // Resolve the fiscal year once and derive quarter, start, and 53-week length from the same context — the
        // delegating form re-ran the three-candidate year search and week alignments up to four times per call.
        (_, long startTicks, bool is53Week) = ResolveFiscalContext(dateTime);
        return GetQuarterEndCore(startTicks, ComputeQuarter(dateTime.Ticks, startTicks), is53Week);
    }

    /// <inheritdoc />
    public DateTime GetQuarterEnd(int quarter, int fiscalYear)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);

        long startTicks = GetFiscalYearStartTicks(fiscalYear);
        bool is53Week = ComputeIs53WeekYear(startTicks, fiscalYear, _anchorMonth, _anchorDayOfWeek, _useNearestDayOfWeek);

        return GetQuarterEndCore(startTicks, quarter, is53Week);
    }

    /// <inheritdoc />
    public DateOnly GetQuarterEndDate(DateOnly dateOnly) =>
        GetQuarterEnd(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

    /// <inheritdoc />
    public DateOnly GetQuarterEndDate(int quarter, int fiscalYear) =>
        GetQuarterEnd(quarter, fiscalYear).ToDateOnly();

    /// <inheritdoc />
    public DateTime GetQuarterStart(DateTime dateTime)
    {
        (_, long startTicks, _) = ResolveFiscalContext(dateTime);
        return GetQuarterStartCore(startTicks, ComputeQuarter(dateTime.Ticks, startTicks));
    }

    /// <inheritdoc />
    public DateTime GetQuarterStart(int quarter, int fiscalYear)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);

        return GetQuarterStartCore(GetFiscalYearStartTicks(fiscalYear), quarter);
    }

    /// <inheritdoc />
    public DateOnly GetQuarterStartDate(DateOnly dateOnly) =>
        GetQuarterStart(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

    /// <inheritdoc />
    public DateOnly GetQuarterStartDate(int quarter, int fiscalYear) =>
        GetQuarterStart(quarter, fiscalYear).ToDateOnly();

    /// <summary>
    /// Returns the tick value of the occurrence of <paramref name="weekStart" /> nearest to the date represented by
    /// <paramref name="ticks" />.
    /// </summary>
    /// <param name="ticks">The tick value of the reference date.</param>
    /// <param name="weekStart">The target <see cref="DayOfWeek" /> to align to.</param>
    /// <returns>The tick value of the nearest <paramref name="weekStart" /> day.</returns>
    private static long AlignToNearestDayOfWeek(long ticks, DayOfWeek weekStart) =>
        DateTimeExtensions.GetTicksForNearestDayOfWeek(ticks, weekStart);

    /// <summary>
    /// Returns the tick value of the occurrence of <paramref name="weekStart" /> on or before the date represented by
    /// <paramref name="ticks" />.
    /// </summary>
    /// <param name="ticks">The tick value of the reference date.</param>
    /// <param name="weekStart">The target <see cref="DayOfWeek" /> to align to.</param>
    /// <returns>
    /// The tick value of the most recent <paramref name="weekStart" /> day on or before the input date.
    /// </returns>
    private static long AlignToOnOrBeforeDayOfWeek(long ticks, DayOfWeek weekStart) =>
        ticks - DateTimeExtensions.GetTicksSincePreviousOrSameDayOfWeek(ticks, weekStart);

    /// <summary>
    /// Determines whether the fiscal year that begins at <paramref name="fiscalYearStartTicks" /> spans more than 52
    /// weeks (i.e., contains a 53rd week).
    /// </summary>
    /// <param name="fiscalYearStartTicks">The tick value of the first day of the fiscal year.</param>
    /// <param name="year">The calendar year of the fiscal anchor month.</param>
    /// <param name="startMonth">
    /// The calendar month in which the fiscal year begins (already adjusted for <c>isFiscalYearEnd</c>).
    /// </param>
    /// <param name="anchorDayOfWeek">
    /// The day of the week to which the anchor in the following year is aligned. Must be the same target day used to
    /// align <paramref name="fiscalYearStartTicks" /> so the resulting span is a multiple of seven days (either 364 or
    /// 371).
    /// </param>
    /// <param name="useNearestDayOfWeek">
    /// <see langword="true" /> to align to the occurrence of <paramref name="anchorDayOfWeek" /> nearest the computed
    /// anchor; <see langword="false" /> to align to the occurrence on or before the computed anchor.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the fiscal year spans more than 364 days; otherwise, <see langword="false" />.
    /// </returns>
    private static bool ComputeIs53WeekYear(
        long fiscalYearStartTicks,
        int year,
        int startMonth,
        DayOfWeek anchorDayOfWeek,
        bool useNearestDayOfWeek)
    {
        const int DaysIn52Weeks = 364;

        long nextYearAnchorTicks = DateTimeExtensions.GetDateTicks(year + 1, startMonth, 1);

        long nextFiscalYearStartTicks = useNearestDayOfWeek
            ? AlignToNearestDayOfWeek(nextYearAnchorTicks, anchorDayOfWeek)
            : AlignToOnOrBeforeDayOfWeek(nextYearAnchorTicks, anchorDayOfWeek);

        long daysInFiscalYear =
            (nextFiscalYearStartTicks - fiscalYearStartTicks) / DateTimeExtensions.TicksPerDay;

        return daysInFiscalYear > DaysIn52Weeks;
    }

    /// <summary>
    /// Computes the tick value of the first day of the specified fiscal year using the provider's recurring calendar
    /// rule.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year whose start date is being requested.</param>
    /// <returns>The tick value of the first day of the fiscal year, aligned to the configured week start.</returns>
    private long GetFiscalYearStartTicks(int fiscalYear)
    {
        long anchorTicks = DateTimeExtensions.GetDateTicks(fiscalYear, _anchorMonth, 1);
        return _useNearestDayOfWeek
            ? AlignToNearestDayOfWeek(anchorTicks, _anchorDayOfWeek)
            : AlignToOnOrBeforeDayOfWeek(anchorTicks, _anchorDayOfWeek);
    }

    /// <summary>
    /// Resolves the fiscal year that contains <paramref name="dateTime" /> under the provider's recurring calendar
    /// rule.
    /// </summary>
    /// <param name="dateTime">The date to map to a fiscal year.</param>
    /// <returns>The fiscal year whose 52- or 53-week span contains the fiscal week of the input date.</returns>
    /// <remarks>
    /// A fiscal year can straddle at most two calendar years, so the search inspects the three candidates
    /// <c>dateTime.Year - 1</c>, <c>dateTime.Year</c>, and <c>dateTime.Year + 1</c>. The input date's fiscal week start
    /// (aligned backwards to the configured first day of the week) must fall within
    /// <c>[fiscalYearStart, fiscalYearStart + length)</c>, where <c>length</c> is 371 days in a 53-week year and 364
    /// days otherwise.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when no candidate fiscal year contains <paramref name="dateTime" />.
    /// </exception>
    private int GetFiscalYearFor(DateTime dateTime) =>
        ResolveFiscalContext(dateTime).FiscalYear;

    /// <summary>
    /// Resolves the fiscal year containing <paramref name="dateTime" /> together with the year's start tick value and
    /// 53-week flag, so date-based members pay the three-candidate search and week alignments exactly once per call.
    /// </summary>
    /// <param name="dateTime">The date to map to a fiscal year.</param>
    /// <returns>The containing fiscal year, its start tick value, and whether it spans 53 weeks.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when no candidate fiscal year contains <paramref name="dateTime" />.
    /// </exception>
    private (int FiscalYear, long StartTicks, bool Is53Week) ResolveFiscalContext(DateTime dateTime)
    {
        long weekStartTicks = dateTime.Ticks
            - DateTimeExtensions.GetTicksSincePreviousOrSameDayOfWeek(dateTime.Ticks, _anchorDayOfWeek);

        int calendarYear = dateTime.Year;
        for (int candidate = calendarYear - 1; candidate <= calendarYear + 1; candidate++)
        {
            long start = GetFiscalYearStartTicks(candidate);
            bool is53 = ComputeIs53WeekYear(start, candidate, _anchorMonth, _anchorDayOfWeek, _useNearestDayOfWeek);
            int lengthDays = is53 ? 371 : 364;

            long deltaDays = (weekStartTicks - start) / DateTimeExtensions.TicksPerDay;
            if (deltaDays >= 0 && deltaDays < lengthDays)
                return (candidate, start, is53);
        }

        throw new ArgumentOutOfRangeException(
            nameof(dateTime),
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_FiscalYearUndeterminable, dateTime));
    }

    /// <summary>
    /// Computes the 1-based fiscal quarter for a date from the containing fiscal year's start tick value.
    /// </summary>
    /// <param name="dateTicks">The tick value of the date being classified.</param>
    /// <param name="fiscalYearStartTicks">The tick value of the first day of the containing fiscal year.</param>
    /// <returns>The quarter number, from 1 through 4.</returns>
    private static int ComputeQuarter(long dateTicks, long fiscalYearStartTicks)
    {
        int weeksFromStart = (int)((dateTicks - fiscalYearStartTicks) / DateTimeExtensions.TicksPerWeek);

        // In a 53-week year the final week computes as quarter 5; clamp it back to Q4.
        return Math.Min((weeksFromStart / 13) + 1, 4);
    }

    /// <summary>
    /// Computes the first day of the specified quarter from the fiscal year's start tick value.
    /// </summary>
    /// <param name="fiscalYearStartTicks">The tick value of the first day of the fiscal year.</param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <returns>Midnight on the first day of the quarter, with <see cref="DateTimeKind.Unspecified" />.</returns>
    private static DateTime GetQuarterStartCore(long fiscalYearStartTicks, int quarter)
    {
        long startTicks = fiscalYearStartTicks + ((long)(quarter - 1) * 13L * DateTimeExtensions.TicksPerWeek);
        return new DateTime(DateTimeExtensions.GetDateAsTicks(startTicks), DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Computes the last day of the specified quarter from the fiscal year's start tick value and 53-week flag.
    /// </summary>
    /// <param name="fiscalYearStartTicks">The tick value of the first day of the fiscal year.</param>
    /// <param name="quarter">The quarter number, from 1 through 4.</param>
    /// <param name="is53WeekYear">Whether the fiscal year spans 53 weeks.</param>
    /// <returns>Midnight on the last day of the quarter, with <see cref="DateTimeKind.Unspecified" />.</returns>
    private static DateTime GetQuarterEndCore(long fiscalYearStartTicks, int quarter, bool is53WeekYear)
    {
        long startTicks = fiscalYearStartTicks + ((long)(quarter - 1) * 13L * DateTimeExtensions.TicksPerWeek);

        // Q4 absorbs the additional week in a 53-week fiscal year; all other quarters are exactly 13 weeks.
        int weeks = (quarter == 4 && is53WeekYear) ? 14 : 13;
        long endTicks = startTicks + ((long)weeks * DateTimeExtensions.TicksPerWeek) - DateTimeExtensions.TicksPerDay;

        return new DateTime(DateTimeExtensions.GetDateAsTicks(endTicks), DateTimeKind.Unspecified);
    }
}
