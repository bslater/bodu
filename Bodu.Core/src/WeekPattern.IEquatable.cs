// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="WeekPattern.IEquatable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial struct WeekPattern :
    System.IEquatable<WeekPattern>,
    System.IEquatable<byte>
{
    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="WeekPattern"/>.
    /// </summary>
    /// <param name="obj">
    /// The object to compare. Accepted types are <see cref="WeekPattern"/> and <see cref="byte"/>;
    /// all other types, including <see langword="null"/>, return <see langword="false"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="WeekPattern"/> or
    /// <see cref="byte"/> with the same selected-day bitmask; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is WeekPattern pattern) return Equals(pattern);
        if (obj is byte b) return Equals(b);
        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="WeekPattern"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="WeekPattern"/> to compare with this instance.</param>
    /// <returns><see langword="true"/> if both instances have the same selected days; otherwise, <see langword="false"/>.</returns>
    public bool Equals(WeekPattern other) => _selectedDays == other._selectedDays;

    /// <summary>
    /// Determines whether the specified <see cref="byte"/> bitmask is equal to the underlying bitmask of
    /// the current instance.
    /// </summary>
    /// <param name="other">The <see cref="byte"/> value to compare.</param>
    /// <returns><see langword="true"/> if the bit patterns match; otherwise, <see langword="false"/>.</returns>
    public bool Equals(byte other) => _selectedDays == other;

    /// <summary>
    /// Returns a hash code for the current instance derived from the underlying bitmask.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code consistent with <see cref="Equals(WeekPattern)"/>.</returns>
    public override int GetHashCode() => _selectedDays.GetHashCode();
}
