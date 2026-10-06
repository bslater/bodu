// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MinuteSet.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct MinuteSet
{
    /// <summary>
    /// Determines whether this set selects the same minutes as another.
    /// </summary>
    /// <param name="other">The set to compare with.</param>
    /// <returns><see langword="true" /> when the sets are equal; otherwise <see langword="false" />.</returns>
    public bool Equals(MinuteSet other) =>
        _bits == other._bits;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is MinuteSet other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _bits.GetHashCode();
}
