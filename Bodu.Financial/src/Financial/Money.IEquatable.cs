// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.IEquatable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public readonly partial struct Money
    : IEquatable<Money>,
    IComparable<Money>,
    IComparable
{
    /// <summary>
    /// Determines whether two instances are equal in both currency and amount.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>
    /// <see langword="true" /> when both currency and amount match; otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(Money other) =>
        _amount == other._amount && _code == other._code;

    /// <summary>
    /// Determines whether this instance equals the boxed <paramref name="obj" />.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is a matching <see cref="Money" />.</returns>
    public override bool Equals(object? obj) =>
        obj is Money other && Equals(other);

    /// <summary>
    /// Returns a hash code combining the currency and amount.
    /// </summary>
    /// <returns>A 32-bit hash code.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(_code, _amount);

    /// <summary>
    /// Compares this instance to another value of the same currency.
    /// </summary>
    /// <param name="other">The value to compare against.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    /// <exception cref="InvalidOperationException">The operands have different currencies.</exception>
    public int CompareTo(Money other)
    {
        return _code != other._code
            ? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_MoneyCompareSameCurrencyRequired,
                    IsoCodeOrEmpty,
                    other.IsoCodeOrEmpty))
            : _amount.CompareTo(other._amount);
    }

    /// <summary>
    /// Compares this instance to a boxed value.
    /// </summary>
    /// <param name="obj">The boxed value.</param>
    /// <returns>A negative, zero, or positive value.</returns>
    /// <exception cref="ArgumentException"><paramref name="obj" /> is not a <see cref="Money" />.</exception>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is Money other
            ? CompareTo(other)
            : throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_MoneyObject, nameof(obj));
    }

    /// <summary>
    /// Determines whether two values are equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(Money left, Money right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two values differ.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when they differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(Money left, Money right)
    {
        return !left.Equals(right);
    }
}
