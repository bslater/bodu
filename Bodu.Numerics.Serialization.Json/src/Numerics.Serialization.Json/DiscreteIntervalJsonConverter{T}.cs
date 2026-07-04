// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalJsonConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Converts a <see cref="DiscreteInterval{T}" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <typeparam name="T">The backing integer type of the interval.</typeparam>
/// <remarks>
/// <para>
/// A <see cref="DiscreteInterval{T}" /> is canonically a closed integer interval, so it serializes through the
/// continuous <see cref="Interval{T}" /> converter over its <see cref="DiscreteInterval{T}.ToInterval" /> projection
/// and is read back with <see cref="DiscreteInterval{T}.FromInterval(Interval{T})" />, which snaps any open bounds
/// inward to the contained integers. The wire shape therefore matches <see cref="IntervalJsonConverter{T}" /> under the
/// same <see cref="NumericsJsonPolicy" /> — the <see cref="NumericsJsonPolicy.Compact" /> form of <c>[1, 5]</c>, the
/// object form for <see cref="NumericsJsonPolicy.Strict" /> / <see cref="NumericsJsonPolicy.Lenient" />, the unbounded
/// markers, and the empty-set representation.
/// </para>
/// </remarks>
public sealed class DiscreteIntervalJsonConverter<T>
    : JsonConverter<DiscreteInterval<T>>
    where T : IBinaryInteger<T>
{
    /// <summary>The continuous-interval converter this discrete converter delegates its wire format to.</summary>
    private readonly IntervalJsonConverter<T> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscreteIntervalJsonConverter{T}" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape.
    /// </summary>
    public DiscreteIntervalJsonConverter()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscreteIntervalJsonConverter{T}" /> class configured for the
    /// supplied <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public DiscreteIntervalJsonConverter(NumericsJsonPolicy policy)
    {
        _inner = new IntervalJsonConverter<T>(policy);
    }

    /// <inheritdoc />
    public override DiscreteInterval<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DiscreteInterval<T>.FromInterval(_inner.Read(ref reader, typeof(Interval<T>), options));

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DiscreteInterval<T> value, JsonSerializerOptions options) =>
        _inner.Write(writer, value.ToInterval(), options);
}
