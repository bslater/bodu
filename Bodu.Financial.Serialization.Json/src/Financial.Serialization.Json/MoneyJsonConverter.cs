// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Converts a <see cref="Money" /> to and from JSON using the policy supplied at construction. Mirrors the shape
/// vocabulary of <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> so a single <see cref="FinancialJsonPolicy" />
/// selection produces a coherent on-the-wire format across the monetary types.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FinancialJsonPolicy.Strict" /> and <see cref="FinancialJsonPolicy.Lenient" /> policies share the
/// canonical object shape <c>{"amount":19.99,"currency":"USD"}</c>. A value carrying an explicit minor-unit scale — a
/// unit price whose precision differs from the currency's registered minor units (see
/// <see cref="Money.FromExplicitScale(decimal, Currencies.CurrencyCode, int, MidpointRounding)" />) — additionally
/// emits a <c>scale</c> property, for example <c>{"amount":145.678912,"currency":"USD","scale":6}</c>, and the reader
/// reconstructs the value at that scale so the precision (including trailing zeros) survives the round-trip. A payload
/// without <c>scale</c> deserializes at the registry precision, keeping previously written documents valid.
/// </para>
/// <para>
/// The <see cref="FinancialJsonPolicy.Compact" /> policy writes a single string, <c>"19.99 USD"</c>, padding the amount
/// to the value's minor units. On read, an amount printed with more fractional digits than the currency's registered
/// minor units is reconstructed at that finer scale (via
/// <see cref="Money.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Money)" />), so a unit price round-trips through
/// the compact form without separate scale metadata; a scale coarser than the registered precision is preserved only by
/// the object shapes.
/// </para>
/// </remarks>
public sealed class MoneyJsonConverter
    : JsonConverter<Money>
{
    /// <summary>The maximum minor-unit scale a value may carry, matching <see cref="decimal" />'s scale ceiling.</summary>
    private const int MaxScale = 28;

    /// <summary>The policy used by this converter instance.</summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyJsonConverter" /> class configured for the
    /// <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public MoneyJsonConverter()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyJsonConverter" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public MoneyJsonConverter(FinancialJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _policy == FinancialJsonPolicy.Compact
            ? ReadCompact(ref reader)
            : ReadObject(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_policy == FinancialJsonPolicy.Compact)
        {
            writer.WriteStringValue(FormatCompact(value));
            return;
        }

        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Code == CurrencyCode.None ? string.Empty : value.Code.ToString());

        // A value materialised at a precision other than its currency's registered minor units - a unit price
        // carried at, say, six decimal places - reports that explicit scale from Money.MinorUnits. Emit it so the
        // reader can rebuild the precision, including trailing zeros that decimal.Round drops from the stored amount.
        // Ordinary money, whose reported minor units already match the registry, keeps the historical two-field shape.
        if (value.Code != CurrencyCode.None && value.MinorUnits != Money.Zero(value.Code).MinorUnits)
            writer.WriteNumber("scale", value.MinorUnits);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the compact string form (e.g. <c>"19.99 USD"</c>) via
    /// <see cref="Money.TryParse(ReadOnlySpan{char}, IFormatProvider?, out Money)" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">
    /// The token is not a string, or the string is not a valid Money representation.
    /// </exception>
    private static Money ReadCompact(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedCompactString_Money);

        string text = reader.GetString()!;
        return !Money.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out Money result)
            ? throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Json_Invalid_CompactMoneyForm, text))
            : result;
    }

    /// <summary>
    /// Reads the canonical object form (Strict / Lenient policies share the same shape).
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="JsonException">Thrown when the JSON shape is invalid.</exception>
    private Money ReadObject(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedObject_Money);

        decimal? amount = null;
        string? currency = null;
        int? scale = null;
        bool amountSeen = false;
        bool currencySeen = false;
        bool scaleSeen = false;

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
            else if (string.Equals(propertyName, "scale", StringComparison.OrdinalIgnoreCase))
            {
                if (scaleSeen)
                    throw new JsonException(
                        string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Json_Invalid_DuplicateProperty, "scale"));
                scaleSeen = true;

                if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int parsedScale))
                    throw new JsonException(
                        string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Json_Invalid_PropertyMustBeNumber, "scale"));
                scale = parsedScale;
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

        // Pre-validate the ISO shape so a malformed code surfaces as JsonException rather than the
        // ArgumentException that the Money constructor would otherwise raise.
        if (currency.Length != 3
            || !char.IsAsciiLetterUpper(currency[0])
            || !char.IsAsciiLetterUpper(currency[1])
            || !char.IsAsciiLetterUpper(currency[2]))
        {
            throw new JsonException(FinancialJsonResourceStrings.Arg_Invalid_IsoCodeShape);
        }

        // Resolve the wire ISO string to its stored CurrencyCode; an unknown code is a deserialization error.
        if (!CurrencyInfo.TryGetCurrencyCode(currency, out CurrencyCode code))
            throw new JsonException(
                string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Arg_Invalid_UnknownCurrencyRejected, currency));

        // When the wire carries an explicit scale (a unit price at a precision other than the currency's registered
        // minor units), rebuild the value at that scale so the reported precision round-trips. Otherwise construct
        // through the ordinary path, which resolves the precision from the currency registry.
        if (scale is not int explicitScale)
            return new Money(amount.Value, code);

        if (explicitScale is < 0 or > MaxScale)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ScaleOutOfRange);

        return Money.FromExplicitScale(amount.Value, code, explicitScale);
    }

    /// <summary>
    /// Formats a runtime-tagged monetary value for the compact JSON shape: the amount rendered in the invariant
    /// culture, then a single space, then the ISO code. When the currency is registered, the registered minor-unit
    /// precision drives the trailing-zero count; otherwise the amount's natural representation is used.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The compact textual representation.</returns>
    private static string FormatCompact(Money value)
    {
        string numericFormat = "F" + value.MinorUnits.ToString(CultureInfo.InvariantCulture);
        return string.Concat(
            value.Amount.ToString(numericFormat, CultureInfo.InvariantCulture),
            " ",
            value.Code == CurrencyCode.None ? string.Empty : value.Code.ToString());
    }
}
