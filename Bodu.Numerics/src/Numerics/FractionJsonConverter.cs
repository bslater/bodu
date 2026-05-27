// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionJsonConverter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Converts a <see cref="Fraction{T}" /> to and from its JSON string representation.
/// </summary>
/// <typeparam name="T">The backing integer type of the fraction.</typeparam>
/// <remarks>
/// The fraction is serialized as a JSON string in <c>numerator/denominator</c> form using the invariant culture,
/// matching the round-trip contract of <see cref="Fraction{T}.ToString()" /> and
/// <see cref="Fraction{T}.Parse(string)" />.
/// </remarks>
public sealed class FractionJsonConverter<T> : JsonConverter<Fraction<T>>
    where T : IBinaryInteger<T>
{
    /// <summary>
    /// Reads a <see cref="Fraction{T}" /> from its JSON string representation.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The deserialized rational value.</returns>
    /// <exception cref="JsonException">
    /// Thrown if the current token is not a string or does not represent a valid fraction.
    /// </exception>
    public override Fraction<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a JSON string containing a fraction.");

        var text = reader.GetString() ?? throw new JsonException("Expected a non-null fraction string.");

        try
        {
            return Fraction<T>.Parse(text, CultureInfo.InvariantCulture);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"The value '{text}' is not a valid fraction.", ex);
        }
    }

    /// <summary>
    /// Writes a <see cref="Fraction{T}" /> as its JSON string representation.
    /// </summary>
    /// <param name="writer">The writer that receives the value.</param>
    /// <param name="value">The rational value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, Fraction<T> value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.WriteStringValue(value.ToString(null, CultureInfo.InvariantCulture));
    }
}
