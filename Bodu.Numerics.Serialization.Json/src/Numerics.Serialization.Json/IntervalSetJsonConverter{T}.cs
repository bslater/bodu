// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetJsonConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Converts an <see cref="IntervalSet{T}" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <typeparam name="T">The numeric endpoint type of the set's pieces.</typeparam>
/// <remarks>
/// <para>
/// An <see cref="IntervalSet{T}" /> serializes as a JSON <b>array</b> of its normalized <see cref="Interval{T}" />
/// pieces, each written through <see cref="IntervalJsonConverter{T}" /> under the same <see cref="NumericsJsonPolicy" />.
/// The empty set is the empty array <c>[]</c>; a two-piece set under <see cref="NumericsJsonPolicy.Compact" /> is, for
/// example, <c>["[1, 3]", "(5, 8]"]</c>. On read the pieces are collected and re-normalized through
/// <see cref="IntervalSet{T}.From(IEnumerable{Interval{T}})" />, so overlapping or adjacent input coalesces.
/// </para>
/// </remarks>
public sealed class IntervalSetJsonConverter<T>
    : JsonConverter<IntervalSet<T>>
    where T : INumber<T>
{
    /// <summary>The element converter used for each <see cref="Interval{T}" /> piece.</summary>
    private readonly IntervalJsonConverter<T> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalSetJsonConverter{T}" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape.
    /// </summary>
    public IntervalSetJsonConverter()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalSetJsonConverter{T}" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public IntervalSetJsonConverter(NumericsJsonPolicy policy)
    {
        _inner = new IntervalJsonConverter<T>(policy);
    }

    /// <inheritdoc />
    public override IntervalSet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, NumericsJsonResourceStrings.Json_Invalid_ExpectedArray_IntervalSet, typeof(T).Name));
        }

        var pieces = new List<Interval<T>>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            pieces.Add(_inner.Read(ref reader, typeof(Interval<T>), options));
        }

        return IntervalSet<T>.From(pieces);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IntervalSet<T> value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.WriteStartArray();
        foreach (Interval<T> piece in value)
            _inner.Write(writer, piece, options);
        writer.WriteEndArray();
    }
}
