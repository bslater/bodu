// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.FirstDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;
using System.Threading;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the first day of the week that contains the specified <see cref="DateOnly"/>, using the current culture's settings.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> for which to determine the start of the week.</param>
    /// <returns>A <see cref="DateOnly"/> representing the first day of the week that includes <paramref name="date"/>.</returns>
    /// <remarks>This method uses <see cref="CultureInfo.CurrentCulture"/> to determine the first day of the week, based on <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>.</remarks>
    public static DateOnly FirstDayOfWeek(this DateOnly date) => date.FirstDayOfWeek(null!);

    /// <summary>
    /// Returns the first day of the week that contains the specified <see cref="DateOnly"/>, using the provided culture or the current culture.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> for which to determine the start of the week.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo"/> that defines the first day of the week via
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>. If <c>null</c>, <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>A <see cref="DateOnly"/> representing the first day of the week that includes <paramref name="date"/>.</returns>
    /// <remarks>
    /// The method calculates the number of days to subtract from <paramref name="date"/> to reach the start of the week as defined by
    /// the specified or current culture. The result is validated to ensure it falls within the valid range of <see cref="DateOnly"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the calculated date is outside the valid range supported by <see cref="DateOnly"/>.</exception>
    public static DateOnly FirstDayOfWeek(this DateOnly date, CultureInfo culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

        int dayNumber = date.DayNumber - ((7 + (date.DayOfWeek - firstDayOfWeek)) % 7);

        if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns the first day of the week that contains the specified <see cref="DateOnly"/>, using the inferred start-of-week day from
    /// the provided <see cref="CalendarWeekendDefinition"/>.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> whose week context is evaluated.</param>
    /// <param name="weekend">
    /// A <see cref="CalendarWeekendDefinition"/> value used to infer the start of the week and determine the week's structure.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> value representing the first day of the week that contains <paramref name="date"/>. The returned
    /// value is normalised to midnight and preserves the date context without a time component.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined <see cref="CalendarWeekendDefinition"/> value, or if the resulting date
    /// exceeds the valid <see cref="DateOnly"/> range.
    /// </exception>
    /// <remarks>
    /// This method uses the specified <paramref name="weekend"/> to infer the first day of the week.
    /// <para>
    /// If <paramref name="weekend"/> is set to <see cref="CalendarWeekendDefinition.None"/>, the method defaults to using
    /// <see cref="DayOfWeek.Monday"/> as the start of the week.
    /// </para>
    /// The result is offset backward from <paramref name="date"/> to the most recent occurrence of the inferred first day of the week,
    /// and is returned with no time component (00:00:00).
    /// </remarks>
    public static DateOnly FirstDayOfWeek(this DateOnly date, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);
        DayOfWeek firstDayOfWeek = DateTimeExtensions.GetWeekStartDay(weekend);

        int dayNumber = date.DayNumber - ((7 + (date.DayOfWeek - firstDayOfWeek)) % 7);

        if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)));

        return DateOnly.FromDayNumber(dayNumber);
    }
}