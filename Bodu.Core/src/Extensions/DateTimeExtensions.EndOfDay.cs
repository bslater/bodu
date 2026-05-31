// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.EndOfDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the end of the calendar day (23:59:59.9999999) that contains
    /// the specified <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">
    /// The date and time value whose date is preserved while the time is set to the final tick of the day.
    /// </param>
    /// <returns>
    /// An object whose value is set to 23:59:59.9999999 on the same calendar day as <paramref name="dateTime" />, with
    /// the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is calculated by extracting the date component of <paramref name="dateTime" /> and adding
    /// <c>TicksPerDay - 1</c> to represent the last representable moment of the day — one tick before midnight of the
    /// next day.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var dt = new DateTime(2024, 7, 7, 10, 30, 0);
    /// var result = dt.EndOfDay(); // → 2024-07-07 23:59:59.9999999
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime EndOfDay(this DateTime dateTime) => new DateTime(TruncateToDateTicks(dateTime) + (TicksPerDay - 1), dateTime.Kind);
}
