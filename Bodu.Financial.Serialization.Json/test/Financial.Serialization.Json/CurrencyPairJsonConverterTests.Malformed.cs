// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyPairJsonConverterTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Serialization.Json;

public partial class CurrencyPairJsonConverterTests
{

    /// <summary>
    /// Verifies that malformed payloads — a non-object root under Strict, a missing property, or a slashless compact
    /// string — are rejected with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void WhenPayloadMalformed_ShouldThrowJsonException()
    {
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<CurrencyPair>("[1,2]", OptionsFor(FinancialJsonPolicy.Strict)));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<CurrencyPair>("{\"from\":\"USD\"}", OptionsFor(FinancialJsonPolicy.Strict)));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<CurrencyPair>("\"USDJPY\"", OptionsFor(FinancialJsonPolicy.Compact)));
    }
}
