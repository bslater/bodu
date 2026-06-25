// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerCoercionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializer" /> applies strict numeric and scalar coercions, rejecting lossy
/// conversions and round-tripping <see cref="decimal" /> and large <see cref="ulong" /> values without corruption.
/// </summary>
[TestClass]
public sealed class YamlSerializerCoercionTests
{
    /// <summary>
    /// Verifies that deserializing a non-integral float into an integer target throws
    /// <see cref="YamlSerializationException" /> rather than silently truncating.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFloatIntoInteger_ShouldThrowYamlSerializationException()
    {
        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<int>("3.9\n");
        });
    }

    /// <summary>
    /// Verifies that deserializing an integral-valued float into an integer target succeeds.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegralFloatIntoInteger_ShouldSucceed()
    {
        var value = YamlSerializer.Deserialize<int>("3.0\n");

        Assert.AreEqual(3, value);
    }

    /// <summary>
    /// Verifies that a high-precision <see cref="decimal" /> round-trips without precision loss.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimal_ShouldRoundTripExactly()
    {
        var original = decimal.MaxValue;

        var yaml = YamlSerializer.Serialize(original);
        var value = YamlSerializer.Deserialize<decimal>(yaml);

        Assert.AreEqual(original, value);
    }

    /// <summary>
    /// Verifies that a fractional <see cref="decimal" /> round-trips without binary-floating-point drift.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenFractionalDecimal_ShouldRoundTripExactly()
    {
        const decimal Original = 0.1m + 0.2m;

        var yaml = YamlSerializer.Serialize(Original);
        var value = YamlSerializer.Deserialize<decimal>(yaml);

        Assert.AreEqual(0.3m, value);
    }

    /// <summary>
    /// Verifies that <see cref="ulong.MaxValue" /> round-trips without loss.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenLargeUInt64_ShouldRoundTripExactly()
    {
        var original = ulong.MaxValue;

        var yaml = YamlSerializer.Serialize(original);
        var value = YamlSerializer.Deserialize<ulong>(yaml);

        Assert.AreEqual(original, value);
    }

    /// <summary>
    /// Verifies that deserializing a multi-character scalar into a <see cref="char" /> target throws
    /// <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMultiCharIntoChar_ShouldThrowYamlSerializationException()
    {
        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<char>("ab\n");
        });
    }

    /// <summary>
    /// Verifies that deserializing a single-character scalar into a <see cref="char" /> target succeeds.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSingleCharIntoChar_ShouldSucceed()
    {
        var value = YamlSerializer.Deserialize<char>("a\n");

        Assert.AreEqual('a', value);
    }
}
