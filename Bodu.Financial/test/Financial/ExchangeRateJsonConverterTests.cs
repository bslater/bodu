// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="ExchangeRateJsonConverter" /> across the Strict, Lenient, and Compact policies, the
/// <c>"pair"</c> shorthand, and the malformed-payload rejection paths.
/// </summary>
[TestClass]
public partial class ExchangeRateJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    private static ExchangeRate Sample() =>
        new(CurrencyCode.USD, CurrencyCode.JPY, new DateOnly(2024, 1, 15), 150.25m, "ecb");
}
