// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.DayName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the full name of the day of the week for the specified <see cref="DateOnly" />, using the formatting rules of the
    /// current culture.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value whose day of the week is used to determine the name.</param>
    /// <returns>A <see cref="string" /> representing the localized full day name based on <see cref="CultureInfo.CurrentCulture" />.</returns>
    /// <remarks>This method retrieves the day name from <see cref="DateTimeFormatInfo.DayNames" /> using the current culture's formatting.</remarks>
    public static string DayName(this DateOnly date) => date.DayName((CultureInfo?)null);

    /// <summary>
    /// Returns the full name of the day of the week for the specified <see cref="DateOnly" />, using the formatting rules of the
    /// specified <see cref="CultureInfo" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value whose day of the week is used to determine the name.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> used to format the result. If <c>null</c>, <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>A <see cref="string" /> representing the localized full day name for the day of <paramref name="date" />.</returns>
    /// <remarks>
    /// This method retrieves the day name from the <see cref="DateTimeFormatInfo.DayNames" /> collection of the specified or current culture.
    /// </remarks>
    public static string DayName(this DateOnly date, CultureInfo? culture) => (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetDayName(date.DayOfWeek);
}