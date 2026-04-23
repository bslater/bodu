// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsInRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime"/> value falls within the inclusive range defined by
    /// <paramref name="start"/> and <paramref name="end"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="dateTime"/> is greater than or equal to <paramref name="start"/> and less than or equal
    /// to <paramref name="end"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The range check is inclusive, meaning the result is <see langword="true"/> if <paramref name="dateTime"/> equals either
    /// <paramref name="start"/> or <paramref name="end"/>.
    /// </para>
    /// <para>
    /// If <paramref name="end"/> is earlier than <paramref name="start"/>, the method returns <see langword="false"/> for all values
    /// of <paramref name="dateTime"/>.
    /// </para>
    /// </remarks>
    public static bool IsInRange(this DateTime dateTime, DateTime start, DateTime end) => dateTime.CompareTo(start) >= 0 && dateTime.CompareTo(end) <= 0;

    /// <summary>
    /// Returns an indication whether the specified nullable <see cref="DateTime"/> value falls within the inclusive range defined by
    /// <paramref name="start"/> and <paramref name="end"/>.
    /// </summary>
    /// <param name="dateTime">The nullable date and time value to evaluate.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="dateTime"/> is not <see langword="null"/> and its value is greater than or equal to
    /// <paramref name="start"/> and less than or equal to <paramref name="end"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>If <paramref name="dateTime"/> is <see langword="null"/>, the result is <see langword="false"/>.</para>
    /// <para>The range check is inclusive, meaning the result is <see langword="true"/> if the value equals either boundary.</para>
    /// <para>
    /// If <paramref name="end"/> is earlier than <paramref name="start"/>, the method returns <see langword="false"/> for all values
    /// of <paramref name="dateTime"/>.
    /// </para>
    /// </remarks>
    public static bool IsInRange(this DateTime? dateTime, DateTime start, DateTime end) => dateTime.HasValue && dateTime.Value.IsInRange(start, end);
}
