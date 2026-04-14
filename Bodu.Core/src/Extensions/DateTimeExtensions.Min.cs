// --------------------------------------------------------------------------------------------------------------- //
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
    /// <returns>
    /// An object whose value is set to the earlier of the two <see cref="DateTime"/> values. If both values are equal,
    /// <paramref name="first"/> is returned.
    /// </returns>
    /// <remarks>
    /// This method compares the values using <see cref="DateTime.CompareTo(DateTime)"/> and returns the earlier one. The result preserves
    /// the original <see cref="DateTime.Kind"/> of the selected value.
    /// </remarks>
    public static DateTime Min(DateTime first, DateTime second) => first <= second ? first : second;

    /// <summary>
    /// Returns the earlier of two specified nullable <see cref="DateTime"/> values.
    /// </summary>
    /// <param name="first">The first nullable <see cref="DateTime"/> value to compare.</param>
    /// <param name="second">The second nullable <see cref="DateTime"/> value to compare.</param>
    /// <returns>
    /// An object whose value is set to the earlier of the two non-null <see cref="DateTime"/> values, or <c>null</c> if both are <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>If both values are non-null, they are compared using <see cref="DateTime.CompareTo(DateTime)"/> and the earlier is returned.</para>
    /// <para>If only one value is non-null, it is returned. If both are <c>null</c>, the result is <see langword="null"/>.</para>
    /// </remarks>
#pragma warning disable S3358 // Ternary operators should not be nested

    public static DateTime? Min(DateTime? first, DateTime? second) =>
        first.HasValue && second.HasValue
            ? (first.Value <= second.Value ? first : second)
            : first ?? second;

#pragma warning restore S3358 // Ternary operators should not be nested
}
