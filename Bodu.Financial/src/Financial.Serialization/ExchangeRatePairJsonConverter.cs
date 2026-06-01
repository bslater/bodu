// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Converts an <see cref="ExchangeRatePair" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FinancialJsonPolicy.Strict" /> and <see cref="FinancialJsonPolicy.Lenient" /> use the canonical object
/// form <c>{ "from": "USD", "to": "JPY" }</c>; Lenient additionally normalises lowercase ISO codes to uppercase and
/// trims surrounding whitespace. <see cref="FinancialJsonPolicy.Compact" /> uses the slash-separated string form
/// <c>"USD/JPY"</c>.
/// </para>
/// </remarks>
public sealed class ExchangeRatePairJsonConverter
    : JsonConverter<ExchangeRatePair>
{
    /// <summary>
    /// The policy used by this converter instance.
    /// </summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRatePairJsonConverter" /> class configured for the
    /// <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public ExchangeRatePairJsonConverter()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRatePairJsonConverter" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public ExchangeRatePairJsonConverter(FinancialJsonPolicy policy)
    {
        FinancialThrowHelper.ThrowIfFinancialJsonPolicyUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override ExchangeRatePair Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy == FinancialJsonPolicy.Compact
            ? ReadCompact(ref reader)
            : ReadObject(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ExchangeRatePair value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == FinancialJsonPolicy.Compact)
        {
            writer.WriteStringValue(string.Concat(value.FromIsoCode, "/", value.ToIsoCode));
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("from", value.FromIsoCode);
        writer.WriteString("to", value.ToIsoCode);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form <c>"FROM/TO"</c> (e.g. <c>"USD/JPY"</c>).
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <returns>The deserialised pair.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the token is not a string, or when the string is not a valid pair representation.
    /// </exception>
    private static ExchangeRatePair ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedCompactString_ExchangeRatePair);

        var text = reader.GetString()!;
        var slashIndex = text.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= text.Length - 1 || text.LastIndexOf('/') != slashIndex)
            throw CompactPairFormatException(text);

        var from = text[..slashIndex];
        var to = text[(slashIndex + 1)..];

        try
        {
            return new ExchangeRatePair(from, to);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same shape; Lenient normalises ISO codes).
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <returns>The deserialised pair.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON shape is invalid or a required property is missing.
    /// </exception>
    private ExchangeRatePair ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedObject_ExchangeRatePair);

        string? from = null;
        string? to = null;
        var fromSeen = false;
        var toSeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedPropertyName);

            var propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException(FinancialResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "from", StringComparison.OrdinalIgnoreCase))
            {
                if (fromSeen)
                    throw DuplicateProperty("from");
                fromSeen = true;
                from = ReadIsoString(ref reader, "from");
            }
            else if (string.Equals(propertyName, "to", StringComparison.OrdinalIgnoreCase))
            {
                if (toSeen)
                    throw DuplicateProperty("to");
                toSeen = true;
                to = ReadIsoString(ref reader, "to");
            }
            else
            {
                reader.Skip();
            }
        }

        if (from is null) throw MissingRequiredProperty("from");
        if (to is null) throw MissingRequiredProperty("to");

        if (_policy == FinancialJsonPolicy.Lenient)
        {
            from = from.Trim().ToUpperInvariant();
            to = to.Trim().ToUpperInvariant();
        }

        try
        {
            return new ExchangeRatePair(from, to);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Reads a JSON string token and returns its value, throwing a typed error when the token is not a string.
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <param name="propertyName">The JSON property name reported in the exception message.</param>
    /// <returns>The string value.</returns>
    private static string ReadIsoString(ref Utf8JsonReader reader, string propertyName)
    {
        return reader.TokenType != JsonTokenType.String
            ? throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_PropertyMustBeString, propertyName))
            : reader.GetString()!;
    }

    /// <summary>
    /// Builds a <see cref="JsonException" /> for a duplicate object property.
    /// </summary>
    /// <param name="propertyName">The duplicated property name.</param>
    /// <returns>The exception to throw.</returns>
    private static JsonException DuplicateProperty(string propertyName) =>
        new(string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_DuplicateProperty, propertyName));

    /// <summary>
    /// Builds a <see cref="JsonException" /> for a missing required property.
    /// </summary>
    /// <param name="propertyName">The missing property name.</param>
    /// <returns>The exception to throw.</returns>
    private static JsonException MissingRequiredProperty(string propertyName) =>
        new(string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_MissingRequiredProperty, propertyName));

    /// <summary>
    /// Builds a <see cref="JsonException" /> describing an invalid compact pair string.
    /// </summary>
    /// <param name="text">The offending input.</param>
    /// <returns>The exception to throw.</returns>
    private static JsonException CompactPairFormatException(string text) =>
        new(string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_CompactExchangeRatePairForm, text));
}
