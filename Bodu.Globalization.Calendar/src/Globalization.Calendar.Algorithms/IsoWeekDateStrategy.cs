// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IsoWeekDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a weekday within an ISO-8601 week of an ISO week-year, such as the Monday of ISO week 1 or the Friday of
/// ISO week 20.
/// </summary>
/// <remarks>
/// <para>
/// The year supplied to <see cref="Calculate" /> is interpreted as the ISO week-year, so a valid result can fall in the
/// previous or following Gregorian calendar year. ISO week 53 is valid only for ISO years that contain 53 weeks; a
/// week-53 request for a 52-week year produces no occurrence.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The Monday of ISO week 1.
/// var strategy = new IsoWeekDateStrategy(1, DayOfWeek.Monday);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class IsoWeekDateStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsoWeekDateStrategy" /> class.
    /// </summary>
    /// <param name="week">The one-based ISO-8601 week number.</param>
    /// <param name="dayOfWeek">The weekday within the ISO week.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="week" /> is not between 1 and 53, or <paramref name="dayOfWeek" /> is not a defined
    /// <see cref="System.DayOfWeek" />.
    /// </exception>
    public IsoWeekDateStrategy(int week, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(week, 1, 53);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        Week = week;
        DayOfWeek = dayOfWeek;
    }

    /// <summary>
    /// Gets the one-based ISO-8601 week number.
    /// </summary>
    /// <value>The ISO week.</value>
    public int Week { get; }

    /// <summary>
    /// Gets the weekday within the ISO week.
    /// </summary>
    /// <value>The target weekday.</value>
    public DayOfWeek DayOfWeek { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        // GetIsoWeeksInYear inspects the following ISO year, so leave headroom at the representable extremes.
        if (year is < 1 or > 9998)
            return null;

        try
        {
            if (Week > DateTimeExtensions.GetIsoWeeksInYear(year))
                return null;

            // ISO weekdays run Monday(1)..Sunday(7); System.DayOfWeek has Sunday(0)..Saturday(6).
            int isoOffset = ((int)DayOfWeek == 0 ? 7 : (int)DayOfWeek) - 1;
            return DateOnly.FromDateTime(DateTimeExtensions.GetFirstDateOfIsoWeek(year, Week).AddDays(isoOffset));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
