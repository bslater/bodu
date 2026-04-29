// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.Min.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the earlier of two specified <see cref="DateTime"/> values.
    /// </summary>
    /// <param name="first">The first <see cref="DateTime"/> value to compare.</param>
    /// <param name="second">The second <see cref="DateTime"/> value to compare.</param>
    /// <returns>The earlier of the two <see cref="DateTime"/> values. If both values are equal, <paramref name="first"/> is returned.</returns>
    /// <remarks>
    /// <para>This method compares the two values using the less-than-or-equal-to (<c>&lt;=</c>) operator, which is equivalent to <see cref="DateTime.CompareTo(DateTime)"/>. The <see cref="DateTime.Kind"/> of the selected value is preserved.</para>
    /// </remarks>
    public static DateTime Min(DateTime first, DateTime second) => first <= second ? first : second;

    /// <summary>
    /// Returns the earlier of two specified nullable <see cref="DateTime"/> values.
    /// </summary>
    /// <param name="first">The first nullable <see cref="DateTime"/> value to compare.</param>
    /// <param name="second">The second nullable <see cref="DateTime"/> value to compare.</param>
    /// <returns>The earlier non-null <see cref="DateTime"/> value, or <see langword="null"/> if both values are <see langword="null"/>.</returns>
    /// <remarks>
    /// <para>If both values are non-null, they are compared using the less-than-or-equal-to (<c>&lt;=</c>) operator. If only one value is non-null, that value is returned. If both are <see langword="null"/>, the result is <see langword="null"/>.</para>
    /// </remarks>
#pragma warning disable S3358 // Ternary operators should not be nested

    public static DateTime? Min(DateTime? first, DateTime? second) =>
        first.HasValue && second.HasValue
            ? (first.Value <= second.Value ? first : second)
            : first ?? second;

#pragma warning restore S3358 // Ternary operators should not be nested
}
