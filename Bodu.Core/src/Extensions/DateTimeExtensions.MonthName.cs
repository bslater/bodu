// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.MonthName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the full name of the specified calendar month, using the formatting rules of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="month">
    /// The calendar month. Must be between 1 and 12, inclusive, where 1 represents January and 12 represents December.
    /// </param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full month name, formatted using
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetMonthName(int)" /> method of the current culture to
    /// retrieve the month name.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="month" /> is less than 1 or greater than 12.
    /// </exception>
    public static string GetMonthName(int month) => GetMonthName(month, (CultureInfo?)null);

    /// <summary>
    /// Returns the full name of the specified calendar month, using the formatting rules of the supplied or current
    /// culture.
    /// </summary>
    /// <param name="month">
    /// The calendar month. Must be between 1 and 12, inclusive, where 1 represents January and 12 represents December.
    /// </param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> used to format the result. If <see langword="null" />,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full month name, formatted using the supplied or current
    /// culture.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetMonthName(int)" /> method of the supplied or current
    /// culture to retrieve the month name.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="month" /> is less than 1 or greater than 12.
    /// </exception>
    public static string GetMonthName(int month, CultureInfo? culture)
    {
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        return (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetMonthName(month);
    }

    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateTime" />, using the formatting rules of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="dateTime">The date and time value whose month component is used to determine the name.</param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full month name, formatted using
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetMonthName(int)" /> method of the current culture to
    /// retrieve the month name. For culture-specific results, use the <see cref="MonthName(DateTime, CultureInfo)" />
    /// overload.
    /// </para>
    /// </remarks>
    public static string MonthName(this DateTime dateTime) => dateTime.MonthName((CultureInfo?)null);

    /// <summary>
    /// Returns the full name of the month for the specified <see cref="DateTime" />, using the formatting rules of the
    /// supplied or current culture.
    /// </summary>
    /// <param name="dateTime">The date and time value whose month component is used to determine the name.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> used to format the result. If <see langword="null" />,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full month name for <paramref name="dateTime" />, formatted
    /// using the supplied or current culture.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetMonthName(int)" /> method of the supplied or current
    /// culture to retrieve the month name.
    /// </para>
    /// </remarks>
    public static string MonthName(this DateTime dateTime, CultureInfo? culture) => (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetMonthName(dateTime.Month);
}
