// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.DayName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the full name of the day of the week for the specified <see cref="DateOnly" />, using the formatting
    /// rules of <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="date">The date value whose <see cref="DateOnly.DayOfWeek" /> is used to determine the name.</param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full day name, formatted using
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetDayName(DayOfWeek)" /> method of the current culture to
    /// retrieve the day name. For culture-specific results, use the <see cref="DayName(DateOnly, CultureInfo?)" />
    /// overload.
    /// </para>
    /// </remarks>
    public static string DayName(this DateOnly date) => date.DayName(null!);

    /// <summary>
    /// Returns the full name of the day of the week for the specified <see cref="DateOnly" />, using the formatting
    /// rules of the supplied or current culture.
    /// </summary>
    /// <param name="date">The date value whose <see cref="DateOnly.DayOfWeek" /> is used to determine the name.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> used to format the result. If <see langword="null" />,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// A <see cref="string" /> containing the localized full day name for <paramref name="date" />, formatted using the
    /// supplied or current culture.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the <see cref="DateTimeFormatInfo.GetDayName(DayOfWeek)" /> method of the supplied or current
    /// culture to retrieve the day name.
    /// </para>
    /// </remarks>
    public static string DayName(this DateOnly date, CultureInfo? culture) => (culture ?? CultureInfo.CurrentCulture).DateTimeFormat.GetDayName(date.DayOfWeek);
}
