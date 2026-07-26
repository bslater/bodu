// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexJsonConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Converts a <see cref="Complex{T}" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <typeparam name="T">The floating-point component type of the complex value.</typeparam>
/// <remarks>
/// <para>
/// The converter shape is selected by <see cref="NumericsJsonPolicy" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Strict" /> — canonical object form <c>{ "real": 3, "imaginary": 4 }</c>; rejects
/// duplicate properties and non-object tokens. A finite component is written as a JSON number; a non-finite component
/// is written as one of the JSON strings <c>"NaN"</c>, <c>"Infinity"</c>, or <c>"-Infinity"</c>, since JSON numbers
/// cannot represent those values. Either representation is accepted on read.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Lenient" /> — same object shape as <see cref="NumericsJsonPolicy.Strict" />, but a
/// top-level JSON string token is accepted and routed through
/// <see cref="Complex{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" /> as a tolerant fallback.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Compact" /> — string form <c>"&lt;3; 4&gt;"</c>; reads delegate to
/// <see cref="Complex{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Complex{T})" /> and writes call
/// <see cref="Complex{T}.ToString(string?, IFormatProvider?)" /> with the invariant culture.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class ComplexJsonConverter<T>
    : JsonConverter<Complex<T>>
    where T : IFloatingPointIeee754<T>
{
    /// <summary>The policy used by this converter instance.</summary>
    private readonly NumericsJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplexJsonConverter{T}" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape.
    /// </summary>
    public ComplexJsonConverter()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplexJsonConverter{T}" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public ComplexJsonConverter(NumericsJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override Complex<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy switch
        {
            NumericsJsonPolicy.Compact => ReadCompact(ref reader),
            NumericsJsonPolicy.Lenient when reader.TokenType == JsonTokenType.String => ReadCompact(ref reader),
            _ => ReadObject(ref reader),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Complex<T> value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == NumericsJsonPolicy.Compact)
        {
            writer.WriteStringValue(value.ToString(null, CultureInfo.InvariantCulture));
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("real");
        WriteComponent(writer, value.Real);
        writer.WritePropertyName("imaginary");
        WriteComponent(writer, value.Imaginary);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (e.g. <c>"&lt;3; 4&gt;"</c>) and constructs the complex value via
    /// <see cref="Complex{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Complex{T})" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized complex value.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid <see cref="Complex{T}" /> representation.
    /// </exception>
    private static Complex<T> ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedCompactString_Complex);

        string text = reader.GetString() !;
        return Complex<T>.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out Complex<T> result)
            ? result
            : throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, NumericsJsonResourceStrings.Json_Invalid_CompactComplexForm, text));
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same object shape).
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized complex value.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is not an object, is missing the <c>"real"</c> or <c>"imaginary"</c> property, declares a
    /// duplicate property, or carries a value that cannot be parsed as <typeparamref name="T" />.
    /// </exception>
    private static Complex<T> ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedObject_Complex);

        T real = T.Zero;
        T imaginary = T.Zero;
        bool realSeen = false;
        bool imaginarySeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedPropertyName);

            string propertyName = reader.GetString() !;
            if (!reader.Read())
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "real", StringComparison.OrdinalIgnoreCase))
            {
                if (realSeen)
                    throw Duplicate("real");
                realSeen = true;
                real = ReadComponent(ref reader, "real");
            }
            else if (string.Equals(propertyName, "imaginary", StringComparison.OrdinalIgnoreCase))
            {
                if (imaginarySeen)
                    throw Duplicate("imaginary");
                imaginarySeen = true;
                imaginary = ReadComponent(ref reader, "imaginary");
            }
            else
            {
                reader.Skip();
            }
        }

        if (!realSeen)
            throw Missing("real");

        if (!imaginarySeen)
            throw Missing("imaginary");

        return new Complex<T>(real, imaginary);
    }

    /// <summary>
    /// Reads a <typeparamref name="T" />-typed component from either a JSON number or a JSON string token, using the
    /// invariant culture so that both finite values and the named non-finite literals round-trip.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="propertyName">The property name, used in the error message.</param>
    /// <returns>The parsed <typeparamref name="T" /> component.</returns>
    /// <exception cref="JsonException">The token is neither a number nor a parseable numeric string.</exception>
    private static T ReadComponent(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // The raw number token is ASCII, so a 1:1 byte-to-char decode is safe and lets an arbitrary-magnitude
            // value flow through without the precision loss of reader.GetDouble().
            string text = reader.HasValueSequence
                ? Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
                : Encoding.UTF8.GetString(reader.ValueSpan);

            return T.TryParse(text.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out T? parsed)
                ? parsed
                : throw MustBeNumber(propertyName);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? text = reader.GetString();
            return text is not null
                && T.TryParse(text.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out T? parsed)
                ? parsed
                : throw MustBeNumber(propertyName);
        }

        throw MustBeNumber(propertyName);
    }

    /// <summary>
    /// Writes a <typeparamref name="T" />-typed component, as a raw JSON number when finite and as a named string
    /// literal (<c>"NaN"</c>, <c>"Infinity"</c>, or <c>"-Infinity"</c>) when non-finite, using the invariant culture.
    /// </summary>
    /// <param name="writer">The writer that receives the value.</param>
    /// <param name="value">The component to write.</param>
    private static void WriteComponent(Utf8JsonWriter writer, T value)
    {
        string text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

        if (T.IsFinite(value))
            writer.WriteRawValue(text);
        else
            writer.WriteStringValue(text);
    }

    /// <summary>
    /// Creates the missing-property exception for <paramref name="propertyName" />.
    /// </summary>
    /// <param name="propertyName">The missing property name.</param>
    /// <returns>The exception to throw.</returns>
    private static JsonException Missing(string propertyName) =>
        new(string.Format(CultureInfo.CurrentCulture, NumericsJsonResourceStrings.Json_Invalid_MissingProperty, propertyName));

    /// <summary>
    /// Creates the duplicate-property exception for <paramref name="propertyName" />.
    /// </summary>
    /// <param name="propertyName">The duplicated property name.</param>
    /// <returns>The exception to throw.</returns>
    private static JsonException Duplicate(string propertyName) =>
        new(string.Format(CultureInfo.CurrentCulture, NumericsJsonResourceStrings.Json_Invalid_DuplicateProperty, propertyName));

    /// <summary>
    /// Creates the type-mismatch exception for <paramref name="propertyName" />.
    /// </summary>
    /// <param name="propertyName">The offending property name.</param>
    /// <returns>The exception to throw.</returns>
    private static JsonException MustBeNumber(string propertyName) =>
        new(string.Format(CultureInfo.CurrentCulture, NumericsJsonResourceStrings.Json_Invalid_PropertyMustBeNumber, propertyName));
}
