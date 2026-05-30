// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonSerializerOptionsExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Extension methods that register the <see cref="Bodu.Financial" /> JSON converters on a
/// <see cref="JsonSerializerOptions" />, picking a coherent shape for every shipped monetary type from a single
/// <see cref="FinancialJsonPolicy" /> value.
/// </summary>
public static class FinancialJsonSerializerOptionsExtensions
{
    /// <summary>
    /// Adds the <see cref="Bodu.Financial.Money{TCurrency}" />, <see cref="Bodu.Financial.MoneyValue" />, and
    /// <see cref="Bodu.Financial.MoneyBag" /> JSON converters to <paramref name="options" />, configured for the
    /// supplied <paramref name="policy" />.
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
    /// Converters registered on <see cref="JsonSerializerOptions.Converters" /> take precedence over the
    /// <c>[JsonConverter]</c> attribute that ships on the monetary types, so calling this method overrides the default
    /// attribute-driven policy for the lifetime of <paramref name="options" />.
    /// </para>
    /// <para>
    /// Use the <see cref="FinancialJsonPolicy.Strict" /> policy for ledger / persistence data, the
    /// <see cref="FinancialJsonPolicy.Lenient" /> policy for import workflows, and the
    /// <see cref="FinancialJsonPolicy.Compact" /> policy for the compact <c>"19.99 USD"</c> string form.
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions AddFinancialJsonConverters(
        this JsonSerializerOptions options,
        FinancialJsonPolicy policy = FinancialJsonPolicy.Strict)
    {
        ThrowHelper.ThrowIfNull(options);
        FinancialThrowHelper.ThrowIfFinancialJsonPolicyUndefined(policy);

        options.Converters.Add(new MoneyJsonConverterFactory(policy));
        options.Converters.Add(new MoneyValueJsonConverter(policy));
        options.Converters.Add(new MoneyBagJsonConverter(policy));
        options.Converters.Add(new ExchangeRateJsonConverter(policy));
        options.Converters.Add(new ExchangeRatePairJsonConverter(policy));

        return options;
    }
}
