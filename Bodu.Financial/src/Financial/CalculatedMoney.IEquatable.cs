// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct CalculatedMoney
    : IEquatable<CalculatedMoney>
{
    /// <summary>
    /// Determines whether this value equals <paramref name="other" /> in both amount and currency.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when both the amount and currency match; otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(CalculatedMoney other) =>
        _amount == other._amount && _code == other._code;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is CalculatedMoney other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(_amount, _code);

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when equal.</returns>
    public static bool operator ==(CalculatedMoney left, CalculatedMoney right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two values differ.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when they differ.</returns>
    public static bool operator !=(CalculatedMoney left, CalculatedMoney right)
    {
        return !left.Equals(right);
    }
}
