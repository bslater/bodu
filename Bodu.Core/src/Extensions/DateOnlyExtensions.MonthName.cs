// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.MonthName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateOnly" />, using the formatting rules of the current culture.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value whose month is used to determine the name.</param>
    /// <returns>A <see cref="string" /> representing the localized full month name based on <see cref="CultureInfo.CurrentCulture" />.</returns>
    /// <remarks>
    /// This method retrieves the month name from <see cref="DateTimeFormatInfo.MonthNames" /> using the current culture's formatting.
    /// </remarks>
    public static string MonthName(this DateOnly date) => date.MonthName(null!);

    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateOnly" />, using the formatting rules of the specified <see cref="CultureInfo" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value whose month is used to determine the name.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> used to format the result. If <c>null</c>, <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>A <see cref="string" /> representing the localized full month name for the month of <paramref name="date" />.</returns>
    /// <remarks>
    /// This method retrieves the month name from the <see cref="DateTimeFormatInfo.MonthNames" /> collection of the specified or
    /// current culture.
    /// </remarks>
    public static string MonthName(this DateOnly date, CultureInfo culture) => (culture ?? System.Globalization.CultureInfo.CurrentCulture).DateTimeFormat.GetMonthName(date.Month);
}