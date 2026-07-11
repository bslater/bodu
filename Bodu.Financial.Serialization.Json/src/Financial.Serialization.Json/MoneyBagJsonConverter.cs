// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Converts a <see cref="MoneyBag" /> to and from JSON using the policy supplied at construction.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FinancialJsonPolicy.Strict" /> and <see cref="FinancialJsonPolicy.Lenient" /> use the canonical wrapper
/// form <c>{ "balances": { "AUD": 12.34, "USD": 56.78 } }</c>. <see cref="FinancialJsonPolicy.Compact" /> drops the
/// wrapper and emits a flat ISO-code-to-amount map (<c>{ "AUD": 12.34, "USD": 56.78 }</c>); this is the natural minimal
/// shape when the consumer already knows the payload is a bag.
/// </para>
/// </remarks>
public sealed class MoneyBagJsonConverter
    : JsonConverter<MoneyBag>
{
    /// <summary>The policy used by this converter instance.</summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBagJsonConverter" /> class configured for the
    /// <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public MoneyBagJsonConverter()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBagJsonConverter" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The serialization policy.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public MoneyBagJsonConverter(FinancialJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <inheritdoc />
    public override MoneyBag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedObject_MoneyBag);

        return _policy == FinancialJsonPolicy.Compact ? ReadCompactBalances(ref reader) : ReadWrappedBalances(ref reader);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, MoneyBag value, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(value);

        if (_policy == FinancialJsonPolicy.Compact)
        {
            writer.WriteStartObject();
            foreach (Money balance in value)
                writer.WriteNumber(balance.Code.ToString(), balance.Amount);
            writer.WriteEndObject();
            return;
        }

        writer.WriteStartObject();
        writer.WriteStartObject("balances");
        foreach (Money balance in value)
            writer.WriteNumber(balance.Code.ToString(), balance.Amount);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    /// <summary>
    /// Reads the wrapped form: an outer object whose single significant property <c>"balances"</c> contains the
    /// ISO-to-amount map. Used by <see cref="FinancialJsonPolicy.Strict" /> and
    /// <see cref="FinancialJsonPolicy.Lenient" />.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized bag.</returns>
    private static MoneyBag ReadWrappedBalances(ref Utf8JsonReader reader)
    {
        List<Money> entries = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedPropertyName);

            string propertyName = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_UnexpectedEnd);

            if (string.Equals(propertyName, "balances", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_BalancesMustBeObject);

                ReadBalanceMap(ref reader, entries);
            }
            else
            {
                reader.Skip();
            }
        }

        return new MoneyBag(entries);
    }

    /// <summary>
    /// Reads the compact form: a flat ISO-to-amount map directly under the outer object, with no wrapper key.
    /// </summary>
    /// <param name="reader">The reader positioned at the value to convert.</param>
    /// <returns>The deserialized bag.</returns>
    private static MoneyBag ReadCompactBalances(ref Utf8JsonReader reader)
    {
        List<Money> entries = new();
        ReadBalanceMap(ref reader, entries);
        return new MoneyBag(entries);
    }

    /// <summary>
    /// Reads an ISO-to-amount property map, treating every property as a balance entry. The reader must already be
    /// positioned at the <see cref="JsonTokenType.StartObject" /> token of the map.
    /// </summary>
    /// <param name="reader">The reader positioned at the map's <c>StartObject</c>.</param>
    /// <param name="entries">The list that receives the per-currency balances.</param>
    private static void ReadBalanceMap(ref Utf8JsonReader reader, List<Money> entries)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_ExpectedPropertyNameInBalances);

            string iso = reader.GetString()!;
            if (!reader.Read())
                throw new JsonException(FinancialJsonResourceStrings.Json_Invalid_UnexpectedEndInBalances);

            decimal amount;
            if (reader.TokenType == JsonTokenType.Number)
            {
                amount = reader.GetDecimal();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string? text = reader.GetString();
                if (text is null || !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                {
                    throw new JsonException(
                        string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Json_Invalid_BalanceMustBeNumber, iso));
                }
            }
            else
            {
                throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Json_Invalid_BalanceMustBeNumber, iso));
            }

            if (!CurrencyInfo.TryGetCurrencyCode(iso, out CurrencyCode code))
            {
                throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Arg_Invalid_UnknownCurrencyRejected, iso));
            }

            entries.Add(new Money(amount, code));
        }
    }
}
