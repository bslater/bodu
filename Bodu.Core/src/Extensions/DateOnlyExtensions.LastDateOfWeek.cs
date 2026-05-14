// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDateOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Extensions;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the last day of the week that contains the specified <paramref name="date"/>, using the last day of the week defined by <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    /// <param name="date">The date value used to determine the containing week.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the culturally defined last day of the week containing <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>This overload uses <see cref="CultureInfo.CurrentCulture"/> to determine the last day of the week, inferred from <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>.</para>
    /// </remarks>
    public static DateOnly LastDateOfWeek(this DateOnly date) => date.LastDateOfWeek((CultureInfo?)null);

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the last day of the week that contains the specified <paramref name="date"/>, using the last day of the week defined by the supplied or current culture.
    /// </summary>
    /// <param name="date">The date value used to determine the containing week.</param>
    /// <param name="culture">An optional <see cref="CultureInfo"/> that defines the first day of the week via <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>. If <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the culturally defined last day of the week containing <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>This method computes the day offset between <paramref name="date"/> and the culture-specific last day of the week, and adds that offset.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the resulting date is later than <see cref="DateOnly.MaxValue"/>.</exception>
    public static DateOnly LastDateOfWeek(this DateOnly date, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        DayOfWeek lastDayOfWeek = culture.DateTimeFormat.LastDayOfWeek();

        var dayNumber = date.DayNumber + (((int)lastDayOfWeek - (int)date.DayOfWeek + 7) % 7);

        if (dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the last day of the week that contains the specified <paramref name="date"/>, using a start-of-week inferred from the specified <see cref="CalendarWeekendDefinition"/>.
    /// </summary>
    /// <param name="date">The date value used to determine the containing week.</param>
    /// <param name="weekend">A <see cref="CalendarWeekendDefinition"/> used to infer the last day of the week. For example, <see cref="CalendarWeekendDefinition.SaturdaySunday"/> implies a Monday start (and therefore a Sunday end).</param>
    /// <returns>A <see cref="DateOnly"/> value set to the last day of the week containing <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>The method infers the start of the week based on the specified <paramref name="weekend"/> value, then calculates the last day as six days after the inferred start. If <see cref="CalendarWeekendDefinition.None"/> is supplied, the method defaults to using <see cref="DayOfWeek.Monday"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined <see cref="CalendarWeekendDefinition"/> value,
    /// -or- the resulting date is later than <see cref="DateOnly.MaxValue"/>.
    /// </exception>
    public static DateOnly LastDateOfWeek(this DateOnly date, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);
        DayOfWeek startOfWeek = DateTimeExtensions.GetWeekStartDay(weekend);
        var endOfWeek = (DayOfWeek)(((int)startOfWeek + 6) % 7);

        var dayNumber = date.DayNumber + (((int)endOfWeek - (int)date.DayOfWeek + 7) % 7);

        if (dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(
                nameof(date),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateOnly)));

        return DateOnly.FromDayNumber(dayNumber);
    }
}
