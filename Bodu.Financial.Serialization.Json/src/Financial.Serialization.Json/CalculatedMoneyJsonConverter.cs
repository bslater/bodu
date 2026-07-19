// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyJsonConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Converts a <see cref="CalculatedMoney" /> to and from JSON using the policy supplied at construction. Because
/// <see cref="CalculatedMoney" /> is the unrounded, deferred-arithmetic tier, its full <see cref="decimal" /> precision
/// - including trailing zeros - is written verbatim and read back unchanged, so a high-precision unit price survives a
/// round-trip without settling to the currency's minor units.
/// </summary>
public sealed class CalculatedMoneyJsonConverter
    : JsonConverter<CalculatedMoney>
{
    /// <summary>The policy used by this converter instance.</summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedMoneyJsonConverter" /> class configured for the
    /// <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public CalculatedMoneyJsonConverter()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedMoneyJsonConverter" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public CalculatedMoneyJsonConverter(FinancialJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override CalculatedMoney Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy == FinancialJsonPolicy.Compact
            ? ReadCompact(ref reader)
            : ReadObject(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, CalculatedMoney value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == FinancialJsonPolicy.Compact)
        {
            writer.WriteStringValue(FormatCompact(value));
            return;
        }

        writer.WriteStartObject();

        // Write the amount verbatim: CalculatedMoney is unrounded, so its decimal already carries every significant
        // digit and any trailing zeros, and System.Text.Json renders a decimal at its stored scale.
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Code == CurrencyCode.None ? string.Empty : value.Code.ToString());
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (e.g. <c>"19.995 USD"</c>): the amount rendered in the invariant culture, a single
    /// space, then the ISO code.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid compact CalculatedMoney representation.
    /// </exception>
    private static CalculatedMoney ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedCompactString_CalculatedMoney);

        string text = reader.GetString()!;

        int split = text.LastIndexOf(' ');
        if (split > 0
            && decimal.TryParse(text.AsSpan(0, split), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
            && CurrencyInfo.TryGetCurrencyCode(text[(split + 1)..], out CurrencyCode code))
        {
            return new CalculatedMoney(amount, code);
        }

        throw new JsonException(
            string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Json_Invalid_CompactCalculatedMoneyForm, text));
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same shape).
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">Thrown when the JSON shape is invalid.</exception>
    private CalculatedMoney ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedObject_CalculatedMoney);

        decimal? amount = null;
        string? currency = null;
        bool amountSeen = false;
        bool currencySeen = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedPropertyName);

            string propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "amount", StringComparison.OrdinalIgnoreCase))
            {
                if (amountSeen)
                    throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_DuplicateAmount);
                amountSeen = true;

                if (reader.TokenType == JsonTokenType.String)
                {
                    string? text = reader.GetString();
                    if (text is null || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
                        throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_AmountMustBeNumber);
                    amount = parsed;
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    amount = reader.GetDecimal();
                }
                else
                {
                    throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_AmountMustBeNumber);
                }
            }
            else if (string.Equals(propertyName, "currency", StringComparison.OrdinalIgnoreCase))
            {
                if (currencySeen)
                    throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_DuplicateCurrency);
                currencySeen = true;

                if (reader.TokenType != JsonTokenType.String)
                    throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_CurrencyMustBeString);
                currency = reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        if (amount is null)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_MissingAmount);

        if (currency is null)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_MissingCurrency);

        if (_policy == FinancialJsonPolicy.Lenient)
            currency = currency.Trim().ToUpperInvariant();

        // Pre-validate the ISO shape so a malformed code surfaces as JsonException rather than the ArgumentException
        // that the CalculatedMoney constructor would otherwise raise.
        if (currency.Length != 3
            || !char.IsAsciiLetterUpper(currency[0])
            || !char.IsAsciiLetterUpper(currency[1])
            || !char.IsAsciiLetterUpper(currency[2]))
        {
            throw new JsonException(FinancialJsonResourceStrings.Arg_Invalid_IsoCodeShape);
        }

        // Resolve the wire ISO string to its stored CurrencyCode; an unknown code is a deserialization error.
        return CurrencyInfo.TryGetCurrencyCode(currency, out CurrencyCode code)
            ? new CalculatedMoney(amount.Value, code)
            : throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Arg_Invalid_UnknownCurrencyRejected, currency));
    }

    /// <summary>
    /// Formats an unrounded monetary value for the compact JSON shape: the amount rendered verbatim in the invariant
    /// culture, then a single space, then the ISO code. Trailing zeros carried by the amount are preserved.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The compact textual representation.</returns>
    private static string FormatCompact(CalculatedMoney value) =>
        string.Concat(
            value.Amount.ToString(CultureInfo.InvariantCulture),
            " ",
            value.Code == CurrencyCode.None ? string.Empty : value.Code.ToString());
}
