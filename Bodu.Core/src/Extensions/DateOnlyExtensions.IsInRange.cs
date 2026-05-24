// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsInRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls within the inclusive range defined by
    /// <paramref name="start" /> and <paramref name="end" />.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="start">The inclusive lower bound of the range.</param>
    /// <param name="end">The inclusive upper bound of the range.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> is greater than or equal to <paramref name="start" /> and
    /// less than or equal to <paramref name="end" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The range check is inclusive: the result is <see langword="true" /> if <paramref name="date" /> equals either
    /// <paramref name="start" /> or <paramref name="end" />.
    /// </para>
    /// <para>
    /// If <paramref name="end" /> is earlier than <paramref name="start" />, the method returns
    /// <see langword="false" /> for all values of <paramref name="date" />.
    /// </para>
    /// </remarks>
    public static bool IsInRange(this DateOnly date, DateOnly start, DateOnly end) => date.CompareTo(start) >= 0 && date.CompareTo(end) <= 0;

    /// <summary>
    /// Determines whether the specified nullable <see cref="DateOnly" /> falls within the inclusive range defined by
    /// <paramref name="start" /> and <paramref name="end" />.
    /// </summary>
    /// <param name="date">The nullable date value to evaluate.</param>
    /// <param name="start">The inclusive lower bound of the range.</param>
    /// <param name="end">The inclusive upper bound of the range.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> has a value that is greater than or equal to
    /// <paramref name="start" /> and less than or equal to <paramref name="end" />; otherwise, <see langword="false" />
    /// .
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="date" /> is <see langword="null" />, the result is <see langword="false" />.
    /// </para>
    /// <para>
    /// The range check is inclusive: the result is <see langword="true" /> if the value equals either boundary.
    /// </para>
    /// <para>
    /// If <paramref name="end" /> is earlier than <paramref name="start" />, the method returns
    /// <see langword="false" /> for all values of <paramref name="date" />.
    /// </para>
    /// </remarks>
    public static bool IsInRange(this DateOnly? date, DateOnly start, DateOnly end) => date.HasValue && date.Value.IsInRange(start, end);
}
