// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Converts an <see cref="ExchangeRate" /> observation to and from JSON using the policy supplied at construction.
/// </summary>
/// <remarks>
/// <para>
/// Shape vocabulary:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="FinancialJsonPolicy.Strict" /> emits the full canonical object
/// <c>{ "from", "to", "date", "rate", "provider", "isInverted" }</c> in declaration order and rejects malformed inputs
/// (non-string ISO codes, non-numeric rate, missing fields, duplicate fields, currency-mismatch shape).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="FinancialJsonPolicy.Lenient" /> shares the Strict shape but normalises lowercase ISO codes to uppercase
/// and trims surrounding whitespace before validation. Intended for ingesting external feeds.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="FinancialJsonPolicy.Compact" /> emits a smaller object that combines the currencies into a single
/// <c>"pair": "FROM/TO"</c> property, drops <c>isInverted</c> when it is <see langword="false" />, and uses ISO date
/// string for <c>date</c>. Reads accept both the canonical and compact shapes (presence of <c>"pair"</c> or
/// <c>"from"</c>/<c>"to"</c> selects the shape).
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class ExchangeRateJsonConverter
    : JsonConverter<ExchangeRate>
{
    /// <summary>
    /// The policy used by this converter instance.
    /// </summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateJsonConverter" /> class configured for the
    /// <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public ExchangeRateJsonConverter()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateJsonConverter" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public ExchangeRateJsonConverter(FinancialJsonPolicy policy)
    {
        FinancialThrowHelper.ThrowIfFinancialJsonPolicyUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override ExchangeRate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedObject_ExchangeRate);

        string? from = null;
        string? to = null;
        DateOnly? date = null;
        decimal? rate = null;
        string? provider = null;
        var isInverted = false;
        var seenFrom = false;
        var seenTo = false;
        var seenPair = false;
        var seenDate = false;
        var seenRate = false;
        var seenProvider = false;
        var seenIsInverted = false;

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
                if (seenFrom || seenPair) throw DuplicateProperty("from");
                seenFrom = true;
                from = ReadStringProperty(ref reader, "from");
            }
            else if (string.Equals(propertyName, "to", StringComparison.OrdinalIgnoreCase))
            {
                if (seenTo || seenPair) throw DuplicateProperty("to");
                seenTo = true;
                to = ReadStringProperty(ref reader, "to");
            }
            else if (string.Equals(propertyName, "pair", StringComparison.OrdinalIgnoreCase))
            {
                if (seenPair || seenFrom || seenTo) throw DuplicateProperty("pair");
                seenPair = true;
                (from, to) = ReadCompactPair(ref reader);
            }
            else if (string.Equals(propertyName, "date", StringComparison.OrdinalIgnoreCase))
            {
                if (seenDate) throw DuplicateProperty("date");
                seenDate = true;
                date = ReadDateProperty(ref reader, "date");
            }
            else if (string.Equals(propertyName, "rate", StringComparison.OrdinalIgnoreCase))
            {
                if (seenRate) throw DuplicateProperty("rate");
                seenRate = true;
                rate = ReadDecimalProperty(ref reader, "rate");
            }
            else if (string.Equals(propertyName, "provider", StringComparison.OrdinalIgnoreCase))
            {
                if (seenProvider) throw DuplicateProperty("provider");
                seenProvider = true;
                provider = ReadStringProperty(ref reader, "provider");
            }
            else if (string.Equals(propertyName, "isInverted", StringComparison.OrdinalIgnoreCase))
            {
                if (seenIsInverted) throw DuplicateProperty("isInverted");
                seenIsInverted = true;
                isInverted = ReadBoolProperty(ref reader, "isInverted");
            }
            else
            {
                reader.Skip();
            }
        }

        if (from is null) throw MissingRequiredProperty("from");
        if (to is null) throw MissingRequiredProperty("to");
        if (date is null) throw MissingRequiredProperty("date");
        if (rate is null) throw MissingRequiredProperty("rate");
        if (provider is null) throw MissingRequiredProperty("provider");

        if (_policy == FinancialJsonPolicy.Lenient)
        {
            from = from.Trim().ToUpperInvariant();
            to = to.Trim().ToUpperInvariant();
            provider = provider.Trim();
        }

        try
        {
            return new ExchangeRate(from, to, date.Value, rate.Value, provider, isInverted);
        }
        catch (ArgumentException ex)
        {
            throw new JsonException(ex.Message, ex);
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ExchangeRate value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.WriteStartObject();

        if (_policy == FinancialJsonPolicy.Compact)
        {
            writer.WriteString("pair", string.Concat(value.FromIsoCode, "/", value.ToIsoCode));
            writer.WriteString("date", value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            writer.WriteNumber("rate", value.Rate);
            writer.WriteString("provider", value.Provider);
            if (value.IsInverted)
                writer.WriteBoolean("isInverted", true);
        }
        else
        {
            writer.WriteString("from", value.FromIsoCode);
            writer.WriteString("to", value.ToIsoCode);
            writer.WriteString("date", value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            writer.WriteNumber("rate", value.Rate);
            writer.WriteString("provider", value.Provider);
            writer.WriteBoolean("isInverted", value.IsInverted);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads a slash-separated <c>"FROM/TO"</c> compact pair string from the current token.
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <returns>The <c>From</c> and <c>To</c> ISO codes split out of the input.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the token is not a string or the string lacks the expected slash separator.
    /// </exception>
    private static (string From, string To) ReadCompactPair(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedCompactString_ExchangeRatePair);

        var text = reader.GetString()!;
        var slashIndex = text.IndexOf('/');
        return slashIndex <= 0 || slashIndex >= text.Length - 1 || text.LastIndexOf('/') != slashIndex
            ? throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_CompactExchangeRatePairForm, text))
            : ((string From, string To))(text[..slashIndex], text[(slashIndex + 1)..]);
    }

    /// <summary>
    /// Reads a string-typed property value, throwing when the token is not a string.
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <param name="propertyName">The originating property name (used in the error message).</param>
    /// <returns>The string value.</returns>
    private static string ReadStringProperty(ref Utf8JsonReader reader, string propertyName)
    {
        return reader.TokenType == JsonTokenType.String
            ? reader.GetString()!
            : throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_PropertyMustBeString, propertyName));
    }

    /// <summary>
    /// Reads a numeric property as a <see cref="decimal" />, accepting both number and string tokens (string is parsed
    /// under invariant culture).
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <param name="propertyName">The originating property name (used in the error message).</param>
    /// <returns>The decimal value.</returns>
    private static decimal ReadDecimalProperty(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetDecimal();

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (text is not null && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        throw new JsonException(
            string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_PropertyMustBeNumber, propertyName));
    }

    /// <summary>
    /// Reads a date property as a <see cref="DateOnly" /> in ISO <c>yyyy-MM-dd</c> form.
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <param name="propertyName">The originating property name (used in the error message).</param>
    /// <returns>The parsed date.</returns>
    private static DateOnly ReadDateProperty(ref Utf8JsonReader reader, string propertyName)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_PropertyMustBeDateString, propertyName));

        var text = reader.GetString();
        return text is not null
            && DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_PropertyMustBeDateString, propertyName));
    }

    /// <summary>
    /// Reads a boolean property.
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <param name="propertyName">The originating property name (used in the error message).</param>
    /// <returns>The boolean value.</returns>
    private static bool ReadBoolProperty(ref Utf8JsonReader reader, string propertyName)
    {
        return reader.TokenType is JsonTokenType.True or JsonTokenType.False
            ? reader.GetBoolean()
            : throw new JsonException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Json_Invalid_PropertyMustBeBoolean, propertyName));
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
}
