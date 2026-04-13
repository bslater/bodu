// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

/// <summary>
/// Provides quarter boundary logic for a week-based retail fiscal calendar using a configurable
/// <see cref="FiscalWeekPattern" /> (5–4–4, 4–5–4, or 4–4–5 week distribution).
/// </summary>
/// <remarks>
/// <para>
/// This provider implements a fiscal calendar in which each quarter consists of exactly 13 weeks, divided
/// into three fiscal periods according to the <see cref="FiscalWeekPattern" /> supplied at construction.
/// Quarters are defined as contiguous 13-week blocks measured from the configured fiscal year start:
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
/// The <see cref="FiscalWeekPattern" /> controls how the 13 weeks within each quarter are divided into
/// three fiscal periods (fiscal months). It does not affect quarter start or end boundaries, but is
/// exposed via the <see cref="Pattern" /> property for consumers that require intra-quarter period logic.
/// </para>
/// <para>
/// The fiscal year may contain a 53rd week when the span between the computed fiscal year start and the
/// equivalent start in the following year exceeds 364 days. In a 53-week year, the extra week is always
/// appended to Q4.
/// </para>
/// <para>
/// The fiscal week start day is governed by the <see cref="DayOfWeek" /> supplied to the constructor.
/// The anchor date is automatically aligned to the nearest occurrence of that day on or before the
/// computed start of the fiscal year.
/// </para>
/// </remarks>
public sealed class FiscalWeekQuarterProvider : IQuarterDefinitionProvider
{
    private readonly DayOfWeek _firstDayOfWeek;
    private readonly long _fiscalYearStartTicks;
    private readonly bool _is53WeekFiscalYear;
    private readonly FiscalWeekPattern _pattern;

    /// <summary>
    /// Initialises a new instance of the <see cref="FiscalWeekQuarterProvider" /> class using the
    /// specified fiscal year anchor parameters and aligning the fiscal year start to the configured
    /// fiscal week start day.
    /// </summary>
    /// <param name="year">The calendar year of the fiscal year anchor month.</param>
    /// <param name="month">The calendar month (1–12) of the fiscal year anchor.</param>
    /// <param name="dayOfWeek">
    /// The day of the week on which each fiscal week begins. Common values are
    /// <see cref="DayOfWeek.Sunday" /> and <see cref="DayOfWeek.Saturday" />.
    /// Defaults to <see cref="DayOfWeek.Saturday" />.
    /// </param>
    /// <param name="isFiscalYearEnd">
    /// When <see langword="true" />, the <paramref name="year" /> and <paramref name="month" /> identify
    /// the fiscal year's closing month, and the actual fiscal start date is derived from the month that
    /// follows. When <see langword="false" />, they identify the fiscal year's opening month directly.
    /// Defaults to <see langword="true" />.
    /// </param>
    /// <param name="useNearestDayOfWeek">
    /// <see langword="true" /> to align the fiscal year start to the occurrence of
    /// <paramref name="dayOfWeek" /> nearest to the computed anchor date;
    /// <see langword="false" /> to align it to the occurrence of <paramref name="dayOfWeek" />
    /// on or before the computed anchor date.
    /// Defaults to <see langword="true" />.
    /// </param>
    /// <param name="pattern">
    /// The week distribution pattern applied to the three fiscal periods within each quarter.
    /// Defaults to <see cref="FiscalWeekPattern.Weeks445" />.
    /// </param>
    /// <remarks>
    /// <para>
    /// The fiscal year start date is derived from the computed fiscal anchor and then aligned to the
    /// configured fiscal week start day using one of two strategies:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// When <paramref name="useNearestDayOfWeek" /> is <see langword="true" />, the start date is aligned
    /// to the occurrence of <paramref name="dayOfWeek" /> nearest to the computed anchor date. This
    /// aligned date may fall up to three days before or after the anchor, depending on which occurrence
    /// is closer.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// When <paramref name="useNearestDayOfWeek" /> is <see langword="false" />, the start date is aligned
    /// to the occurrence of <paramref name="dayOfWeek" /> on or before the computed anchor date.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// When <paramref name="isFiscalYearEnd" /> is <see langword="true" />, the anchor is the first day
    /// of the month following <paramref name="month" />, and the first day of the fiscal week is shifted
    /// forward by one day relative to <paramref name="dayOfWeek" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dayOfWeek" /> is not a defined <see cref="DayOfWeek" /> value,
    /// -or- <paramref name="pattern" /> is not a defined <see cref="FiscalWeekPattern" /> value.
    /// </exception>
    public FiscalWeekQuarterProvider(
        int year,
        int month,
        DayOfWeek dayOfWeek = DayOfWeek.Saturday,
        bool isFiscalYearEnd = true,
        bool useNearestDayOfWeek = true,
        FiscalWeekPattern pattern = FiscalWeekPattern.Weeks445)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(pattern);

        this._pattern = pattern;

        // When isFiscalYearEnd is true, the fiscal year begins in the month following the anchor month.
        // The first day of each fiscal week is therefore one day after the anchor day-of-week.
        int startMonth = month + (isFiscalYearEnd ? 1 : 0);
        this._firstDayOfWeek = isFiscalYearEnd ? (DayOfWeek)(((int)dayOfWeek + 1) % 7) : dayOfWeek;

        this._fiscalYearStartTicks = useNearestDayOfWeek
            ? AlignToNearestDayOfWeek(
                DateTimeExtensions.GetDateTicks(year, startMonth, 1),
                dayOfWeek)
            : AlignToOnOrBeforeDayOfWeek(
                DateTimeExtensions.GetDateTicks(year, startMonth, 1),
                dayOfWeek);

        this._is53WeekFiscalYear = ComputeIs53WeekYear(this._fiscalYearStartTicks, year, startMonth, this._firstDayOfWeek, useNearestDayOfWeek);
    }

    /// <summary>
    /// Gets a value indicating whether the configured fiscal year contains 53 weeks rather than the
    /// standard 52.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the fiscal year spans 371 days (53 complete weeks);
    /// <see langword="false" /> if it spans 364 days (52 complete weeks).
    /// </value>
    public bool Is53WeekFiscalYear => this._is53WeekFiscalYear;

    /// <summary>
    /// Gets the week distribution pattern applied to the three fiscal periods within each quarter.
    /// </summary>
    /// <value>
    /// One of the <see cref="FiscalWeekPattern" /> values that describes how the 13 weeks of each
    /// quarter are divided into fiscal periods.
    /// </value>
    public FiscalWeekPattern Pattern => this._pattern;

    /// <summary>
    /// Gets the total number of weeks in the fiscal year.
    /// </summary>
    /// <value>53 in a 53-week fiscal year; otherwise, 52.</value>
    public int WeeksInFiscalYear => this._is53WeekFiscalYear ? 53 : 52;

    /// <inheritdoc />
    public int GetQuarter(DateTime dateTime)
    {
        this.ValidateDateInFiscalYear(dateTime);

        int weeksFromStart = (int)((dateTime.Ticks - this._fiscalYearStartTicks) / DateTimeExtensions.TicksPerWeek);
        int quarter = (weeksFromStart / 13) + 1;

        // In a 53-week year the final week computes as quarter 5; clamp it back to Q4.
        if (quarter > 4)
            quarter = 4;

        return quarter;
    }

    /// <inheritdoc />
    public int GetQuarter(DateOnly dateOnly) => this.GetQuarter(dateOnly.ToDateTime(TimeOnly.MinValue));

    /// <inheritdoc />
    public DateTime GetQuarterEnd(DateTime dateTime) => this.GetQuarterEnd(this.GetQuarter(dateTime));

    /// <inheritdoc />
    public DateTime GetQuarterEnd(int quarter)
    {
        DateTime start = this.GetQuarterStart(quarter);

        // Q4 absorbs the additional week in a 53-week fiscal year; all other quarters are exactly 13 weeks.
        int weeks = (quarter == 4 && this._is53WeekFiscalYear) ? 14 : 13;
        long endTicks = start.Ticks + ((long)weeks * DateTimeExtensions.TicksPerWeek) - DateTimeExtensions.TicksPerDay;

        return new DateTime(DateTimeExtensions.GetDateAsTicks(endTicks), DateTimeKind.Unspecified);
    }

    /// <inheritdoc />
    public DateOnly GetQuarterEndDate(DateOnly dateOnly) => this.GetQuarterEnd(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

    /// <inheritdoc />
    public DateOnly GetQuarterEndDate(int quarter) => this.GetQuarterEnd(quarter).ToDateOnly();

    /// <inheritdoc />
    public DateTime GetQuarterStart(DateTime dateTime)
    {
        this.ValidateDateInFiscalYear(dateTime);
        return this.GetQuarterStart(this.GetQuarter(dateTime));
    }

    /// <inheritdoc />
    public DateTime GetQuarterStart(int quarter)
    {
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);

        long offsetTicks = (long)(quarter - 1) * 13L * DateTimeExtensions.TicksPerWeek;
        long startTicks = this._fiscalYearStartTicks + offsetTicks;

        return new DateTime(DateTimeExtensions.GetDateAsTicks(startTicks), DateTimeKind.Unspecified);
    }

    /// <inheritdoc />
    public DateOnly GetQuarterStartDate(DateOnly dateOnly) => this.GetQuarterStart(dateOnly.ToDateTime(TimeOnly.MinValue)).ToDateOnly();

    /// <inheritdoc />
    public DateOnly GetQuarterStartDate(int quarter) => this.GetQuarterStart(quarter).ToDateOnly();

    /// <summary>
    /// Returns the tick value of the nearest occurrence of <paramref name="weekStart" /> on or before the
    /// date represented by <paramref name="ticks" />.
    /// </summary>
    /// <param name="ticks">The tick value of the reference date.</param>
    /// <param name="weekStart">The target <see cref="DayOfWeek" /> to align to.</param>
    /// <returns>
    /// The tick value of the most recent <paramref name="weekStart" /> day on or before the input date.
    /// </returns>
    private static long AlignToNearestDayOfWeek(long ticks, DayOfWeek weekStart) =>
        DateTimeExtensions.GetTicksForNearestDayOfWeek(ticks, weekStart);

    /// <summary>
    /// Returns the tick value of the nearest occurrence of <paramref name="weekStart" /> on or before the
    /// date represented by <paramref name="ticks" />.
    /// </summary>
    /// <param name="ticks">The tick value of the reference date.</param>
    /// <param name="weekStart">The target <see cref="DayOfWeek" /> to align to.</param>
    /// <returns>
    /// The tick value of the most recent <paramref name="weekStart" /> day on or before the input date.
    /// </returns>
    private static long AlignToOnOrBeforeDayOfWeek(long ticks, DayOfWeek weekStart) =>
        ticks- DateTimeExtensions.GetTicksSincePreviousOrSameDayOfWeek(ticks, weekStart);

    /// <summary>
    /// Determines whether the fiscal year that begins at <paramref name="fiscalYearStartTicks" /> spans
    /// more than 52 weeks (i.e., contains a 53rd week).
    /// </summary>
    /// <param name="fiscalYearStartTicks">The tick value of the first day of the current fiscal year.</param>
    /// <param name="year">The calendar year of the fiscal anchor month.</param>
    /// <param name="startMonth">
    /// The calendar month in which the fiscal year begins (already adjusted for <c>isFiscalYearEnd</c>).
    /// </param>
    /// <param name="firstDayOfWeek">
    /// The fiscal week start day, used to align the equivalent anchor in the following year.
    /// </param>
    /// <param name="useNearestDayOfWeek">
    /// <see langword="true" /> to align the fiscal year start to the occurrence of
    /// <paramref name="firstDayOfWeek" /> nearest to the computed anchor date;
    /// <see langword="false" /> to align it to the occurrence of <paramref name="firstDayOfWeek" />
    /// on or before the computed anchor date.
    /// Defaults to <see langword="true" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the fiscal year spans more than 364 days; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool ComputeIs53WeekYear(
        long fiscalYearStartTicks,
        int year,
        int startMonth,
        DayOfWeek firstDayOfWeek,
        bool useNearestDayOfWeek)
    {
        const int DaysIn52Weeks = 364;

        long nextYearAnchorTicks = DateTimeExtensions.GetDateTicks(year + 1, startMonth, 1);

        long nextFiscalYearStartTicks = useNearestDayOfWeek
            ? AlignToNearestDayOfWeek(nextYearAnchorTicks, firstDayOfWeek)
            : AlignToOnOrBeforeDayOfWeek(nextYearAnchorTicks, firstDayOfWeek);

        long daysInFiscalYear =
            (nextFiscalYearStartTicks - fiscalYearStartTicks) / DateTimeExtensions.TicksPerDay;

        return daysInFiscalYear > DaysIn52Weeks;
    }

    /// <summary>
    /// Validates that <paramref name="dateTime" /> falls within the bounds of the fiscal year configured
    /// for this provider instance.
    /// </summary>
    /// <param name="dateTime">The date and time to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dateTime" /> does not fall within the fiscal year defined by this
    /// provider.
    /// </exception>
    private void ValidateDateInFiscalYear(DateTime dateTime)
    {
        // Align the input date back to the start of its fiscal week. A date belongs to a fiscal year
        // if the week in which it falls begins within [fiscalYearStart, fiscalYearStart + yearLength).
        long weekStartTicks = dateTime.Ticks
            - DateTimeExtensions.GetTicksSincePreviousOrSameDayOfWeek(dateTime.Ticks, this._firstDayOfWeek);

        long deltaDays = (weekStartTicks - this._fiscalYearStartTicks) / DateTimeExtensions.TicksPerDay;
        int fiscalYearLengthDays = this._is53WeekFiscalYear ? 371 : 364;

        if (deltaDays < 0 || deltaDays >= fiscalYearLengthDays)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(
                    ResourceStrings.Arg_OutOfRange_DateOutsideFiscalYear,
                    dateTime,
                    new DateTime(this._fiscalYearStartTicks, DateTimeKind.Unspecified)));
        }
    }
}
