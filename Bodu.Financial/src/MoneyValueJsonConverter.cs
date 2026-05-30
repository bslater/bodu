// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValueJsonConverter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Serialises <see cref="MoneyValue" /> as a JSON object with <c>"amount"</c> and <c>"currency"</c> fields,
/// matching the shape used by <see cref="Money{TCurrency}" />.
/// </summary>
public sealed class MoneyValueJsonConverter : JsonConverter<MoneyValue>
{
    /// <summary>
    /// Reads a <see cref="MoneyValue" /> from its JSON object representation.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the value.</param>
    /// <param name="typeToConvert">The target type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialised value.</returns>
    /// <exception cref="JsonException">The JSON does not match the expected shape.</exception>
    public override MoneyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object containing a MoneyValue.");

        decimal? amount = null;
        string? currency = null;
        bool amountSeen = false;
        bool currencySeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a JSON property name.");

            string propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException("Unexpected end of JSON.");

            if (string.Equals(propertyName, "amount", StringComparison.OrdinalIgnoreCase))
            {
                if (amountSeen)
                    throw new JsonException("The JSON object contains a duplicate 'amount' property.");
                amountSeen = true;

                if (reader.TokenType == JsonTokenType.String)
                {
                    string? text = reader.GetString();
                    if (text is null || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
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

        return new MoneyValue(amount.Value, currency);
    }

    /// <summary>
    /// Writes a <see cref="MoneyValue" /> as a JSON object.
    /// </summary>
    /// <param name="writer">The writer to receive the value.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, MoneyValue value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.IsoCode);
        writer.WriteEndObject();
    }
}
