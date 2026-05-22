// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Serialization.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that JSON serialization writes a fraction as its string representation.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_WhenSerializing_ShouldWriteStringForm()
    {
        string json = JsonSerializer.Serialize(new Fraction<int>(3, 4));

        Assert.AreEqual("\"3/4\"", json);
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
        Fraction<int> original = new Fraction<int>(numerator, denominator);

        string json = JsonSerializer.Serialize(original);
        Fraction<int> restored = JsonSerializer.Deserialize<Fraction<int>>(json);

        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that a <see cref="BigInteger" />-backed fraction round-trips through JSON serialization.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_WhenBackedByBigInteger_ShouldPreserveValue()
    {
        Fraction<BigInteger> original = new Fraction<BigInteger>(BigInteger.Pow(10, 30), 3);

        string json = JsonSerializer.Serialize(original);
        Fraction<BigInteger> restored = JsonSerializer.Deserialize<Fraction<BigInteger>>(json);

        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that deserializing a malformed JSON fraction string throws <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_WhenStringIsMalformed_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>("\"not a fraction\"");
        });
    }

    /// <summary>
    /// Verifies that deserializing a non-string JSON token throws <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_WhenTokenIsNotString_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>("123");
        });
    }

    /// <summary>
    /// Verifies that ToJson and FromJson round-trip a fraction through its JSON representation.
    /// </summary>
    [TestMethod]
    public void ToJsonAndFromJson_WhenRoundTripped_ShouldPreserveValue()
    {
        Fraction<int> original = new Fraction<int>(-7, 8);

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
        Fraction<int> original = new Fraction<int>(numerator, denominator);

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
}
