// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalWeekQuarterProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// <summary>The calendar month (1-12) that anchors the fiscal year.</summary>
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
        _anchorMonth = month + (isFiscalYearEnd ? 1 : 0);
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
        int fiscalYear = GetFiscalYearFor(dateTime);
        long fiscalYearStartTicks = GetFiscalYearStartTicks(fiscalYear);
        int weeksFromStart = (int)((dateTime.Ticks - fiscalYearStartTicks) / DateTimeExtensions.TicksPerWeek);

        // In a 53-week year the final week computes as quarter 5; clamp it back to Q4.
        return Math.Min((weeksFromStart / 13) + 1, 4);
    }

    /// <inheritdoc />
    public int GetQuarter(DateOnly dateOnly) =>
        GetQuarter(dateOnly.ToDateTime(TimeOnly.MinValue));

    /// <inheritdoc />
    public DateTime GetQuarterEnd(DateTime dateTime) =>
        GetQuarterEnd(GetQuarter(dateTime), GetFiscalYearFor(dateTime));

    /// <inheritdoc />
    [Obsolete("Use GetQuarterEnd(int quarter, int fiscalYear).")]
    public DateTime GetQuarterEnd(int quarter) =>
        throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_NotSupported_FiscalYearRequired, "GetQuarterEnd(int quarter, int fiscalYear)"));

    /// <inheritdoc />
    public DateTime GetQuarterEnd(int quarter, int fiscalYear)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);

        DateTime start = GetQuarterStart(quarter, fiscalYear);

        // Q4 absorbs the additional week in a 53-week fiscal year; all other quarters are exactly 13 weeks.
        int weeks = (quarter == 4 && Is53WeekFiscalYear(fiscalYear)) ? 14 : 13;
        long endTicks = start.Ticks + ((long)weeks * DateTimeExtensions.TicksPerWeek) - DateTimeExtensions.TicksPerDay;

        return new DateTime(DateTimeExtensions.GetDateAsTicks(endTicks), DateTimeKind.Unspecified);
    }

    /// <inheritdoc />
    public DateOnly GetQuarterEndDate(DateOnly dateOnly) =>
        GetQuarterEnd(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

    /// <inheritdoc />
    [Obsolete("Use GetQuarterEndDate(int quarter, int fiscalYear).")]
    public DateOnly GetQuarterEndDate(int quarter) =>
        throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_NotSupported_FiscalYearRequired, "GetQuarterEndDate(int quarter, int fiscalYear)"));

    /// <inheritdoc />
    public DateOnly GetQuarterEndDate(int quarter, int fiscalYear) =>
        GetQuarterEnd(quarter, fiscalYear).ToDateOnly();

    /// <inheritdoc />
    public DateTime GetQuarterStart(DateTime dateTime) =>
        GetQuarterStart(GetQuarter(dateTime), GetFiscalYearFor(dateTime));

    /// <inheritdoc />
    [Obsolete("Use GetQuarterStart(int quarter, int fiscalYear).")]
    public DateTime GetQuarterStart(int quarter) =>
        throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_NotSupported_FiscalYearRequired, "GetQuarterStart(int quarter, int fiscalYear)"));

    /// <inheritdoc />
    public DateTime GetQuarterStart(int quarter, int fiscalYear)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);

        long offsetTicks = (long)(quarter - 1) * 13L * DateTimeExtensions.TicksPerWeek;
        long startTicks = GetFiscalYearStartTicks(fiscalYear) + offsetTicks;

        return new DateTime(DateTimeExtensions.GetDateAsTicks(startTicks), DateTimeKind.Unspecified);
    }

    /// <inheritdoc />
    public DateOnly GetQuarterStartDate(DateOnly dateOnly) =>
        GetQuarterStart(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

    /// <inheritdoc />
    [Obsolete("Use GetQuarterStartDate(int quarter, int fiscalYear).")]
    public DateOnly GetQuarterStartDate(int quarter) =>
        throw new NotSupportedException(
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_NotSupported_FiscalYearRequired, "GetQuarterStartDate(int quarter, int fiscalYear)"));

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
    private int GetFiscalYearFor(DateTime dateTime)
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
                return candidate;
        }

        throw new ArgumentOutOfRangeException(
            nameof(dateTime),
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_FiscalYearUndeterminable, dateTime));
    }
}
