// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Comparison.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Returns the smaller of two monetary values in the same currency.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The lesser of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static Money<TCurrency> Min(Money<TCurrency> left, Money<TCurrency> right) =>
        left._amount <= right._amount ? left : right;

    /// <summary>
    /// Returns the larger of two monetary values in the same currency.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>The greater of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static Money<TCurrency> Max(Money<TCurrency> left, Money<TCurrency> right) =>
        left._amount >= right._amount ? left : right;

    /// <summary>
    /// Clamps <paramref name="value" /> to the inclusive range <c>[min, max]</c>.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The lower bound (inclusive).</param>
    /// <param name="max">The upper bound (inclusive).</param>
    /// <returns><paramref name="value" /> when it lies within the range, otherwise the nearer bound.</returns>
    /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
    public static Money<TCurrency> Clamp(Money<TCurrency> value, Money<TCurrency> min, Money<TCurrency> max)
    {
        if (min._amount > max._amount)
            throw new ArgumentException($"Min ({min}) must be ≤ Max ({max}).", nameof(min));

        return value._amount < min._amount
            ? min
            : value._amount > max._amount
                ? max
                : value;
    }
}
