// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDateOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the week that contains the specified
    /// <paramref name="date" />, using the first day of the week defined by <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="date">The date value used to determine the containing week.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the culturally defined first day of the week containing
    /// <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses <see cref="CultureInfo.CurrentCulture" /> to determine the first day of the week, based on
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek" />.
    /// </para>
    /// </remarks>
    public static DateOnly FirstDateOfWeek(this DateOnly date) => date.FirstDateOfWeek((CultureInfo?)null);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the week that contains the specified
    /// <paramref name="date" />, using the first day of the week defined by the supplied or current culture.
    /// </summary>
    /// <param name="date">The date value used to determine the containing week.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> that defines the first day of the week via
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek" />. If <see langword="null" />,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the culturally defined first day of the week containing
    /// <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method computes the day offset between <paramref name="date" /> and the culture-specific first day of the
    /// week, and subtracts that offset.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the resulting date is earlier than <see cref="DateOnly.MinValue" />.
    /// </exception>
    public static DateOnly FirstDateOfWeek(this DateOnly date, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        DayOfWeek firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

        var dayNumber = date.DayNumber - ((7 + (date.DayOfWeek - firstDayOfWeek)) % 7);

        return dayNumber < DateOnly.MinValue.DayNumber
            ? throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)))
            : DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the week that contains the specified
    /// <paramref name="date" />, using a start-of-week inferred from the specified <see cref="WorkingDaysOfWeek" />.
    /// </summary>
    /// <param name="date">The date value used to determine the containing week.</param>
    /// <param name="workingWeek">
    /// A <see cref="WorkingDaysOfWeek" /> used to infer the first day of the week. For example,
    /// <see cref="WorkingDaysOfWeek.MondayToFriday" /> implies a Monday start.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first day of the week containing <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method infers the start of the week based on the specified <paramref name="workingWeek" /> value. If
    /// <see cref="WorkingDaysOfWeek.AllDays" /> is supplied, the method defaults to using
    /// <see cref="DayOfWeek.Monday" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined <see cref="WorkingDaysOfWeek" /> value, -or- the
    /// resulting date is earlier than <see cref="DateOnly.MinValue" />.
    /// </exception>
    public static DateOnly FirstDateOfWeek(this DateOnly date, WorkingDaysOfWeek workingWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(workingWeek);
        DayOfWeek firstDayOfWeek = DateTimeExtensions.GetWeekStartDay(workingWeek);

        var dayNumber = date.DayNumber - ((7 + (date.DayOfWeek - firstDayOfWeek)) % 7);

        return dayNumber < DateOnly.MinValue.DayNumber
            ? throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)))
            : DateOnly.FromDayNumber(dayNumber);
    }
}
