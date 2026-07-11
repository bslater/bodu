// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that <see cref="Fraction{T}" /> round-trips through the registered <c>Bodu.Numerics</c> JSON converters and
/// through the <see cref="FractionJsonExtensions" /> convenience helpers.
/// </summary>
[TestClass]
public class FractionJsonConverterTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the Numerics converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(NumericsJsonPolicy policy = NumericsJsonPolicy.Strict) =>
        new JsonSerializerOptions().AddNumericsJsonConverters(policy);

    /// <summary>
    /// Verifies that JSON serialization writes a fraction in the canonical object form under the Strict policy.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStrict_ShouldEmitCanonicalObjectShape()
    {
        string json = JsonSerializer.Serialize(new Fraction<int>(3, 4), Options());

        Assert.AreEqual("{\"numerator\":3,\"denominator\":4}", json);
    }

    /// <summary>
    /// Verifies that a fraction round-trips through JSON serialization.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4)]
    [DataRow(-7, 2)]
    [DataRow(5, 1)]
    public void RoundTrip_WhenSerialized_ShouldPreserveValue(int numerator, int denominator)
    {
        var original = new Fraction<int>(numerator, denominator);
        JsonSerializerOptions options = Options();

        string json = JsonSerializer.Serialize(original, options);
        Fraction<int> restored = JsonSerializer.Deserialize<Fraction<int>>(json, options);

        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that a <see cref="BigInteger" />-backed fraction round-trips through JSON serialization, preserving
    /// magnitudes that exceed the JSON-number primitive range.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenBackedByBigInteger_ShouldPreserveValue()
    {
        var original = new Fraction<BigInteger>(BigInteger.Pow(10, 30), 3);
        JsonSerializerOptions options = Options();

        string json = JsonSerializer.Serialize(original, options);
        Fraction<BigInteger> restored = JsonSerializer.Deserialize<Fraction<BigInteger>>(json, options);

        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that <see cref="FractionJsonExtensions.ToJson{T}" /> and
    /// <see cref="FractionJsonExtensions.FromJson{T}" /> round-trip a fraction through its JSON representation.
    /// </summary>
    [TestMethod]
    public void ToJsonAndFromJson_WhenRoundTripped_ShouldPreserveValue()
    {
        var original = new Fraction<int>(-7, 8);

        Assert.AreEqual(original, FractionJsonExtensions.FromJson<int>(original.ToJson()));
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="FractionJsonConverter{T}" /> constructor selects the canonical Strict
    /// object shape.
    /// </summary>
    [TestMethod]
    public void FractionJsonConverter_WhenConstructedWithoutPolicy_ShouldUseStrictObjectShape()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new FractionJsonConverter<int>());

        Assert.AreEqual("{\"numerator\":3,\"denominator\":4}", JsonSerializer.Serialize(new Fraction<int>(3, 4), options));
    }

    /// <summary>
    /// Verifies that deserializing the canonical object form rejects malformed payloads — a duplicate property, a
    /// missing required property, or a non-numeric value — with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("{\"numerator\":1,\"denominator\":2,\"denominator\":3}", DisplayName = "Duplicate denominator")]
    [DataRow("{\"denominator\":2}", DisplayName = "Missing numerator")]
    [DataRow("{\"numerator\":true,\"denominator\":2}", DisplayName = "Numerator is not a number")]
    public void Deserialize_WhenObjectPayloadIsMalformed_ShouldThrowJsonException(string json)
    {
        JsonSerializerOptions options = Options();

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, options);
        });
    }
}
