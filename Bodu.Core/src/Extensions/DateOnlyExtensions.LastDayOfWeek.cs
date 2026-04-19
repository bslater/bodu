// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.LastDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using Bodu.Globalization.Extensions;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the last day of the week that contains the specified <see cref="DateOnly"/>, using the current culture's settings.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> for which to determine the end of the week.</param>
    /// <returns>A <see cref="DateOnly"/> representing the last day of the week that includes <paramref name="date"/>.</returns>
    /// <remarks>This method uses <see cref="CultureInfo.CurrentCulture"/> to determine the last day of the week, inferred from <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>.</remarks>
    public static DateOnly LastDayOfWeek(this DateOnly date) => date.LastDayOfWeek(null!);

    /// <summary>
    /// Returns the last day of the week that contains the specified <see cref="DateOnly"/>, using the provided or current culture.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> for which to determine the end of the week.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo"/> used to determine the first day of the week via
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>. If <c>null</c>, <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>A <see cref="DateOnly"/> representing the last day of the week that includes <paramref name="date"/>.</returns>
    /// <remarks>
    /// This method calculates the number of days to add to <paramref name="date"/> to reach the culturally defined last day of the week.
    /// The result is validated to ensure it falls within the valid range of <see cref="DateOnly"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the calculated result exceeds the supported range for <see cref="DateOnly"/>.</exception>
    public static DateOnly LastDayOfWeek(this DateOnly date, CultureInfo culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        DayOfWeek lastDayOfWeek = culture.DateTimeFormat.LastDayOfWeek();

        int dayNumber = date.DayNumber + (((int)lastDayOfWeek - (int)date.DayOfWeek + 7) % 7);

        if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns the last day of the week that contains the specified <see cref="DateOnly"/>, using the inferred start-of-week day from the
    /// provided <see cref="CalendarWeekendDefinition"/>.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> whose week context is evaluated.</param>
    /// <param name="weekend">
    /// A <see cref="CalendarWeekendDefinition"/> value used to infer the start of the week and determine the week's structure.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> value representing the last day of the week that contains <paramref name="date"/>. The returned value is
    /// normalized to midnight and preserves the date context without a time component.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined <see cref="CalendarWeekendDefinition"/> value, or if the resulting date
    /// exceeds the valid <see cref="DateOnly"/> range.
    /// </exception>
    /// <remarks>
    /// This method uses the specified <paramref name="weekend"/> to infer the first day of the week, then calculates the last day as 6
    /// days after the inferred start.
    /// <para>
    /// If <paramref name="weekend"/> is set to <see cref="CalendarWeekendDefinition.None"/>, the method defaults to using
    /// <see cref="DayOfWeek.Monday"/> as the start of the week.
    /// </para>
    /// The result is offset forward from <paramref name="date"/> to the next occurrence of the inferred last day of the week, and is
    /// returned with no time component (00:00:00).
    /// </remarks>
    public static DateOnly LastDayOfWeek(this DateOnly date, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);
        DayOfWeek startOfWeek = DateTimeExtensions.GetWeekStartDay(weekend);
        var endOfWeek = (DayOfWeek)(((int)startOfWeek + 6) % 7);

        int dayNumber = date.DayNumber + (((int)endOfWeek - (int)date.DayOfWeek + 7) % 7);

        if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)));

        return DateOnly.FromDayNumber(dayNumber);
    }
}
