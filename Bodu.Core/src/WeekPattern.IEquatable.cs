// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial struct WeekPattern
    : System.IEquatable<WeekPattern>,
    System.IEquatable<byte>
{
    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="WeekPattern" />.
    /// </summary>
    /// <param name="obj">
    /// The object to compare. Only another boxed <see cref="WeekPattern" /> can be equal; all other types, including a
    /// boxed <see cref="byte" /> and <see langword="null" />, return <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="WeekPattern" /> with the same selected-day
    /// bitmask; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A boxed <see cref="byte" /> is intentionally not considered equal here so that <see cref="object" />-level
    /// equality stays symmetric: <see cref="byte.Equals(object)" /> can never recognize a boxed
    /// <see cref="WeekPattern" />, so treating the reverse comparison as equal would violate the reflexive-symmetric
    /// contract of <see cref="object.Equals(object)" /> and corrupt hash-based collections. Use the strongly typed
    /// <see cref="Equals(byte)" /> overload to compare against a raw bitmask.
    /// </para>
    /// </remarks>
    public override bool Equals(object? obj) =>
        obj is WeekPattern pattern && Equals(pattern);

    /// <summary>
    /// Determines whether the specified <see cref="WeekPattern" /> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="WeekPattern" /> to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if both instances have the same selected days; otherwise, <see langword="false" />.
    /// </returns>
    public readonly bool Equals(WeekPattern other) => _selectedDays == other._selectedDays;

    /// <summary>
    /// Determines whether the specified <see cref="byte" /> bitmask is equal to the underlying bitmask of the current
    /// instance.
    /// </summary>
    /// <param name="other">The <see cref="byte" /> value to compare.</param>
    /// <returns><see langword="true" /> if the bit patterns match; otherwise, <see langword="false" />.</returns>
    public readonly bool Equals(byte other) => _selectedDays == other;

    /// <summary>
    /// Returns a hash code for the current instance derived from the underlying bitmask.
    /// </summary>
    /// <returns>An <see cref="int" /> hash code consistent with <see cref="Equals(WeekPattern)" />.</returns>
    public override readonly int GetHashCode() => _selectedDays.GetHashCode();
}
