// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Converts a <see cref="Money{TCurrency}" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <typeparam name="TCurrency">The currency type identifier.</typeparam>
/// <remarks>
/// <para>
/// The converter shape is selected by <see cref="FinancialJsonPolicy" />:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="FinancialJsonPolicy.Strict" /> — canonical object form <c>{ "amount": 19.99, "currency": "USD" }</c>;
/// rejects duplicate properties and currency mismatches.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="FinancialJsonPolicy.Lenient" /> — same shape as <see cref="FinancialJsonPolicy.Strict" />, with
/// additional tolerance for lowercase currency codes and whitespace around the currency value.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="FinancialJsonPolicy.Compact" /> — string form <c>"19.99 USD"</c>; reads accept either ISO-prefix or
/// ISO-suffix arrangement and reuse
/// <see cref="Money{TCurrency}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Money{TCurrency})" />.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class MoneyOfTCurrencyJsonConverter<TCurrency>
    : JsonConverter<Money<TCurrency>>
    where TCurrency : ICurrency
{
    /// <summary>The policy used by this converter instance.</summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> class configured for
    /// the <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public MoneyOfTCurrencyJsonConverter()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> class configured for
    /// the supplied <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public MoneyOfTCurrencyJsonConverter(FinancialJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override Money<TCurrency> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy == FinancialJsonPolicy.Compact
            ? ReadCompact(ref reader)
            : ReadObject(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Money<TCurrency> value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == FinancialJsonPolicy.Compact)
        {
            writer.WriteStringValue(FormatCompact(value));
            return;
        }

        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", CurrencyMetadata<TCurrency>.Value.IsoCode);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (e.g. <c>"19.99 USD"</c>) and constructs the monetary amount via
    /// <see cref="Money{TCurrency}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Money{TCurrency})" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized monetary amount.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid Money representation.
    /// </exception>
    private static Money<TCurrency> ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Json_Invalid_ExpectedCompactString_MoneyOfTCurrency,
                    typeof(TCurrency).Name));
        }

        string text = reader.GetString()!;
        return !Money<TCurrency>.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out Money<TCurrency> result)
            ? throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Json_Invalid_CompactMoneyOfTCurrencyForm,
                    text,
                    typeof(TCurrency).Name))
            : result;
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same shape).
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized monetary amount.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is not an object, is missing one of the expected properties, or carries a <c>"currency"</c>
    /// code that does not match <typeparamref name="TCurrency" />.
    /// </exception>
    private Money<TCurrency> ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedObject_MoneyOfTCurrency);

        decimal? amount = null;
        string? currency = null;
        bool amountSeen = false;
        bool currencySeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(FinancialResourceStrings.Json_Invalid_ExpectedPropertyName);

            string propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException(FinancialResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "amount", StringComparison.OrdinalIgnoreCase))
            {
                if (amountSeen)
                    throw new JsonException(FinancialResourceStrings.Json_Invalid_DuplicateAmount);
                amountSeen = true;

                if (reader.TokenType == JsonTokenType.String)
                {
                    string? text = reader.GetString();
                    if (text is null || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
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

        string expectedIso = CurrencyMetadata<TCurrency>.Value.IsoCode;
        return !string.Equals(currency, expectedIso, StringComparison.Ordinal)
            ? throw new JsonException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Json_Invalid_CurrencyMismatch,
                    currency,
                    expectedIso))
            : new Money<TCurrency>(amount.Value);
    }

    /// <summary>
    /// Formats a monetary value for the compact JSON shape: the amount rendered in the invariant culture with no
    /// grouping, then a single space, then the ISO code.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The compact textual representation.</returns>
    private static string FormatCompact(Money<TCurrency> value)
    {
        CurrencyMetadataDescriptor metadata = CurrencyMetadata<TCurrency>.Value;
        string numericFormat = "F" + metadata.MinorUnits.ToString(CultureInfo.InvariantCulture);
        return string.Concat(
            value.Amount.ToString(numericFormat, CultureInfo.InvariantCulture),
            " ",
            metadata.IsoCode);
    }
}
