// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.IComparable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial struct WeekPattern :
    System.IComparable<WeekPattern>,
    System.IComparable<byte>,
    System.IComparable
{
    /// <summary>
    /// Compares this instance to a specified object and returns an indication of their relative values.
    /// </summary>
    /// <param name="obj">
    /// An object to compare. Must be a <see cref="WeekPattern" /> or a <see cref="byte" />, or <see langword="null" />.
    /// </param>
    /// <returns>
    /// A signed integer indicating the relative order: greater than zero if this instance is greater than
    /// <paramref name="obj" /> or <paramref name="obj" /> is <see langword="null" />; zero if equal; less than zero if
    /// smaller.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="obj" /> is not a <see cref="WeekPattern" /> or <see cref="byte" />.
    /// </exception>
    /// <remarks>
    /// Ordering is based on the numeric value of the underlying bitmask and has no inherent day-of-week meaning. It is
    /// provided to support sorting and binary-search scenarios where a consistent total order is required. For
    /// domain-specific ordering, enumerate the selected days directly.
    /// </remarks>
    public int CompareTo(object? obj) =>
        obj is null
            ? 1
            : obj is WeekPattern other
                ? CompareTo(other)
                : obj is byte b
                    ? CompareTo(b)
                    : throw new ArgumentException(string.Format(ResourceStrings.Arg_Invalid_MustBeComparableType, string.Join(" or ", nameof(WeekPattern), nameof(Byte))), nameof(obj));

    /// <summary>
    /// Compares this instance to a specified <see cref="WeekPattern" /> and returns an indication of their relative
    /// values.
    /// </summary>
    /// <param name="other">A <see cref="WeekPattern" /> to compare with this instance.</param>
    /// <returns>
    /// A signed integer whose sign indicates whether this instance is less than, equal to, or greater than
    /// <paramref name="other" />.
    /// </returns>
    public readonly int CompareTo(WeekPattern other) =>
        _selectedDays.CompareTo(other._selectedDays);

    /// <summary>
    /// Compares this instance to a specified <see cref="byte" /> bitmask value and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="other">A <see cref="byte" /> bitmask to compare with the underlying value of this instance.</param>
    /// <returns>
    /// A signed integer whose sign indicates whether this instance is less than, equal to, or greater than
    /// <paramref name="other" />.
    /// </returns>
    public readonly int CompareTo(byte other) =>
        _selectedDays.CompareTo(other);
}
