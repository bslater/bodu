// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.FirstDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first day of the week that contains the specified
    /// <paramref name="dateTime" />, using the first day of the week defined by
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the culturally defined first day of the week containing
    /// <paramref name="dateTime" />, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses <see cref="CultureInfo.CurrentCulture" /> to determine the first day of the week, based on
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek" />.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// </remarks>
    public static DateTime FirstDateOfWeek(this DateTime dateTime) => dateTime.FirstDateOfWeek((CultureInfo?)null);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first day of the week that contains the specified
    /// <paramref name="dateTime" />, using the first day of the week defined by the supplied or current culture.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> that defines the first day of the week via
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek" />. If <see langword="null" />,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the culturally defined first day of the week containing
    /// <paramref name="dateTime" />, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method computes the day offset between <paramref name="dateTime" /> and the culture-specific first day of
    /// the week, subtracts that offset, and resets the time to midnight.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the resulting date is earlier than <see cref="DateTime.MinValue" /> or later than
    /// <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static DateTime FirstDateOfWeek(this DateTime dateTime, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

        long baseTicks = TruncateToDateTicks(dateTime);
        long offsetTicks = (7 + (dateTime.DayOfWeek - firstDayOfWeek)) % 7 * TicksPerDay;

        long ticks = baseTicks - offsetTicks;

        return (ulong)ticks > (ulong)DateTime.MaxValue.Ticks
            ? throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateTime)))
            : new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first day of the week that contains the specified
    /// <paramref name="dateTime" />, using a start-of-week inferred from the specified <see cref="WorkingDaysOfWeek" />
    /// .
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <param name="workingWeek">
    /// A <see cref="WorkingDaysOfWeek" /> used to infer the first day of the week. For example,
    /// <see cref="WorkingDaysOfWeek.MondayToFriday" /> implies a Monday start.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first day of the week containing
    /// <paramref name="dateTime" />, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method infers the start of the week based on the specified <paramref name="workingWeek" /> value. If
    /// <see cref="WorkingDaysOfWeek.AllDays" /> is supplied, the method defaults to using
    /// <see cref="DayOfWeek.Monday" />.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined <see cref="WorkingDaysOfWeek" /> value, -or- the
    /// resulting date is earlier than <see cref="DateTime.MinValue" /> or later than <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static DateTime FirstDateOfWeek(this DateTime dateTime, WorkingDaysOfWeek workingWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(workingWeek);
        DayOfWeek startOfWeek = GetWeekStartDay(workingWeek);

        int offsetDays = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        long dateTicks = dateTime.Ticks - (offsetDays * TicksPerDay);

        return (ulong)dateTicks > (ulong)DateTime.MaxValue.Ticks
            ? throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateTime)))
            : new DateTime(dateTicks, dateTime.Kind);
    }
}
