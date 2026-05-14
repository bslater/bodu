// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.Midday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing midday (12:00:00) on the same calendar day as the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value whose date is preserved while the time is set to midday.</param>
    /// <returns>An object whose value is set to 12:00:00.000 on the same calendar day as <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>This method replaces the time component of the input with exactly 12:00 PM (noon) while retaining the date and <see cref="DateTime.Kind"/> of the input. The returned value contains no fractional seconds or milliseconds.</para>
    /// <para><b>Example:</b></para>
    /// <code>
    ///<![CDATA[
    /// var dt = new DateTime(2025, 7, 15, 14, 45, 0);
    /// var result = dt.Midday(); // → 2025-07-15 12:00:00
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime Midday(this DateTime dateTime) => new DateTime(dateTime.Date.Ticks + (TicksPerHour * 12), dateTime.Kind);
}
