// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionJsonConverter{T}.cs" company="Bodu Pty. Ltd.">
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
/// Converts a <see cref="Fraction{T}" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <typeparam name="T">The backing integer type of the fraction.</typeparam>
/// <remarks>
/// <para>
/// The converter shape is selected by <see cref="NumericsJsonPolicy" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Strict" /> — canonical object form <c>{ "numerator": 3, "denominator": 4 }</c>;
/// rejects duplicate properties and non-object tokens. Property values may be either JSON numbers or numeric strings,
/// so <see cref="BigInteger" />-backed values survive without precision loss.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Lenient" /> — same object shape as <see cref="NumericsJsonPolicy.Strict" />, but a
/// top-level JSON string token is accepted and routed through
/// <see cref="Fraction{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" /> as a tolerant fallback.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Compact" /> — string form <c>"3/4"</c>; reads delegate to
/// <see cref="Fraction{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Fraction{T})" /> and writes call
/// <see cref="Fraction{T}.ToString(string?, IFormatProvider?)" /> with the invariant culture.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class FractionJsonConverter<T>
    : JsonConverter<Fraction<T>>
    where T : IBinaryInteger<T>
{
    /// <summary>The policy used by this converter instance.</summary>
    private readonly NumericsJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="FractionJsonConverter{T}" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape.
    /// </summary>
    public FractionJsonConverter()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FractionJsonConverter{T}" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public FractionJsonConverter(NumericsJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override Fraction<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy switch
        {
            NumericsJsonPolicy.Compact => ReadCompact(ref reader),
            NumericsJsonPolicy.Lenient when reader.TokenType == JsonTokenType.String => ReadCompact(ref reader),
            _ => ReadObject(ref reader),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Fraction<T> value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == NumericsJsonPolicy.Compact)
        {
            writer.WriteStringValue(value.ToString(null, CultureInfo.InvariantCulture));
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("numerator");
        WriteIntegerValue(writer, value.Numerator);
        writer.WritePropertyName("denominator");
        WriteIntegerValue(writer, value.Denominator);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (e.g. <c>"3/4"</c>) and constructs the rational value via
    /// <see cref="Fraction{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Fraction{T})" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized rational value.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid <see cref="Fraction{T}" /> representation.
    /// </exception>
    private static Fraction<T> ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    NumericsJsonResourceStrings.Json_Invalid_ExpectedCompactString_Fraction,
                    typeof(T).Name));
        }

        string text = reader.GetString() !;
        return !Fraction<T>.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out Fraction<T> result)
            ? throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    NumericsJsonResourceStrings.Json_Invalid_CompactFractionForm,
                    text,
                    typeof(T).Name))
            : result;
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same object shape).
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized rational value.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is not an object, is missing the <c>"numerator"</c> or <c>"denominator"</c> property,
    /// declares a duplicate property, or carries a value that cannot be parsed as <typeparamref name="T" />.
    /// </exception>
    private static Fraction<T> ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedObject_Fraction);

        T? numerator = default;
        T? denominator = default;
        bool numeratorSeen = false;
        bool denominatorSeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedPropertyName);

            string propertyName = reader.GetString() !;
            if (!reader.Read())
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "numerator", StringComparison.OrdinalIgnoreCase))
            {
                if (numeratorSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateNumerator);
                numeratorSeen = true;

                numerator = ReadIntegerValue(ref reader, NumericsJsonResourceStrings.Json_Invalid_NumeratorMustBeNumber);
            }
            else if (string.Equals(propertyName, "denominator", StringComparison.OrdinalIgnoreCase))
            {
                if (denominatorSeen)
                    throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DuplicateDenominator);
                denominatorSeen = true;

                denominator = ReadIntegerValue(ref reader, NumericsJsonResourceStrings.Json_Invalid_DenominatorMustBeNumber);
            }
            else
            {
                reader.Skip();
            }
        }

        if (!numeratorSeen)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_MissingNumerator);

        if (!denominatorSeen)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_MissingDenominator);

        if (T.IsZero(denominator!))
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_DenominatorZero);

        // TryCreate reports components whose canonical form is unrepresentable (for example a denominator of
        // T.MinValue, which has no positive negation) so the failure surfaces as JsonException per the
        // deserialization contract rather than leaking OverflowException from the constructor.
        return Fraction<T>.TryCreate(numerator!, denominator!, out Fraction<T> fraction)
            ? fraction
            : throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_FractionCanonicalOverflow);
    }

    /// <summary>
    /// Reads a <typeparamref name="T" />-typed integer value from either a JSON number or a JSON string token, using
    /// the invariant culture so <see cref="BigInteger" />-magnitude values round-trip without precision loss.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="typeMismatchMessage">
    /// The resource message to report when the token is neither a number nor a parseable string.
    /// </param>
    /// <returns>The parsed <typeparamref name="T" /> value.</returns>
    /// <exception cref="JsonException">The token is neither a number nor a parseable integer string.</exception>
    private static T ReadIntegerValue(ref Utf8JsonReader reader, string typeMismatchMessage)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // The raw value span is ASCII (JSON number tokens are digits, optional sign, optional fractional and
            // exponent parts), so a 1:1 byte-to-char decode is safe and allows BigInteger-sized values to flow
            // through without losing precision via reader.GetInt64() / GetDecimal().
            string text = reader.HasValueSequence
                ? Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
                : Encoding.UTF8.GetString(reader.ValueSpan);

            return T.TryParse(text.AsSpan(), NumberStyles.Integer, CultureInfo.InvariantCulture, out T? parsed)
                ? parsed
                : throw new JsonException(typeMismatchMessage);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? stringValue = reader.GetString();
            return stringValue is not null
                && T.TryParse(stringValue.AsSpan(), NumberStyles.Integer, CultureInfo.InvariantCulture, out T? parsed)
                ? parsed
                : throw new JsonException(typeMismatchMessage);
        }

        throw new JsonException(typeMismatchMessage);
    }

    /// <summary>
    /// Writes a <typeparamref name="T" />-typed integer value as a raw JSON number using the invariant culture, so
    /// <see cref="BigInteger" />-magnitude values are not truncated by the writer's primitive overloads.
    /// </summary>
    /// <param name="writer">The writer that receives the value.</param>
    /// <param name="value">The integer value to write.</param>
    private static void WriteIntegerValue(Utf8JsonWriter writer, T value)
    {
        string text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
        writer.WriteRawValue(text);
    }
}
