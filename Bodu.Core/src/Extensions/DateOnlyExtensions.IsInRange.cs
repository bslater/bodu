// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsInRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> value falls within the range defined by <paramref name="start"/> and
    /// <paramref name="end"/>, inclusive.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> value to evaluate.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="date"/> is greater than or equal to <paramref name="start"/> and less than or equal to
    /// <paramref name="end"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The range check is inclusive, meaning the result is <see langword="true"/> if <paramref name="date"/> equals either
    /// <paramref name="start"/> or <paramref name="end"/>.
    /// </remarks>
    public static bool IsInRange(this DateOnly date, DateOnly start, DateOnly end) => date.CompareTo(start) >= 0 && date.CompareTo(end) <= 0;

    /// <summary>
    /// Determines whether the specified nullable <see cref="DateOnly"/> value falls within the range defined by <paramref name="start"/>
    /// and <paramref name="end"/>, inclusive.
    /// </summary>
    /// <param name="date">The nullable <see cref="DateOnly"/> value to evaluate.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="date"/> has a value and that value is greater than or equal to <paramref name="start"/> and less
    /// than or equal to <paramref name="end"/>; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>If <paramref name="date"/> is <see langword="null"/>, the result is <see langword="false"/>.</para>
    /// <para>The range check is inclusive, meaning the result is <see langword="true"/> if the value equals either boundary.</para>
    /// </remarks>
    public static bool IsInRange(this DateOnly? date, DateOnly start, DateOnly end) => date.HasValue && date.Value.IsInRange(start, end);
}
