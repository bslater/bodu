// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Converts a <see cref="Money{TCurrency}" /> to and from its JSON object representation.
/// </summary>
/// <typeparam name="TCurrency">The currency type identifier.</typeparam>
/// <remarks>
/// The amount is serialized as a JSON object with two properties — <c>"amount"</c> (a JSON number) and
/// <c>"currency"</c> (the ISO 4217 code). Deserialization rejects payloads whose <c>"currency"</c> field does not match
/// <typeparamref name="TCurrency" />.<see cref="ICurrency.IsoCode" />, surfacing currency drift as a
/// <see cref="JsonException" /> rather than silently converting amounts across currencies.
/// </remarks>
public sealed class MoneyJsonConverter<TCurrency> : JsonConverter<Money<TCurrency>>
    where TCurrency : ICurrency
{
    /// <summary>
    /// Reads a <see cref="Money{TCurrency}" /> from its JSON object representation.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The deserialized monetary amount.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is not an object, is missing one of the expected properties, or carries a <c>"currency"</c>
    /// code that does not match <typeparamref name="TCurrency" />.
    /// </exception>
    public override Money<TCurrency> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object containing a Money amount.");

        decimal? amount = null;
        string? currency = null;
        var amountSeen = false;
        var currencySeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a JSON property name.");

            var propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException("Unexpected end of JSON.");

            if (string.Equals(propertyName, "amount", StringComparison.OrdinalIgnoreCase))
            {
                if (amountSeen)
                    throw new JsonException("The JSON object contains a duplicate 'amount' property.");
                amountSeen = true;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var text = reader.GetString();
                    if (text is null || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                        throw new JsonException("The 'amount' property must be a number.");

                    amount = parsed;
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    amount = reader.GetDecimal();
                }
                else
                {
                    throw new JsonException("The 'amount' property must be a number.");
                }
            }
            else if (string.Equals(propertyName, "currency", StringComparison.OrdinalIgnoreCase))
            {
                if (currencySeen)
                    throw new JsonException("The JSON object contains a duplicate 'currency' property.");
                currencySeen = true;

                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException("The 'currency' property must be a string.");

                currency = reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        if (amount is null)
            throw new JsonException("The JSON object is missing the required 'amount' property.");

        if (currency is null)
            throw new JsonException("The JSON object is missing the required 'currency' property.");

        var expectedIso = CurrencyMetadata<TCurrency>.Value.IsoCode;
        if (!string.Equals(currency, expectedIso, StringComparison.Ordinal))
        {
            throw new JsonException(
                $"The JSON 'currency' value '{currency}' does not match the expected currency '{expectedIso}'.");
        }

        return new Money<TCurrency>(amount.Value);
    }

    /// <summary>
    /// Writes a <see cref="Money{TCurrency}" /> as a JSON object containing <c>"amount"</c> and <c>"currency"</c>.
    /// </summary>
    /// <param name="writer">The writer that receives the value.</param>
    /// <param name="value">The monetary value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public override void Write(Utf8JsonWriter writer, Money<TCurrency> value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", CurrencyMetadata<TCurrency>.Value.IsoCode);
        writer.WriteEndObject();
    }
}
