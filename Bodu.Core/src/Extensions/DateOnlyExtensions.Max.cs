// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.Max.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the later of two specified <see cref="DateOnly"/> values.
    /// </summary>
    /// <param name="first">The first <see cref="DateOnly"/> value to compare.</param>
    /// <param name="second">The second <see cref="DateOnly"/> value to compare.</param>
    /// <returns>The later of the two <see cref="DateOnly"/> values. If both values are equal, <paramref name="first"/> is returned.</returns>
    /// <remarks>
    /// <para>This method compares the two values using the greater-than-or-equal-to (<c>&gt;=</c>) operator, which is equivalent to <see cref="DateOnly.CompareTo(DateOnly)"/>.</para>
    /// </remarks>
    public static DateOnly Max(DateOnly first, DateOnly second) => first >= second ? first : second;

    /// <summary>
    /// Returns the later of two specified nullable <see cref="DateOnly"/> values.
    /// </summary>
    /// <param name="first">The first nullable <see cref="DateOnly"/> value to compare.</param>
    /// <param name="second">The second nullable <see cref="DateOnly"/> value to compare.</param>
    /// <returns>The later non-null <see cref="DateOnly"/> value, or <see langword="null"/> if both values are <see langword="null"/>.</returns>
    /// <remarks>
    /// <para>If both values are non-null, they are compared using the greater-than-or-equal-to (<c>&gt;=</c>) operator. If only one value is non-null, that value is returned. If both are <see langword="null"/>, the result is <see langword="null"/>.</para>
    /// </remarks>
    public static DateOnly? Max(DateOnly? first, DateOnly? second) => first.HasValue && second.HasValue ? (first.Value >= second.Value ? first : second) : first ?? second;
}
