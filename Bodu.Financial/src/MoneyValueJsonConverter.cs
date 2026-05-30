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
/// Serialises <see cref="MoneyValue" /> as a JSON object with <c>"amount"</c> and <c>"currency"</c> fields, matching
/// the shape used by <see cref="Money{TCurrency}" />.
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
            throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedObject_MoneyValue);

        decimal? amount = null;
        string? currency = null;
        var amountSeen = false;
        var currencySeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedPropertyName);

            var propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException(FinancialResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "amount", StringComparison.OrdinalIgnoreCase))
            {
                if (amountSeen)
                    throw new JsonException(FinancialResourceStrings.Json_Invalid_DuplicateAmount);
                amountSeen = true;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var text = reader.GetString();
                    if (text is null || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                        throw new JsonException(FinancialResourceStrings.Json_Invalid_AmountMustBeNumber);
                    amount = parsed;
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    amount = reader.GetDecimal();
                }
                else
                {
                    throw new JsonException(FinancialResourceStrings.Json_Invalid_AmountMustBeNumber);
                }
            }
            else if (string.Equals(propertyName, "currency", StringComparison.OrdinalIgnoreCase))
            {
                if (currencySeen)
                    throw new JsonException(FinancialResourceStrings.Json_Invalid_DuplicateCurrency);
                currencySeen = true;

                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException(FinancialResourceStrings.Json_Invalid_CurrencyMustBeString);
                currency = reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        if (amount is null)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_MissingAmount);

        if (currency is null)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_MissingCurrency);

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
