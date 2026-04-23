// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.MonthName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the full name of the specified calendar month, using the formatting rules of the current culture.
    /// </summary>
    /// <param name="month">
    /// The month number to evaluate. Must be an integer between 1 and 12, where 1 represents January and 12 represents December.
    /// </param>
    /// <returns>A <see cref="string"/> containing the localized full name of the specified month, based on <see cref="CultureInfo.CurrentCulture"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="month"/> is less than 1 or greater than 12.</exception>
    /// <remarks>
    /// This method uses the <see cref="DateTimeFormatInfo.GetMonthName(int)"/> method of the current culture to retrieve the full name of
    /// the specified month.
    /// </remarks>
    public static string GetMonthName(int month) => GetMonthName(month, (CultureInfo?)null);

    /// <summary>
    /// Returns the full name of the specified calendar month, using the formatting rules of the provided culture.
    /// </summary>
    /// <param name="month">
    /// The month number to evaluate. Must be an integer between 1 and 12, where 1 represents January and 12 represents December.
    /// </param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo"/> that specifies the formatting rules. If <c>null</c>,
    /// <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>
    /// A <see cref="string"/> containing the localized full name of the specified month, based on the specified or current culture.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="month"/> is less than 1 or greater than 12.</exception>
    /// <remarks>
    /// This method uses the <see cref="DateTimeFormatInfo.GetMonthName(int)"/> method of the specified or current culture to retrieve the
    /// full name of the specified month.
    /// </remarks>
    public static string GetMonthName(int month, CultureInfo? culture)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        return (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetMonthName(month);
    }

    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateTime"/>, using the current culture's formatting rules.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> whose month component is used to retrieve the name.</param>
    /// <returns>A <see cref="string"/> containing the localized full name of the month, based on <see cref="CultureInfo.CurrentCulture"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the <see cref="DateTimeFormatInfo.GetMonthName(int)"/> method of the current culture to return the full month name.
    /// </para>
    /// <para>For culture-specific results, use the <see cref="MonthName(DateTime, CultureInfo)"/> overload.</para>
    /// </remarks>
    public static string MonthName(this DateTime dateTime) => dateTime.MonthName((CultureInfo?)null);

    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateTime"/>, using the formatting rules of the provided <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> whose month component is used to retrieve the name.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo"/> that specifies the formatting rules. If <c>null</c>,
    /// <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>A <see cref="string"/> containing the localized full name of the month, based on the specified or current culture.</returns>
    /// <remarks>
    /// This method uses the <see cref="DateTimeFormatInfo.GetMonthName(int)"/> method of the specified or current culture to retrieve the
    /// full name of the month represented by <paramref name="dateTime"/>.
    /// </remarks>
    public static string MonthName(this DateTime dateTime, CultureInfo? culture) => (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetMonthName(dateTime.Month);
}
