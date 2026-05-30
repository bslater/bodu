// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable, bounded interval over any <see cref="INumber{TSelf}" /> type, with independent open or
/// closed endpoints on each side.
/// </summary>
/// <typeparam name="T">
/// The numeric type used for the interval's endpoints. Any type implementing <see cref="INumber{TSelf}" /> works,
/// including <see cref="int" />, <see cref="long" />, <see cref="double" />, <see cref="decimal" />, and
/// <see cref="BigInteger" />.
/// </typeparam>
/// <remarks>
/// <para>
/// An interval is the set of values <c>x</c> such that <c>Lower &#x2264; x &#x2264; Upper</c> (closed-closed),
/// <c>Lower &lt; x &lt; Upper</c> (open-open), <c>Lower &#x2264; x &lt; Upper</c> (closed-open), or
/// <c>Lower &lt; x &#x2264; Upper</c> (open-closed). Endpoint inclusion is independent for the two sides and is
/// preserved through every operation defined on the type.
/// </para>
/// <para>
/// An interval is <b>empty</b> when its bounds do not admit any value — either <c>Lower &gt; Upper</c>, or
/// <c>Lower &#x2261; Upper</c> with at least one endpoint open. All empty intervals are equal to
/// <see cref="Empty" /> by <see cref="IEquatable{T}.Equals(T)" />, and a structural pattern-match against an empty
/// instance always returns the same canonical projection regardless of how the empty value was constructed.
/// </para>
/// <para>
/// Unbounded intervals (intervals with no lower or no upper limit, conventionally written
/// <c>(-&#x221E;, b]</c> or <c>[a, +&#x221E;)</c>) are <b>not</b> directly representable; both
/// <see cref="Lower" /> and <see cref="Upper" /> are always concrete values of <typeparamref name="T" />. Consumers
/// requiring half-infinite or doubly-infinite ranges should use the floating-point infinity constants of
/// <typeparamref name="T" /> when supported (e.g. <see cref="double.NegativeInfinity" />), or model the unbounded
/// case at a higher level.
/// </para>
/// <para>
/// Instances are immutable, structurally equatable, and safe to share. They format using ISO 31-11 interval
/// notation (square brackets for closed endpoints and round brackets for open endpoints) via
/// <see cref="ToString()" />, and parse the same notation through <see cref="Parse(string, IFormatProvider?)" />
/// and the <see cref="TryParse(string?, IFormatProvider?, out Interval{T})" /> family.
/// </para>
/// </remarks>
[Serializable]
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Interval<T>
    where T : INumber<T>
{
    /// <summary>
    /// The lower endpoint backing field. Read through <see cref="Lower" />.
    /// </summary>
    private readonly T _lower;

    /// <summary>
    /// The upper endpoint backing field. Read through <see cref="Upper" />.
    /// </summary>
    private readonly T _upper;

    /// <summary>
    /// Packed endpoint inclusion flags. Bit 0 is <see cref="LowerInclusive" />; bit 1 is
    /// <see cref="UpperInclusive" />. Storing both flags in a single byte keeps the struct size at
    /// <c>2 * sizeof(T) + 1</c> with the JIT supplying the trailing alignment padding.
    /// </summary>
    private readonly byte _flags;

    /// <summary>
    /// Initializes a new instance of the <see cref="Interval{T}" /> struct from a lower and upper endpoint and
    /// explicit inclusion flags for each side.
    /// </summary>
    /// <param name="lower">The lower endpoint of the interval.</param>
    /// <param name="upper">The upper endpoint of the interval.</param>
    /// <param name="lowerInclusive">
    /// <see langword="true" /> when <paramref name="lower" /> is part of the interval (a closed lower endpoint);
    /// <see langword="false" /> when it is excluded (an open lower endpoint).
    /// </param>
    /// <param name="upperInclusive">
    /// <see langword="true" /> when <paramref name="upper" /> is part of the interval (a closed upper endpoint);
    /// <see langword="false" /> when it is excluded (an open upper endpoint).
    /// </param>
    /// <remarks>
    /// <para>
    /// The constructor accepts any combination of endpoints, including <paramref name="lower" /> equal to or
    /// greater than <paramref name="upper" />. When the supplied bounds do not admit any value, the resulting
    /// instance is empty and compares equal to <see cref="Empty" />. The original bounds remain readable via
    /// <see cref="Lower" /> and <see cref="Upper" /> for diagnostic inspection, but participate only as the
    /// representation of <see cref="Empty" /> in set operations.
    /// </para>
    /// </remarks>
    public Interval(T lower, T upper, bool lowerInclusive, bool upperInclusive)
    {
        _lower = lower;
        _upper = upper;
        _flags = (byte)((lowerInclusive ? 1 : 0) | (upperInclusive ? 2 : 0));
    }
}
