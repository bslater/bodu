// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MoneyBagJsonConverter" /> across the wrapped (Strict/Lenient) and flat (Compact) shapes,
/// including the malformed-payload rejection paths.
/// </summary>
[TestClass]
public partial class MoneyBagJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    private static MoneyBag SampleBag() =>
        new([new Money(10m, CurrencyCode.USD), new Money(5m, CurrencyCode.EUR)]);
}
