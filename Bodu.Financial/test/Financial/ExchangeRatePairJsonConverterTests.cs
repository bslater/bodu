// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="ExchangeRatePairJsonConverter" /> across the object and compact <c>"FROM/TO"</c> shapes,
/// including the malformed-payload rejection paths.
/// </summary>
[TestClass]
public class ExchangeRatePairJsonConverterTests
{
    private static JsonSerializerOptions OptionsFor(FinancialJsonPolicy policy) =>
        new JsonSerializerOptions().AddFinancialJsonConverters(policy);

    /// <summary>
    /// Verifies that the Strict and Compact policies both round-trip a pair's source and destination codes.
    /// </summary>
    [TestMethod]
    [DataRow(FinancialJsonPolicy.Strict)]
    [DataRow(FinancialJsonPolicy.Compact)]
    public void RoundTrip_ShouldPreserveFromAndTo(FinancialJsonPolicy policy)
    {
        JsonSerializerOptions options = OptionsFor(policy);

        string json = JsonSerializer.Serialize(new ExchangeRatePair("USD", "JPY"), options);
        ExchangeRatePair restored = JsonSerializer.Deserialize<ExchangeRatePair>(json, options);

        Assert.AreEqual("USD", restored.FromIsoCode);
        Assert.AreEqual("JPY", restored.ToIsoCode);
    }

    /// <summary>
    /// Verifies that the compact <c>"FROM/TO"</c> string form deserializes through the default Strict object reader's
    /// shared parsing path.
    /// </summary>
    [TestMethod]
    public void Compact_WhenStringForm_ShouldResolve()
    {
        ExchangeRatePair restored = JsonSerializer.Deserialize<ExchangeRatePair>("\"USD/JPY\"", OptionsFor(FinancialJsonPolicy.Compact));

        Assert.AreEqual("USD", restored.FromIsoCode);
        Assert.AreEqual("JPY", restored.ToIsoCode);
    }

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
