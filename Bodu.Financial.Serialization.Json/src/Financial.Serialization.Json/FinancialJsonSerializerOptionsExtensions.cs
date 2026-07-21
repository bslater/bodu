// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonSerializerOptionsExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Extension methods that register the <see cref="Bodu.Financial" /> JSON converters on a
/// <see cref="JsonSerializerOptions" />, picking a coherent shape for every shipped monetary type from a single
/// <see cref="FinancialJsonPolicy" /> value.
/// </summary>
public static class FinancialJsonSerializerOptionsExtensions
{
    /// <summary>
    /// Adds the <see cref="Bodu.Financial.Money{TCurrency}" />, <see cref="Bodu.Financial.Money" />,
    /// <see cref="Bodu.Financial.CalculatedMoney" />, <see cref="Bodu.Financial.MoneyBag" />,
    /// <see cref="Bodu.Financial.ExchangeRates.ExchangeRate" />, and
    /// <see cref="Bodu.Financial.ExchangeRates.CurrencyPair" /> JSON converters to <paramref name="options" />,
    /// configured for the supplied <paramref name="policy" />.
    /// </summary>
    /// <param name="options">The serializer options to extend.</param>
    /// <param name="policy">
    /// The serialization policy applied to every registered converter. Defaults to
    /// <see cref="FinancialJsonPolicy.Strict" />.
    /// </param>
    /// <returns>The same <paramref name="options" /> instance, so calls can be chained inline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="options" /> has already been used for serialization or deserialization, in which case
    /// <see cref="JsonSerializerOptions.Converters" /> is read-only. Configure options before first use.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The core <c>Bodu.Financial</c> types carry no <c>[JsonConverter]</c> attribute — the library is
    /// serialization-agnostic — so this call is required for <see cref="Bodu.Financial.Money" />,
    /// <see cref="Bodu.Financial.Money{TCurrency}" />, <see cref="Bodu.Financial.MoneyBag" />,
    /// <see cref="Bodu.Financial.ExchangeRates.ExchangeRate" />, and
    /// <see cref="Bodu.Financial.ExchangeRates.CurrencyPair" /> to round-trip through their canonical shapes.
    /// </para>
    /// <para>
    /// Use the <see cref="FinancialJsonPolicy.Strict" /> policy for ledger / persistence data, the
    /// <see cref="FinancialJsonPolicy.Lenient" /> policy for import workflows, and the
    /// <see cref="FinancialJsonPolicy.Compact" /> policy for the compact <c>"19.99 USD"</c> string form.
    /// </para>
    /// <para>
    /// All three policies preserve monetary precision on round-trip: a <see cref="Bodu.Financial.Money" /> carrying an
    /// explicit minor-unit scale (a unit price) persists that scale — via a <c>scale</c> property in the object shapes
    /// and via the printed fractional digits in the compact form — and a <see cref="Bodu.Financial.CalculatedMoney" />
    /// serializes its full unrounded amount verbatim.
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions AddFinancialJsonConverters(
        this JsonSerializerOptions options,
        FinancialJsonPolicy policy = FinancialJsonPolicy.Strict)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);

        options.Converters.Add(new MoneyOfTCurrencyJsonConverterFactory(policy));
        options.Converters.Add(new MoneyJsonConverter(policy));
        options.Converters.Add(new CalculatedMoneyJsonConverter(policy));
        options.Converters.Add(new MoneyBagJsonConverter(policy));
        options.Converters.Add(new ExchangeRateJsonConverter(policy));
        options.Converters.Add(new CurrencyPairJsonConverter(policy));

        return options;
    }
}
