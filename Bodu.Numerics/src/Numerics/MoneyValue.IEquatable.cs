// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.IEquatable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct MoneyValue :
    IEquatable<MoneyValue>,
    IComparable<MoneyValue>,
    IComparable
{
    /// <summary>
    /// Determines whether two instances are equal in both currency and amount.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when both ISO code and amount match; otherwise <see langword="false" />.</returns>
    public bool Equals(MoneyValue other) =>
        _amount == other._amount && string.Equals(IsoCode, other.IsoCode, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether this instance equals the boxed <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is a matching <see cref="MoneyValue" />.</returns>
    public override bool Equals(object? obj) =>
        obj is MoneyValue other && Equals(other);

    /// <summary>
    /// Returns a hash code combining the currency and amount.
    /// </summary>
    /// <returns>A 32-bit hash code.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(IsoCode, _amount);

    /// <summary>
    /// Compares this instance to another value of the same currency.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    /// <exception cref="InvalidOperationException">The operands have different ISO codes.</exception>
    public int CompareTo(MoneyValue other)
    {
        if (!string.Equals(IsoCode, other.IsoCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MoneyValue comparison requires the same currency; got '{IsoCode}' and '{other.IsoCode}'.");
        }

        return _amount.CompareTo(other._amount);
    }

    /// <summary>
    /// Compares this instance to a boxed value.
    /// </summary>
    /// <param name="obj">The boxed value.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    /// <exception cref="ArgumentException"><paramref name="obj" /> is not a <see cref="MoneyValue" />.</exception>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is MoneyValue other) return CompareTo(other);
        throw new ArgumentException("Object must be a MoneyValue.", nameof(obj));
    }

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(MoneyValue left, MoneyValue right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two values differ.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when they differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(MoneyValue left, MoneyValue right) =>
        !left.Equals(right);
}
