// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Serialises <see cref="MoneyBag" /> as a JSON object with a <c>"balances"</c> property holding an object map of ISO
/// code to amount.
/// </summary>
public sealed class MoneyBagJsonConverter : JsonConverter<MoneyBag>
{
    /// <summary>
    /// Reads a <see cref="MoneyBag" /> from JSON.
    /// </summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <param name="typeToConvert">The target type.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>The deserialised bag.</returns>
    /// <exception cref="JsonException">The JSON does not match the expected shape.</exception>
    public override MoneyBag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object containing a MoneyBag.");

        List<MoneyValue> entries = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a JSON property name.");

            var propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException("Unexpected end of JSON.");

            if (string.Equals(propertyName, "balances", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("The 'balances' property must be a JSON object.");

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException("Expected a JSON property name inside 'balances'.");

                    var iso = reader.GetString()!;
                    if (!reader.Read())
                        throw new JsonException("Unexpected end of JSON inside 'balances'.");

                    decimal amount;
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        amount = reader.GetDecimal();
                    }
                    else if (reader.TokenType == JsonTokenType.String)
                    {
                        var text = reader.GetString();
                        if (text is null || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                            throw new JsonException($"The balance for '{iso}' must be a number.");
                    }
                    else
                    {
                        throw new JsonException($"The balance for '{iso}' must be a number.");
                    }

                    entries.Add(new MoneyValue(amount, iso));
                }
            }
            else
            {
                reader.Skip();
            }
        }

        return new MoneyBag(entries);
    }

    /// <summary>
    /// Writes a <see cref="MoneyBag" /> as JSON.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The bag to write.</param>
    /// <param name="options">Serializer options.</param>
    public override void Write(Utf8JsonWriter writer, MoneyBag value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteStartObject("balances");
        foreach (MoneyValue balance in value)
            writer.WriteNumber(balance.IsoCode, balance.Amount);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
