// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T>
{
    /// <summary>
    /// Gets the inclusive lower bound. Meaningful only for a non-empty, lower-bounded interval.
    /// </summary>
    /// <value>The smallest integer in the interval.</value>
    public T First =>
        _first;

    /// <summary>
    /// Gets the inclusive upper bound. Meaningful only for a non-empty, upper-bounded interval.
    /// </summary>
    /// <value>The largest integer in the interval.</value>
    public T Last =>
        _last;

    /// <summary>
    /// Gets a value indicating whether the lower side is unbounded (extends to <c>-&#x221E;</c>).
    /// </summary>
    /// <value><see langword="true" /> when lower-unbounded; otherwise <see langword="false" />.</value>
    public bool LowerUnbounded =>
        (_flags & LowerUnboundedFlag) != 0;

    /// <summary>
    /// Gets a value indicating whether the upper side is unbounded (extends to <c>+&#x221E;</c>).
    /// </summary>
    /// <value><see langword="true" /> when upper-unbounded; otherwise <see langword="false" />.</value>
    public bool UpperUnbounded =>
        (_flags & UpperUnboundedFlag) != 0;

    /// <summary>
    /// Gets a value indicating whether both sides are bounded by a finite integer.
    /// </summary>
    /// <value><see langword="true" /> when neither side is unbounded; otherwise <see langword="false" />.</value>
    public bool IsBounded =>
        (_flags & (LowerUnboundedFlag | UpperUnboundedFlag)) == 0;

    /// <summary>
    /// Gets a value indicating whether the interval contains no integer.
    /// </summary>
    /// <value><see langword="true" /> when empty; otherwise <see langword="false" />.</value>
    public bool IsEmpty =>
        (_flags & NonEmptyMask) == 0;

    /// <summary>
    /// Gets the number of integers in the interval.
    /// </summary>
    /// <value>The inclusive count <c>Last - First + 1</c>, or zero when empty.</value>
    /// <exception cref="InvalidOperationException">The interval is unbounded, so its count is infinite.</exception>
    /// <exception cref="OverflowException">
    /// The count does not fit in <typeparamref name="T" /> — a full-domain interval has one more member than the type
    /// can represent.
    /// </exception>
    public T Count
    {
        get
        {
            if (IsEmpty)
                return T.Zero;
            if (!IsBounded)
                throw new InvalidOperationException(NumericsResourceStrings.Op_Invalid_DiscreteIntervalUnboundedCount);

            // The true count Last - First + 1 lies in [1, 2^N] for an N-bit fixed-width type; every unrepresentable
            // value wraps to a result <= 0, so a non-positive count is a reliable overflow signal. Arbitrary-precision
            // types never wrap and always take the return path.
            T count = _last - _first + T.One;
            if (count <= T.Zero)
                throw new OverflowException(NumericsResourceStrings.Overflow_DiscreteIntervalCount);

            return count;
        }
    }
}
