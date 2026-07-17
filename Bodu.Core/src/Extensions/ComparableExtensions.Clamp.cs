// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComparableExtensions.Clamp.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class ComparableExtensions
{
    /// <summary>
    /// Restricts a value to lie within a specified inclusive range.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value to clamp, which must implement <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The inclusive minimum bound, or <see langword="null" /> for no lower bound.</param>
    /// <param name="max">The inclusive maximum bound, or <see langword="null" /> for no upper bound.</param>
    /// <returns>
    /// The clamped value. If <paramref name="value" /> is less than <paramref name="min" />, <paramref name="min" /> is
    /// returned. If <paramref name="value" /> is greater than <paramref name="max" />, <paramref name="max" /> is
    /// returned. Otherwise, <paramref name="value" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when both bounds are supplied and <paramref name="min" /> is greater than <paramref name="max" />,
    /// matching the behavior of <see cref="Math.Clamp(int, int, int)" />.
    /// </exception>
    /// <remarks>
    /// If <paramref name="min" /> or <paramref name="max" /> is <see langword="null" />, the corresponding bound is
    /// treated as unbounded and no bound-ordering validation is performed for that side.
    /// </remarks>
    public static T Clamp<T>(this T value, T? min, T? max)
        where T : struct, IComparable<T>
    {
        if (min is not null && max is not null && min.Value.CompareTo(max.Value) > 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_MinGreaterThanMax, min.Value, max.Value), nameof(min));

        return (min is not null && value.CompareTo(min.Value) < 0) ? min.Value
            : (max is not null && value.CompareTo(max.Value) > 0) ? max.Value
            : value;
    }

    /// <summary>
    /// Restricts a value to lie within a specified inclusive range using a custom <see cref="IComparer{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the value to clamp.</typeparam>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The inclusive minimum bound, or <see langword="null" /> for no lower bound.</param>
    /// <param name="max">The inclusive maximum bound, or <see langword="null" /> for no upper bound.</param>
    /// <param name="comparer">The comparer to use for comparing the values.</param>
    /// <returns>
    /// The clamped value. If <paramref name="value" /> is less than <paramref name="min" />, <paramref name="min" /> is
    /// returned. If <paramref name="value" /> is greater than <paramref name="max" />, <paramref name="max" /> is
    /// returned. Otherwise, <paramref name="value" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when both bounds are supplied and <paramref name="min" /> orders after <paramref name="max" /> according
    /// to <paramref name="comparer" />.
    /// </exception>
    /// <remarks>
    /// If <paramref name="min" /> or <paramref name="max" /> is <see langword="null" />, the corresponding bound is
    /// treated as unbounded and no bound-ordering validation is performed for that side. Use this overload to apply
    /// custom comparison logic when clamping values; the bound-ordering check uses <paramref name="comparer" />, so a
    /// reversed comparer legitimately accepts bounds whose natural ordering is inverted.
    /// </remarks>
    public static T Clamp<T>(this T value, T? min, T? max, IComparer<T> comparer)
        where T : struct
    {
        ThrowHelper.ThrowIfNull(comparer);
        if (min is not null && max is not null && comparer.Compare(min.Value, max.Value) > 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_MinGreaterThanMax, min.Value, max.Value), nameof(min));

        return (min is not null && comparer.Compare(value, min.Value) < 0) ? min.Value
            : (max is not null && comparer.Compare(value, max.Value) > 0) ? max.Value
            : value;
    }
}
