// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDateOfYear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the first day of the same calendar year as the specified
    /// <paramref name="date" />.
    /// </summary>
    /// <param name="date">The date value whose year is used to determine the result.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to January 1 of the same calendar year as <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method calculates the first day of the year using Gregorian calendar rules.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var date = new DateOnly(2025, 7, 15);
    /// var result = date.FirstDateOfYear(); // → 2025-01-01
    ///]]>
    /// </code>
    /// </remarks>
    public static DateOnly FirstDateOfYear(this DateOnly date) => new DateOnly(date.Year, 1, 1);
}
