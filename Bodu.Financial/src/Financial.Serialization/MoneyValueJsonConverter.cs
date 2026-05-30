// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValueJsonConverter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Converts a <see cref="MoneyValue" /> to and from JSON using the policy supplied at construction. Mirrors the
/// shape vocabulary of <see cref="MoneyJsonConverter{TCurrency}" /> so a single
/// <see cref="FinancialJsonPolicy" /> selection produces a coherent on-the-wire format across the monetary types.
/// </summary>
public sealed class MoneyValueJsonConverter
    : JsonConverter<MoneyValue>
{
    /// <summary>
    /// The policy used by this converter instance.
    /// </summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyValueJsonConverter" /> class configured for the
    /// <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public MoneyValueJsonConverter()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyValueJsonConverter" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public MoneyValueJsonConverter(FinancialJsonPolicy policy)
    {
        FinancialThrowHelper.ThrowIfFinancialJsonPolicyUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override MoneyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy == FinancialJsonPolicy.Compact
            ? ReadCompact(ref reader)
            : ReadObject(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, MoneyValue value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == FinancialJsonPolicy.Compact)
        {
            writer.WriteStringValue(FormatCompact(value));
            return;
        }

        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.IsoCode);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (e.g. <c>"19.99 USD"</c>) via
    /// <see cref="MoneyValue.TryParse(ReadOnlySpan{char}, IFormatProvider?, out MoneyValue)" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid MoneyValue representation.
    /// </exception>
    private static MoneyValue ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedCompactString_MoneyValue);

        var text = reader.GetString()!;
        if (!MoneyValue.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out MoneyValue result))
        {
            throw new JsonException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Json_Invalid_CompactMoneyValueForm,
                    text));
        }

        return result;
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same shape).
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON shape is invalid.
    /// </exception>
    private MoneyValue ReadObject(ref Utf8JsonReader reader)
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

        if (_policy == FinancialJsonPolicy.Lenient)
            currency = currency.Trim().ToUpperInvariant();

        // Pre-validate the ISO shape so a malformed code surfaces as JsonException rather than the
        // ArgumentException that the MoneyValue constructor would otherwise raise.
        if (currency.Length != 3
            || !char.IsAsciiLetterUpper(currency[0])
            || !char.IsAsciiLetterUpper(currency[1])
            || !char.IsAsciiLetterUpper(currency[2]))
        {
            throw new JsonException(FinancialResourceStrings.Arg_Invalid_IsoCodeShape);
        }

        return new MoneyValue(amount.Value, currency);
    }

    /// <summary>
    /// Formats a runtime-tagged monetary value for the compact JSON shape: the amount rendered in the invariant
    /// culture, then a single space, then the ISO code. When the currency is registered, the registered minor-unit
    /// precision drives the trailing-zero count; otherwise the amount's natural representation is used.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The compact textual representation.</returns>
    private static string FormatCompact(MoneyValue value)
    {
        var numericFormat = "F" + value.MinorUnits.ToString(CultureInfo.InvariantCulture);
        return string.Concat(
            value.Amount.ToString(numericFormat, CultureInfo.InvariantCulture),
            " ",
            value.IsoCode);
    }
}
