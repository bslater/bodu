// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class ExchangeRatePairJsonConverterTests
{

    /// <summary>
    /// Verifies that malformed payloads — a non-object root under Strict, a missing property, or a slashless compact
    /// string — are rejected with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void WhenPayloadMalformed_ShouldThrowJsonException()
    {
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRatePair>("[1,2]"));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRatePair>("{\"from\":\"USD\"}"));
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ExchangeRatePair>("\"USDJPY\"", OptionsFor(FinancialJsonPolicy.Compact)));
    }
}
