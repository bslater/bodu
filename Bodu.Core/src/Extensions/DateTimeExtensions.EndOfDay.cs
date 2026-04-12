// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.EndOfDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the final possible tick of the same calendar day as the specified instance.
    /// </summary>
    /// <param name="dateTime">The date and time value whose day is used to determine the end-of-day timestamp.</param>
    /// <returns>
    /// An object whose value is set to 23:59:59.9999999 on the same calendar day as <paramref name="dateTime" />, with the original
    /// <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns a <see cref="DateTime" /> instance representing the last representable moment of the specified day,
    /// with a time component of 23:59:59.9999999—one tick before midnight.
    /// </para>
    /// <para>
    /// The result is calculated by extracting the date component from <paramref name="dateTime" /> and adding
    /// <c>TicksPerDay - 1</c> to represent the end of that day.
    /// </para>
    /// <para>
    /// The <see cref="DateTime.Kind" /> property of the returned instance matches that of the original <paramref name="dateTime" />.
    /// </para>
    /// <para><b>Examples:</b></para>
    /// <code>
    ///<![CDATA[
    /// var input = new DateTime(2024, 7, 7, 10, 30, 0);
    /// var end = input.EndOfDay(); // → 2024-07-07 23:59:59.9999999
    ///
    /// var utcInput = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    /// var utcEnd = utcInput.EndOfDay(); // → 2024-01-01 23:59:59.9999999 (UTC)
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime EndOfDay(this DateTime dateTime) => new DateTime(TruncateToDateTicks(dateTime) + (TicksPerDay - 1), dateTime.Kind);
}
