// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.Conversions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T>
{
    /// <summary>
    /// Returns the continuous <see cref="Interval{T}" /> covering the same bounds, in canonical closed form.
    /// </summary>
    /// <returns>The equivalent continuous interval.</returns>
    /// <remarks>
    /// The result is a continuous interval over the same integer endpoints; it contains the non-integer coordinates
    /// between them as well, so it is a superset of this discrete interval's integer members.
    /// </remarks>
    public Interval<T> ToInterval()
    {
        if (IsEmpty)
            return Interval<T>.Empty;

        if (LowerUnbounded && UpperUnbounded)
            return Interval<T>.All;

        if (LowerUnbounded)
            return Interval<T>.AtMost(_last);

        return UpperUnbounded
            ? Interval<T>.AtLeast(_first)
            : Interval<T>.Closed(_first, _last);
    }

    /// <summary>
    /// Creates the discrete interval containing exactly the integers of <paramref name="interval" />, snapping open
    /// bounds inward to the nearest contained integer.
    /// </summary>
    /// <param name="interval">The continuous interval to convert.</param>
    /// <returns>The discrete interval of the integers within <paramref name="interval" />.</returns>
    public static DiscreteInterval<T> FromInterval(Interval<T> interval)
    {
        if (interval.IsEmpty)
            return Empty;

        if (interval.LowerUnbounded && interval.UpperUnbounded)
            return All;

        if (interval.LowerUnbounded)
            return interval.UpperInclusive ? AtMost(interval.Upper) : LessThan(interval.Upper);

        if (interval.UpperUnbounded)
            return interval.LowerInclusive ? AtLeast(interval.Lower) : GreaterThan(interval.Lower);

        T first = interval.LowerInclusive ? interval.Lower : interval.Lower + T.One;
        T last = interval.UpperInclusive ? interval.Upper : interval.Upper - T.One;
        return FromInclusive(first, last);
    }
}
