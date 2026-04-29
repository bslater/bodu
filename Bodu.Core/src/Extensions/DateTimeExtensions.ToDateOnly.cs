// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.ToDateOnly.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
#if NET6_0_OR_GREATER // Uses DateOnly for .NET 6 or greater

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the calendar date of the specified <paramref name="dateTime"/>, excluding any time-of-day or <see cref="DateTime.Kind"/> information.
    /// </summary>
    /// <param name="dateTime">The date and time value to convert.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the year, month, and day components of <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// <para>This method is supported on .NET 6.0 and later. The returned <see cref="DateOnly"/> is constructed using the <see cref="DateTime.Year"/>, <see cref="DateTime.Month"/>, and <see cref="DateTime.Day"/> components of the input. Any time-of-day component and the <see cref="DateTime.Kind"/> property are discarded.</para>
    /// <para><b>Example:</b></para>
    /// <code>
    ///<![CDATA[
    /// var dt = new DateTime(2025, 7, 7, 15, 30, 0);
    /// var result = dt.ToDateOnly(); // → 2025-07-07
    ///]]>
    /// </code>
    /// </remarks>
    public static DateOnly ToDateOnly(this DateTime dateTime) => DateOnly.FromDayNumber(GetDayNumber(dateTime));

#endif
}
