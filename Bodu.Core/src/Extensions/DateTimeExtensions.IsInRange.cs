// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsInRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls within the inclusive range defined by
    /// <paramref name="start" /> and <paramref name="end" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="start">The inclusive lower bound of the range.</param>
    /// <param name="end">The inclusive upper bound of the range.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> is greater than or equal to <paramref name="start" />
    /// and less than or equal to <paramref name="end" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The range check is inclusive: the result is <see langword="true" /> if <paramref name="dateTime" /> equals
    /// either <paramref name="start" /> or <paramref name="end" />.
    /// </para>
    /// <para>
    /// If <paramref name="end" /> is earlier than <paramref name="start" />, the method returns
    /// <see langword="false" /> for all values of <paramref name="dateTime" />.
    /// </para>
    /// </remarks>
    public static bool IsInRange(this DateTime dateTime, DateTime start, DateTime end) => dateTime.CompareTo(start) >= 0 && dateTime.CompareTo(end) <= 0;

    /// <summary>
    /// Determines whether the specified nullable <see cref="DateTime" /> falls within the inclusive range defined by
    /// <paramref name="start" /> and <paramref name="end" />.
    /// </summary>
    /// <param name="dateTime">The nullable date and time value to evaluate.</param>
    /// <param name="start">The inclusive lower bound of the range.</param>
    /// <param name="end">The inclusive upper bound of the range.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> has a value that is greater than or equal to
    /// <paramref name="start" /> and less than or equal to <paramref name="end" />; otherwise, <see langword="false" />
    /// .
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime" /> is <see langword="null" />, the result is <see langword="false" />.
    /// </para>
    /// <para>
    /// The range check is inclusive: the result is <see langword="true" /> if the value equals either boundary.
    /// </para>
    /// <para>
    /// If <paramref name="end" /> is earlier than <paramref name="start" />, the method returns
    /// <see langword="false" /> for all values of <paramref name="dateTime" />.
    /// </para>
    /// </remarks>
    public static bool IsInRange(this DateTime? dateTime, DateTime start, DateTime end) => dateTime.HasValue && dateTime.Value.IsInRange(start, end);
}
