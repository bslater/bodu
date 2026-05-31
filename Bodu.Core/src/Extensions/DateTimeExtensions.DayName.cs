// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.DayName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the full name of the day of the week for the specified <see cref="DateTime" />, using the formatting
    /// rules of <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="dateTime">
    /// The date and time value whose <see cref="DateTime.DayOfWeek" /> is used to determine the name.
    /// </param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full day name, formatted using
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetDayName(DayOfWeek)" /> method of the current culture to
    /// retrieve the day name. For culture-specific results, use the <see cref="DayName(DateTime, CultureInfo)" />
    /// overload.
    /// </para>
    /// </remarks>
    public static string DayName(this DateTime dateTime) => dateTime.DayName((CultureInfo?)null);

    /// <summary>
    /// Returns the full name of the day of the week for the specified <see cref="DateTime" />, using the formatting
    /// rules of the supplied or current culture.
    /// </summary>
    /// <param name="dateTime">
    /// The date and time value whose <see cref="DateTime.DayOfWeek" /> is used to determine the name.
    /// </param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> used to format the result. If <see langword="null" />,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full day name for <paramref name="dateTime" />, formatted using
    /// the supplied or current culture.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetDayName(DayOfWeek)" /> method of the supplied or current
    /// culture to retrieve the day name.
    /// </para>
    /// </remarks>
    public static string DayName(this DateTime dateTime, CultureInfo? culture) => (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetDayName(dateTime.DayOfWeek);
}
