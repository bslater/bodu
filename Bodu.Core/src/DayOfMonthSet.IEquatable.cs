// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfMonthSet.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct DayOfMonthSet
{
    /// <summary>
    /// Determines whether this set selects the same days as another.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true" /> when the sets are equal; otherwise <see langword="false" />.</returns>
    public bool Equals(DayOfMonthSet other) =>
        _bits == other._bits;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is DayOfMonthSet other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _bits.GetHashCode();
}
