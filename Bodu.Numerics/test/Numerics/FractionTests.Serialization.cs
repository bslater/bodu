// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;
using Bodu.Numerics.Serialization;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that JSON serialization writes a fraction in the canonical object form under the default Strict
    /// policy.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        var json = JsonSerializer.Serialize(new Fraction<int>(3, 4));

        Assert.AreEqual("{\"numerator\":3,\"denominator\":4}", json);
    }

    /// <summary>
    /// Verifies that a fraction round-trips through JSON serialization.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4)]
    [DataRow(-7, 2)]
    [DataRow(5, 1)]
    public void JsonSerialization_WhenRoundTripped_ShouldPreserveValue(int numerator, int denominator)
    {
        var original = new Fraction<int>(numerator, denominator);

        var json = JsonSerializer.Serialize(original);
        Fraction<int> restored = JsonSerializer.Deserialize<Fraction<int>>(json);

        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that a <see cref="BigInteger" />-backed fraction round-trips through JSON serialization, preserving
    /// magnitudes that exceed the JSON-number primitive range.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_WhenBackedByBigInteger_ShouldPreserveValue()
    {
        var original = new Fraction<BigInteger>(BigInteger.Pow(10, 30), 3);

        var json = JsonSerializer.Serialize(original);
        Fraction<BigInteger> restored = JsonSerializer.Deserialize<Fraction<BigInteger>>(json);

        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that ToJson and FromJson round-trip a fraction through its JSON representation.
    /// </summary>
    [TestMethod]
    public void ToJsonAndFromJson_WhenRoundTripped_ShouldPreserveValue()
    {
        var original = new Fraction<int>(-7, 8);

        Assert.AreEqual(original, Fraction<int>.FromJson(original.ToJson()));
    }

    /// <summary>
    /// Verifies that ToXml and FromXml round-trip a fraction through its XML representation.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4)]
    [DataRow(-7, 8)]
    [DataRow(5, 1)]
    [DataRow(0, 1)]
    [DataRow(-11, 3)]
    public void ToXmlAndFromXml_WhenRoundTripped_ShouldPreserveValue(int numerator, int denominator)
    {
        var original = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(original, Fraction<int>.FromXml(original.ToXml()));
    }

    /// <summary>
    /// Verifies that FromXml rejects a null argument.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenArgumentIsNull_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Fraction<int>.FromXml(null!);
        });
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="FractionJsonConverter{T}" /> constructor selects the canonical
    /// Strict object shape.
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
    public void FractionJsonConverter_WhenObjectPayloadIsMalformed_ShouldThrowJsonException(string json)
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json);
        });
    }
}
