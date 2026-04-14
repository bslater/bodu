// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.Max.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a <see cref="DateTime"/> whose value is the later of the two specified values.
    /// </summary>
    /// <param name="first">The first <see cref="DateTime"/> value to compare.</param>
    /// <param name="second">The second <see cref="DateTime"/> value to compare.</param>
    /// <returns>The <see cref="DateTime"/> whose value is later. If both values are equal, <paramref name="first"/> is returned.</returns>
    /// <remarks>
    /// This method compares the two values using the greater-than-or-equal-to ( <c>&gt;=</c>) operator, which is equivalent to <see cref="DateTime.CompareTo(DateTime)"/>.
    /// </remarks>
    public static DateTime Max(DateTime first, DateTime second) => first >= second ? first : second;

    /// <summary>
    /// Returns a nullable <see cref="DateTime"/> whose value is the later of the two specified values.
    /// </summary>
    /// <param name="first">The first nullable <see cref="DateTime"/> value to compare.</param>
    /// <param name="second">The second nullable <see cref="DateTime"/> value to compare.</param>
    /// <returns>
    /// A <see cref="DateTime"/> whose value is later, or <c>null</c> if both <paramref name="first"/> and <paramref name="second"/> are <c>null</c>.
    /// </returns>
    /// <remarks>
    /// <para>If both values are non-null, they are compared using the greater-than-or-equal-to ( <c>&gt;=</c>) operator.</para>
    /// <para>If only one value is non-null, that value is returned. If both are <c>null</c>, the result is <see langword="null"/>.</para>
    /// </remarks>
#pragma warning disable S3358 // Ternary operators should not be nested

    public static DateTime? Max(DateTime? first, DateTime? second) =>
        first.HasValue && second.HasValue
            ? (first.Value >= second.Value ? first : second)
            : first ?? second;

#pragma warning restore S3358 // Ternary operators should not be nested
}
