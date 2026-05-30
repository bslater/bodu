// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.IEquatable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency> :
    IEquatable<Money<TCurrency>>,
    IComparable<Money<TCurrency>>,
    IComparable
{
    /// <summary>
    /// Determines whether this instance equals <paramref name="other" /> in both currency and amount.
    /// </summary>
    /// <param name="other">The monetary value to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when both instances share <typeparamref name="TCurrency" /> and represent the same
    /// rounded amount; otherwise <see langword="false" />.
    /// </returns>
    public bool Equals(Money<TCurrency> other) =>
        _amount == other._amount;

    /// <summary>
    /// Determines whether this instance equals the boxed <paramref name="obj" /> reference. Returns
    /// <see langword="false" /> for any <see cref="Money{TCurrency}" /> instance over a different currency.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is a <see cref="Money{TCurrency}" /> over the same
    /// currency and equal in amount; otherwise <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Money<TCurrency> other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(Money{TCurrency})" /> — combining the closed
    /// generic type identity (not the runtime <see cref="ICurrency.IsoCode" /> string) with the amount so
    /// hashing matches equality even when two different currency tag types share an ISO code or when a custom
    /// tag mutates its reported code.
    /// </summary>
    /// <returns>A 32-bit hash code suitable for use in hash-based collections.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(typeof(TCurrency), _amount);

    /// <summary>
    /// Compares this instance to <paramref name="other" /> by amount.
    /// </summary>
    /// <param name="other">The instance to compare against.</param>
    /// <returns>
    /// A negative value when this instance is smaller, zero when the two are equal, and a positive value when this
    /// instance is greater.
    /// </returns>
    public int CompareTo(Money<TCurrency> other) =>
        _amount.CompareTo(other._amount);

    /// <summary>
    /// Compares this instance to <paramref name="obj" /> when it is a same-currency <see cref="Money{TCurrency}" />.
    /// </summary>
    /// <param name="obj">The boxed value to compare against.</param>
    /// <returns>The result of <see cref="CompareTo(Money{TCurrency})" /> when <paramref name="obj" /> matches; the comparison rules of <see cref="IComparable" /> when <paramref name="obj" /> is <see langword="null" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="obj" /> is not a <see cref="Money{TCurrency}" /> over the same
    /// <typeparamref name="TCurrency" />.
    /// </exception>
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        if (obj is Money<TCurrency> other)
            return CompareTo(other);

        throw new ArgumentException(
            $"Object must be a Money<{typeof(TCurrency).Name}>.",
            nameof(obj));
    }

    /// <summary>
    /// Determines whether two monetary values are equal in amount.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when the two are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(Money<TCurrency> left, Money<TCurrency> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two monetary values differ in amount.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when the two differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(Money<TCurrency> left, Money<TCurrency> right) =>
        !left.Equals(right);
}
