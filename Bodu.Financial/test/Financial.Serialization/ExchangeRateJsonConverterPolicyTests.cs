// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies that <see cref="ExchangeRateJsonConverter" /> honours each <see cref="FinancialJsonPolicy" /> on both
/// the read and write paths.
/// </summary>
[TestClass]
public partial class ExchangeRateJsonConverterPolicyTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the financial converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    /// <summary>
    /// A representative fetch instant used by the provenance round-trip cases.
    /// </summary>
    private static readonly DateTimeOffset s_fetchedAt = new(2024, 5, 30, 14, 15, 16, TimeSpan.Zero);

    /// <summary>
    /// Builds a representative exchange-rate observation.
    /// </summary>
    private static ExchangeRate SampleRate(bool isInverted = false) =>
        new(CurrencyCode.USD, CurrencyCode.JPY, new DateOnly(2024, 5, 30), 156.42m, "ECB", isInverted);

    /// <summary>
    /// Builds a representative exchange-rate observation carrying a non-null fetch instant.
    /// </summary>
    private static ExchangeRate SampleRateWithFetch(bool isInverted = false) =>
        new(CurrencyCode.USD, CurrencyCode.JPY, new DateOnly(2024, 5, 30), 156.42m, "ECB", isInverted, s_fetchedAt);
}
