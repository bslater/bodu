// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.Max.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the later of two specified <see cref="DateOnly" /> values.
    /// </summary>
    /// <param name="first">The first <see cref="DateOnly" /> value to compare.</param>
    /// <param name="second">The second <see cref="DateOnly" /> value to compare.</param>
    /// <returns>The later of the two <see cref="DateOnly" /> values. If both values are equal, <paramref name="first" /> is returned.</returns>
    /// <remarks>This method compares two non-null <see cref="DateOnly" /> values using <see cref="DateOnly.CompareTo(DateOnly)" />.</remarks>
    public static DateOnly Max(DateOnly first, DateOnly second) => first >= second ? first : second;

    /// <summary>
    /// Returns the later of two nullable <see cref="DateOnly" /> values.
    /// </summary>
    /// <param name="first">The first nullable <see cref="DateOnly" /> value to compare.</param>
    /// <param name="second">The second nullable <see cref="DateOnly" /> value to compare.</param>
    /// <returns>The later non-null <see cref="DateOnly" /> value, or <c>null</c> if both values are <c>null</c>.</returns>
    /// <remarks>
    /// If both values are non-null, they are compared using <see cref="DateOnly.CompareTo(DateOnly)" />. If only one is non-null, that
    /// value is returned.
    /// </remarks>
    public static DateOnly? Max(DateOnly? first, DateOnly? second) => first.HasValue && second.HasValue ? (first.Value >= second.Value ? first : second) : first ?? second;
}