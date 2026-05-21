// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Serialization.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Xml.Serialization;

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
    public void JsonSerialization_WhenStringIsMalformed_ShouldThrowJsonException()
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>("\"not a fraction\"");
        });
    }

    /// <summary>
    /// Verifies that a fraction round-trips through XML serialization.
    /// </summary>
    [TestMethod]
    public void XmlSerialization_WhenRoundTripped_ShouldPreserveValue()
    {
        Fraction<int> original = new Fraction<int>(-7, 8);
        XmlSerializer serializer = new XmlSerializer(typeof(Fraction<int>));

        using StringWriter writer = new StringWriter();
        serializer.Serialize(writer, original);

        using StringReader reader = new StringReader(writer.ToString());
        Fraction<int> restored = (Fraction<int>)serializer.Deserialize(reader)!;

        Assert.AreEqual(original, restored);
    }

    /// <summary>
    /// Verifies that deserializing a non-string JSON token throws <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void JsonSerialization_WhenTokenIsNotString_ShouldThrowJsonException()
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>("123");
        });
    }

    /// <summary>
    /// Verifies that a table of values round-trips through XML serialization.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(3, 4)]
    [DataRow(-7, 8)]
    [DataRow(5, 1)]
    [DataRow(0, 1)]
    [DataRow(-11, 3)]
    public void XmlSerialization_WhenRoundTrippingKnownValues_ShouldPreserveValue(int numerator, int denominator)
    {
        Fraction<int> original = new Fraction<int>(numerator, denominator);
        XmlSerializer serializer = new XmlSerializer(typeof(Fraction<int>));

        using StringWriter writer = new StringWriter();
        serializer.Serialize(writer, original);

        using StringReader reader = new StringReader(writer.ToString());
        Fraction<int> restored = (Fraction<int>)serializer.Deserialize(reader)!;

        Assert.AreEqual(original, restored);
    }
}
