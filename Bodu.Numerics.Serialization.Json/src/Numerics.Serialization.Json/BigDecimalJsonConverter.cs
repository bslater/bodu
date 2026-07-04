// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalJsonConverter.cs" company="Bodu Pty. Ltd.">
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
/// Converts a <see cref="BigDecimal" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <remarks>
/// <para>
/// The converter shape is selected by <see cref="NumericsJsonPolicy" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Strict" /> — canonical object form <c>{ "unscaledValue": 12340, "scale": 3 }</c>. The
/// unscaled value is written as a raw JSON number and may be read from a number or a numeric string, so an
/// arbitrary-magnitude value round-trips without precision loss.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Lenient" /> — the same object shape, and additionally a top-level JSON string is
/// accepted and routed through the compact parser.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="NumericsJsonPolicy.Compact" /> — the plain decimal string <c>"12.340"</c>. Because JSON numbers can lose
/// precision in some consumers, the value is written as a string rather than a bare number.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class BigDecimalJsonConverter
    : JsonConverter<BigDecimal>
{
    /// <summary>The policy used by this converter instance.</summary>
    private readonly NumericsJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="BigDecimalJsonConverter" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape.
    /// </summary>
    public BigDecimalJsonConverter()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigDecimalJsonConverter" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public BigDecimalJsonConverter(NumericsJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override BigDecimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy switch
        {
            NumericsJsonPolicy.Compact => ReadCompact(ref reader),
            NumericsJsonPolicy.Lenient when reader.TokenType == JsonTokenType.String => ReadCompact(ref reader),
            _ => ReadObject(ref reader),
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BigDecimal value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == NumericsJsonPolicy.Compact)
        {
            writer.WriteStringValue(value.ToString(null, CultureInfo.InvariantCulture));
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("unscaledValue");
        writer.WriteRawValue(value.UnscaledValue.ToString(CultureInfo.InvariantCulture));
        writer.WriteNumber("scale", value.Scale);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (for example <c>"12.34"</c>) via
    /// <see cref="BigDecimal.TryParse(string, IFormatProvider, out BigDecimal)" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid <see cref="BigDecimal" />.
    /// </exception>
    private static BigDecimal ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedCompactString_BigDecimal);

        string text = reader.GetString() !;
        return BigDecimal.TryParse(text, CultureInfo.InvariantCulture, out BigDecimal result)
            ? result
            : throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, NumericsJsonResourceStrings.Json_Invalid_CompactBigDecimalForm, text));
    }

    /// <summary>
    /// Reads the canonical object form <c>{ "unscaledValue": …, "scale": … }</c>.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">
    /// The JSON is not an object, is missing a required property, declares a duplicate property, or carries a value
    /// that cannot be parsed.
    /// </exception>
    private static BigDecimal ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedObject_BigDecimal);

        BigInteger unscaledValue = default;
        int scale = 0;
        bool unscaledSeen = false;
        bool scaleSeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_ExpectedPropertyName);

            string propertyName = reader.GetString() !;
            if (!reader.Read())
                throw new JsonException(NumericsJsonResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "unscaledValue", StringComparison.OrdinalIgnoreCase))
            {
                if (unscaledSeen)
                    throw Duplicate("unscaledValue");
                unscaledSeen = true;
                unscaledValue = ReadBigInteger(ref reader, "unscaledValue");
            }
            else if (string.Equals(propertyName, "scale", StringComparison.OrdinalIgnoreCase))
            {
                if (scaleSeen)
                    throw Duplicate("scale");
                scaleSeen = true;
                scale = ReadInt32(ref reader, "scale");
            }
            else
            {
                reader.Skip();
            }
        }

        if (!unscaledSeen)
            throw Missing("unscaledValue");

        if (!scaleSeen)
            throw Missing("scale");

        return new BigDecimal(unscaledValue, scale);
    }

    /// <summary>
    /// Reads a <see cref="BigInteger" /> from a JSON number or numeric string token.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="propertyName">The property name, used in the error message.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="JsonException">The token is neither a number nor a parseable numeric string.</exception>
    private static BigInteger ReadBigInteger(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            string text = reader.HasValueSequence
                ? Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
                : Encoding.UTF8.GetString(reader.ValueSpan);

            return BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out BigInteger parsed)
                ? parsed
                : throw MustBeNumber(propertyName);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? text = reader.GetString();
            return text is not null && BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out BigInteger parsed)
                ? parsed
                : throw MustBeNumber(propertyName);
        }

        throw MustBeNumber(propertyName);
    }

    /// <summary>
    /// Reads a 32-bit integer from a JSON number token.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="propertyName">The property name, used in the error message.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="JsonException">The token is not a 32-bit integer number.</exception>
    private static int ReadInt32(ref Utf8JsonReader reader, string propertyName) =>
        reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value)
            ? value
            : throw MustBeNumber(propertyName);

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
