// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.StartOfDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the start of the calendar day that contains the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> whose date component is preserved while the time is reset to midnight.</param>
    /// <returns>
    /// A <see cref="DateTime"/> set to 00:00:00 on the same day as <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is functionally equivalent to accessing <c>dateTime.Date</c>, but unlike <c>DateTime.Date</c>, it preserves
    /// the original <see cref="DateTime.Kind"/> value (e.g., <see cref="DateTimeKind.Utc"/>, <see cref="DateTimeKind.Local"/>, or
    /// <see cref="DateTimeKind.Unspecified"/>).
    /// </para>
    /// <para>
    /// This makes the method more suitable in contexts where preserving the time zone context of a <see cref="DateTime"/> is important,
    /// such as when working with scheduled events or cross-system date calculations.
    /// </para>
    /// <para>
    /// The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.
    /// </para>
    /// <example>
    /// The following example demonstrates how <see cref="StartOfDay"/> differs from <c>DateTime.Date</c>:
    /// <code>
    ///<![CDATA[
    /// var original = new DateTime(2024, 12, 5, 10, 45, 0, DateTimeKind.Utc);
    /// var startOfDay = original.StartOfDay(); // 2024-12-05T00:00:00Z
    /// var builtInDate = original.Date;        // 2024-12-05T00:00:00 (Kind = Unspecified)
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    public static DateTime StartOfDay(this DateTime dateTime) => new DateTime(TruncateToDateTicks(dateTime), dateTime.Kind);
}
