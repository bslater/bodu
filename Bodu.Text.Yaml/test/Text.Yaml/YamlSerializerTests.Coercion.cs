// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Coercion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializer" /> applies strict numeric and scalar coercions: integer and integral float
/// scalars bind across the numeric kinds, lossy and out-of-range conversions are rejected, a <see cref="float" /> widens
/// to <see cref="double" /> on write, and <see cref="decimal" /> and large <see cref="ulong" /> values round-trip without
/// corruption.
/// </summary>
public partial class YamlSerializerTests
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
        int value = YamlSerializer.Deserialize<int>("3.0\n");

        Assert.AreEqual(3, value);
    }

    /// <summary>
    /// Verifies that a high-precision <see cref="decimal" /> round-trips without precision loss.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimal_ShouldRoundTripExactly()
    {
        decimal original = decimal.MaxValue;

        string yaml = YamlSerializer.Serialize(original);
        decimal value = YamlSerializer.Deserialize<decimal>(yaml);

        Assert.AreEqual(original, value);
    }

    /// <summary>
    /// Verifies that a fractional <see cref="decimal" /> round-trips without binary-floating-point drift.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenFractionalDecimal_ShouldRoundTripExactly()
    {
        const decimal Original = 0.1m + 0.2m;

        string yaml = YamlSerializer.Serialize(Original);
        decimal value = YamlSerializer.Deserialize<decimal>(yaml);

        Assert.AreEqual(0.3m, value);
    }

    /// <summary>
    /// Verifies that <see cref="ulong.MaxValue" /> round-trips without loss.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenLargeUInt64_ShouldRoundTripExactly()
    {
        ulong original = ulong.MaxValue;

        string yaml = YamlSerializer.Serialize(original);
        ulong value = YamlSerializer.Deserialize<ulong>(yaml);

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
        char value = YamlSerializer.Deserialize<char>("a\n");

        Assert.AreEqual('a', value);
    }

    /// <summary>
    /// Verifies that an integer scalar binds to a <see cref="double" /> member and an integral float scalar binds to an
    /// <see cref="int" /> or a <see cref="uint" /> member, converting across the numeric kinds as the default
    /// <see cref="YamlNumberHandling.Strict" /> handling documents.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerScalarTargetsDoubleOrIntegralFloatTargetsInt_ShouldConvert()
    {
        MemberHolder<double> asDouble = YamlSerializer.Deserialize<MemberHolder<double>>("A: 1\n")!;
        MemberHolder<int> asInt = YamlSerializer.Deserialize<MemberHolder<int>>("A: 1.0\n")!;
        MemberHolder<uint> asUInt = YamlSerializer.Deserialize<MemberHolder<uint>>("A: 1.0\n")!;

        Assert.AreEqual(1.0, asDouble.A);
        Assert.AreEqual(1, asInt.A);
        Assert.AreEqual(1u, asUInt.A);
    }

    /// <summary>
    /// Verifies that a negative integer bound to an unsigned target, <see cref="uint" />, <see cref="ushort" /> or
    /// <see cref="ulong" />, throws <see cref="YamlSerializationException" /> rather than wrapping around.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntegerTargetsUnsignedType_ShouldThrowYamlSerializationException()
    {
        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<uint>("-1");
        });

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<ushort>("-1");
        });

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<ulong>("-1");
        });
    }

    /// <summary>
    /// Verifies that a <see cref="float" /> member holding <c>0.1f</c> is written as the text of the
    /// <see cref="double" /> it widens to, <c>0.10000000149011612</c>, and narrows back to <c>0.1f</c> on read, as the
    /// built-in converter catalog documents.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenSingleMemberIsOneTenth_ShouldRoundTripThroughWidenedText()
    {
        var original = new MemberHolder<float> { A = 0.1f };

        string yaml = YamlSerializer.Serialize(original);
        MemberHolder<float> restored = YamlSerializer.Deserialize<MemberHolder<float>>(yaml)!;

        Assert.AreEqual("A: 0.10000000149011612\n", yaml);
        Assert.AreEqual(0.1f, restored.A);
    }

    /// <summary>
    /// Verifies that a finite scalar outside the range of <see cref="float" /> throws
    /// <see cref="YamlSerializationException" /> when bound to a <see cref="float" /> member, rather than narrowing to
    /// an infinity.
    /// </summary>
    /// <param name="text">The scalar.</param>
    [TestMethod]
    [DataRow("1e300", DisplayName = "a positive overflow")]
    [DataRow("-1e300", DisplayName = "a negative overflow")]
    [DataRow("3.5e38", DisplayName = "just past the boundary")]
    [DataRow("\"1e300\"", DisplayName = "a quoted overflow")]
    public void Deserialize_WhenFiniteFloatExceedsSingleRange_ShouldThrowYamlSerializationException(string text)
    {
        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<MemberHolder<float>>($"A: {text}\n");
        });
    }

    /// <summary>
    /// Verifies that a scalar at the boundary of <see cref="float" />, one that underflows, and the special values bind
    /// to a <see cref="float" /> member, rounding and underflow being accepted.
    /// </summary>
    /// <param name="text">The scalar.</param>
    /// <param name="expected">The value expected.</param>
    [TestMethod]
    [DataRow("3.4028235e38", float.MaxValue, DisplayName = "the largest value")]
    [DataRow("-3.4028235e38", float.MinValue, DisplayName = "the smallest value")]
    [DataRow("1e-50", 0f, DisplayName = "an underflow")]
    [DataRow(".inf", float.PositiveInfinity, DisplayName = "positive infinity")]
    [DataRow("-.inf", float.NegativeInfinity, DisplayName = "negative infinity")]
    [DataRow(".nan", float.NaN, DisplayName = "not a number")]
    public void Deserialize_WhenFloatIsWithinSingleRangeOrSpecial_ShouldBindIt(string text, float expected)
    {
        MemberHolder<float> holder = YamlSerializer.Deserialize<MemberHolder<float>>($"A: {text}\n")!;

        Assert.AreEqual(expected, holder.A);
    }
}
