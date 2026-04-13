// --------------------------------------------------------------------------------------------------------------- //
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
    /// Returns a new <see cref="DateOnly"/> representing the calendar date of the specified <paramref name="dateTime"/>,
    /// excluding any time-of-day or <see cref="DateTime.Kind"/> information.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> instance to convert.</param>
    /// <returns>
    /// A <see cref="DateOnly"/> whose value is set to the year, month, and day components of <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is supported in .NET 6.0 or later. The returned <see cref="DateOnly"/> is constructed using the
    /// <see cref="DateTime.Year"/>, <see cref="DateTime.Month"/>, and <see cref="DateTime.Day"/> components of the input.
    /// </para>
    /// <example>
    /// The following example demonstrates converting a <see cref="DateTime"/> to a <see cref="DateOnly"/>:
    /// <code>
    ///<![CDATA[
    /// var dateTime = new DateTime(2025, 7, 7, 15, 30, 0);
    /// var dateOnly = dateTime.ToDateOnly(); // 2025-07-07
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    public static DateOnly ToDateOnly(this DateTime dateTime) => DateOnly.FromDayNumber(GetDayNumber(dateTime));

#endif
}
