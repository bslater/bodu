// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterTests.Strict.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class MoneyOfTCurrencyJsonConverterTests
{

    /// <summary>
    /// Verifies that a currency field that disagrees with the type parameter is rejected with a
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Strict_WhenCurrencyMismatchesTypeParameter_ShouldThrowJsonException() =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money<USD>>("{\"amount\":1,\"currency\":\"EUR\"}"));

    /// <summary>
    /// Verifies that malformed payloads — a non-numeric amount or a missing property — are rejected with a
    /// <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("{\"amount\":true,\"currency\":\"USD\"}", DisplayName = "Amount is boolean")]
    [DataRow("{\"currency\":\"USD\"}", DisplayName = "Missing amount")]
    public void Strict_WhenPayloadMalformed_ShouldThrowJsonException(string json) =>
        Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Money<USD>>(json));
}
