// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.Min.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the earlier of two specified <see cref="DateOnly" /> values.
    /// </summary>
    /// <param name="first">The first <see cref="DateOnly" /> value to compare.</param>
    /// <param name="second">The second <see cref="DateOnly" /> value to compare.</param>
    /// <returns>
    /// The earlier of the two <see cref="DateOnly" /> values. If both values are equal, <paramref name="first" /> is
    /// returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method compares the two values using the less-than-or-equal-to (<c>&lt;=</c>) operator, which is equivalent
    /// to <see cref="DateOnly.CompareTo(DateOnly)" />.
    /// </para>
    /// </remarks>
    public static DateOnly Min(DateOnly first, DateOnly second) => first <= second ? first : second;

    /// <summary>
    /// Returns the earlier of two specified nullable <see cref="DateOnly" /> values.
    /// </summary>
    /// <param name="first">The first nullable <see cref="DateOnly" /> value to compare.</param>
    /// <param name="second">The second nullable <see cref="DateOnly" /> value to compare.</param>
    /// <returns>
    /// The earlier non-null <see cref="DateOnly" /> value, or <see langword="null" /> if both values are
    /// <see langword="null" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If both values are non-null, they are compared using the less-than-or-equal-to (<c>&lt;=</c>) operator. If only
    /// one value is non-null, that value is returned. If both are <see langword="null" />, the result is
    /// <see langword="null" />.
    /// </para>
    /// </remarks>
    public static DateOnly? Min(DateOnly? first, DateOnly? second) =>
        first.HasValue && second.HasValue
        ? (first.Value <= second.Value ? first : second)
        : first ?? second;
}
