// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.Midnight.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing midnight (00:00:00) on the same calendar day as the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value whose date is preserved while the time is reset to midnight.</param>
    /// <returns>An object whose value is set to 00:00:00 on the same calendar day as <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>This method is functionally equivalent to <see cref="StartOfDay(DateTime)"/> and to accessing <see cref="DateTime.Date"/>. It normalises the time component to midnight while retaining the date and <see cref="DateTime.Kind"/> of the input.</para>
    /// <para><b>Example:</b></para>
    /// <code>
    ///<![CDATA[
    /// var dt = new DateTime(2025, 7, 15, 14, 45, 0);
    /// var result = dt.Midnight(); // → 2025-07-15 00:00:00
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime Midnight(this DateTime dateTime) => dateTime.Date;
}
