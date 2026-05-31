// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.FirstDateOfYear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the first day of the same calendar year as the specified
    /// <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">The date and time value whose year is used to determine the result.</param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on January 1 of the same calendar year as
    /// <paramref name="dateTime" />, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the first day of the year using Gregorian calendar rules.
    /// </para>
    /// <para>
    /// The returned value has its time component normalized to midnight (00:00:00), and the original
    /// <see cref="DateTime.Kind" /> is retained.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var dt = new DateTime(2025, 7, 15, 14, 45, 0);
    /// var result = dt.FirstDateOfYear(); // → 2025-01-01 00:00:00
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime FirstDateOfYear(this DateTime dateTime) => new(GetDateTicks(dateTime.Year, 1, 1), dateTime.Kind);
}
