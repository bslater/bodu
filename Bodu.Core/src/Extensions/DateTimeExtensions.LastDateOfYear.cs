// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.LastDateOfYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the last day of the same calendar year as the specified
    /// <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">The date and time value whose year is used to determine the result.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on December 31 of the same calendar year as
    /// <paramref name="dateTime" />, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the last day of the year using Gregorian calendar rules.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code>
    ///<![CDATA[
    /// var dt = new DateTime(2025, 7, 15, 14, 45, 0);
    /// var result = dt.LastDateOfYear(); // → 2025-12-31 00:00:00
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime LastDateOfYear(this DateTime dateTime) => new DateTime(GetDateTicks(dateTime.Year, 12, 31), dateTime.Kind);
}
