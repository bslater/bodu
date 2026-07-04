// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that <see cref="BigDecimalJsonConverter" /> round-trips <see cref="BigDecimal" /> under each
/// <see cref="NumericsJsonPolicy" /> and rejects malformed payloads.
/// </summary>
[TestClass]
public class BigDecimalJsonConverterTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the Numerics converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(NumericsJsonPolicy policy = NumericsJsonPolicy.Strict) =>
        new JsonSerializerOptions().ConfigureForBoduNumerics(policy);

    /// <summary>
    /// Verifies that the Strict policy writes the canonical object form.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        string json = JsonSerializer.Serialize(new BigDecimal(new BigInteger(1234), 2), Options());

        Assert.AreEqual("{\"unscaledValue\":1234,\"scale\":2}", json);
    }

    /// <summary>
    /// Verifies that the Compact policy writes the plain decimal string.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializing_ShouldEmitDecimalString()
    {
        string json = JsonSerializer.Serialize(new BigDecimal(new BigInteger(1234), 2), Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("\"12.34\"", json);
    }

    /// <summary>
    /// Verifies that values round-trip under both the Strict and Compact policies, including a wide-magnitude value.
    /// </summary>
    [TestMethod]
    [DataRow(NumericsJsonPolicy.Strict)]
    [DataRow(NumericsJsonPolicy.Compact)]
    public void RoundTrip_WhenSerialized_ShouldPreserveValue(NumericsJsonPolicy policy)
    {
        JsonSerializerOptions options = Options(policy);
        BigDecimal[] values =
        [
            BigDecimal.Zero,
            new BigDecimal(new BigInteger(-314), 2),
            BigDecimal.Parse("123456789012345678901234567890.123456789", System.Globalization.CultureInfo.InvariantCulture),
        ];

        foreach (BigDecimal original in values)
        {
            string json = JsonSerializer.Serialize(original, options);
            BigDecimal restored = JsonSerializer.Deserialize<BigDecimal>(json, options);
            Assert.AreEqual(original, restored);
        }
    }

    /// <summary>
    /// Verifies that the parameterless converter defaults to the Strict object shape.
    /// </summary>
    [TestMethod]
    public void BigDecimalJsonConverter_WhenConstructedWithoutPolicy_ShouldUseStrictObjectShape()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new BigDecimalJsonConverter());

        Assert.AreEqual("{\"unscaledValue\":5,\"scale\":1}", JsonSerializer.Serialize(new BigDecimal(new BigInteger(5), 1), options));
    }

    /// <summary>
    /// Verifies that deserializing a malformed object throws <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("{\"scale\":2}", DisplayName = "Missing unscaledValue")]
    [DataRow("{\"unscaledValue\":1}", DisplayName = "Missing scale")]
    [DataRow("{\"unscaledValue\":1,\"unscaledValue\":2,\"scale\":0}", DisplayName = "Duplicate unscaledValue")]
    [DataRow("{\"unscaledValue\":true,\"scale\":0}", DisplayName = "unscaledValue not a number")]
    public void Deserialize_WhenObjectPayloadIsMalformed_ShouldThrowJsonException(string json)
    {
        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<BigDecimal>(json, Options());
        });
    }
}
