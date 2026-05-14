// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.MonthName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateOnly"/>, using the formatting rules of <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    /// <param name="date">The date value whose month component is used to determine the name.</param>
    /// <returns>A <see cref="string"/> containing the localized full month name, formatted using <see cref="CultureInfo.CurrentCulture"/>.</returns>
    /// <remarks>
    /// <para>This overload uses the <see cref="DateTimeFormatInfo.GetMonthName(int)"/> method of the current culture to retrieve the month name. For culture-specific results, use the <see cref="MonthName(DateOnly, CultureInfo?)"/> overload.</para>
    /// </remarks>
    public static string MonthName(this DateOnly date) => date.MonthName((CultureInfo?)null);

    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateOnly"/>, using the formatting rules of the supplied or current culture.
    /// </summary>
    /// <param name="date">The date value whose month component is used to determine the name.</param>
    /// <param name="culture">An optional <see cref="CultureInfo"/> used to format the result. If <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
    /// <returns>A <see cref="string"/> containing the localized full month name for <paramref name="date"/>, formatted using the supplied or current culture.</returns>
    /// <remarks>
    /// <para>This overload uses the <see cref="DateTimeFormatInfo.GetMonthName(int)"/> method of the supplied or current culture to retrieve the month name.</para>
    /// </remarks>
    public static string MonthName(this DateOnly date, CultureInfo? culture) => (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetMonthName(date.Month);
}
