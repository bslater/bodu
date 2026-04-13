// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.FirstDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;
using System.Threading;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first day of the week that contains the specified instance, using the first
    /// day of the week defined by the current culture.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the culturally defined first day of the week containing <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>This overload uses <see cref="CultureInfo.CurrentCulture"/> to determine the first day of the week, based on <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>.</para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    public static DateTime FirstDayOfWeek(this DateTime dateTime) => dateTime.FirstDayOfWeek(null!);

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first day of the week that contains the specified instance, using the first
    /// day of the week defined by the specified or current culture.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo"/> used to determine the first day of the week. If <c>null</c>,
    /// <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the culturally defined first day of the week containing <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method computes the day offset between <paramref name="dateTime"/> and the culture-specific first day of the week, subtracts
    /// that offset, and resets the time to midnight.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the resulting date is earlier than <see cref="DateTime.MinValue"/> or later than <see cref="DateTime.MaxValue"/>.
    /// </exception>
    public static DateTime FirstDayOfWeek(this DateTime dateTime, CultureInfo culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

        long baseTicks = TruncateToDateTicks(dateTime);
        long offsetTicks = ((7 + (dateTime.DayOfWeek - firstDayOfWeek)) % 7) * TicksPerDay;

        long ticks = baseTicks - offsetTicks;

        if ((ulong)ticks > (ulong)DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateTime)));

        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first day of the week that contains the specified instance, using a
    /// start-of-week inferred from the specified <see cref="CalendarWeekendDefinition"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <param name="weekend">
    /// A <see cref="CalendarWeekendDefinition"/> used to infer the first day of the week. For example,
    /// <see cref="CalendarWeekendDefinition.SaturdaySunday"/> implies a Monday start.
    /// </param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the first day of the week containing <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>
    /// The method infers the start of the week based on the specified <paramref name="weekend"/> value. If
    /// <see cref="CalendarWeekendDefinition.None"/> is provided, the method defaults to using <see cref="DayOfWeek.Monday"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a valid <see cref="CalendarWeekendDefinition"/> value,
    /// -or- if the resulting date is earlier than <see cref="DateTime.MinValue"/> or later than <see cref="DateTime.MaxValue"/>.
    /// </exception>
    public static DateTime FirstDayOfWeek(this DateTime dateTime, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);
        DayOfWeek startOfWeek = GetWeekStartDay(weekend);

        int offsetDays = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        long dateTicks = dateTime.Ticks - (offsetDays * TicksPerDay);

        if ((ulong)dateTicks > (ulong)DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateTime)));

        return new DateTime(dateTicks, dateTime.Kind);
    }
}
