// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Decimal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the built-in <see cref="decimal" /> support of <see cref="TomlSerializer" />: the
/// <see cref="TomlDecimalHandling.Float" /> and <see cref="TomlDecimalHandling.String" /> write representations, the
/// read path that accepts a float, integer, or string regardless of the configured handling, the lossless string
/// round-trip of full-precision values, and the rejection of unconvertible inputs.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Provides the canonical-text known-answer rows for decimals under both handlings: each row pins the exact value
    /// text a decimal serializes to for a given <see cref="TomlDecimalHandling" />.
    /// </summary>
    /// <returns>The sequence of single-element argument arrays, each carrying one decimal canonical-text row.</returns>
    public static IEnumerable<object[]> DecimalCanonicalRows()
    {
        yield return [new DecimalCanon("float simple", 1.5m, TomlDecimalHandling.Float, "1.5")];
        yield return [new DecimalCanon("float whole gets point", 1m, TomlDecimalHandling.Float, "1.0")];
        yield return [new DecimalCanon("float negative", -19.95m, TomlDecimalHandling.Float, "-19.95")];
        yield return [new DecimalCanon("float zero", 0m, TomlDecimalHandling.Float, "0.0")];
        yield return [new DecimalCanon("string simple", 1.5m, TomlDecimalHandling.String, "\"1.5\"")];
        yield return [new DecimalCanon("string preserves scale", 1.50m, TomlDecimalHandling.String, "\"1.50\"")];
        yield return [new DecimalCanon("string negative", -19.95m, TomlDecimalHandling.String, "\"-19.95\"")];
        yield return [new DecimalCanon("string max", decimal.MaxValue, TomlDecimalHandling.String, "\"79228162514264337593543950335\"")];
        yield return [new DecimalCanon("string min", decimal.MinValue, TomlDecimalHandling.String, "\"-79228162514264337593543950335\"")];
        yield return [new DecimalCanon("string full precision", 0.1234567890123456789012345678m, TomlDecimalHandling.String, "\"0.1234567890123456789012345678\"")];
    }

    /// <summary>
    /// Verifies that a decimal serializes to its expected canonical value text under the handling each row selects.
    /// </summary>
    /// <param name="kat">The decimal canonical-text row under test.</param>
    [TestMethod]
    [DynamicData(nameof(DecimalCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenDecimal_ShouldEmitCanonicalTextForHandling(DecimalCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new TomlSerializerOptions { DecimalHandling = kat.Handling };
        var text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = kat.Input }, options);

        Assert.AreEqual($"Value = {kat.Expected}\n", text);
    }

    /// <summary>
    /// Verifies that a decimal written under <see cref="TomlDecimalHandling.String" /> round-trips exactly, including
    /// the full-precision and boundary values that the float form cannot hold.
    /// </summary>
    /// <param name="kat">The decimal canonical-text row, reused as a round-trip input.</param>
    [TestMethod]
    [DynamicData(nameof(DecimalCanonicalRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenDecimalStringHandling_ShouldRoundTripExactly(DecimalCanon kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new TomlSerializerOptions { DecimalHandling = TomlDecimalHandling.String };
        var text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = kat.Input }, options);
        var actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text, options).Value;

        Assert.AreEqual(kat.Input, actual);
    }

    /// <summary>
    /// Verifies that a decimal within double's exactly-representable range round-trips under the default
    /// <see cref="TomlDecimalHandling.Float" /> handling.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimalFloatHandlingWithinDoublePrecision_ShouldRoundTrip()
    {
        var text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = 19.95m });
        var actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text).Value;

        Assert.AreEqual(19.95m, actual);
    }

    /// <summary>
    /// Verifies that the default <see cref="TomlDecimalHandling.Float" /> handling loses precision beyond what an IEEE
    /// 754 binary64 value can hold, documenting the trade-off the string handling exists to avoid.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimalFloatHandlingExceedsDoublePrecision_ShouldLosePrecision()
    {
        const decimal value = 0.1234567890123456789012345678m;

        var text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = value });
        var actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text).Value;

        Assert.AreNotEqual(value, actual);
        Assert.AreEqual((decimal)(double)value, actual);
    }

    /// <summary>
    /// Verifies that the read path accepts a TOML float, integer, or string token regardless of the configured
    /// handling, so a document produced under one setting reads under either.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the decimal in one of the accepted token forms.</param>
    /// <param name="expected">The expected decimal value as invariant text.</param>
    [TestMethod]
    [DataRow("Value = 1.5\n", "1.5", DisplayName = "from float")]
    [DataRow("Value = 3\n", "3", DisplayName = "from integer")]
    [DataRow("Value = \"19.95\"\n", "19.95", DisplayName = "from string")]
    public void Deserialize_WhenDecimalFromAnyAcceptedToken_ShouldRead(string toml, string expected)
    {
        var actual = TomlSerializer.Deserialize<ValueModel<decimal>>(toml).Value;

        Assert.AreEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), actual);
    }

    /// <summary>
    /// Verifies that reading a decimal from a string that is not an invariant-culture decimal throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDecimalStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<decimal>>("Value = \"not-a-decimal\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a decimal from a non-finite or out-of-range TOML float throws
    /// <see cref="TomlSerializationException" /> carrying the conversion <see cref="OverflowException" />.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the unconvertible float.</param>
    [TestMethod]
    [DataRow("Value = nan\n", DisplayName = "nan")]
    [DataRow("Value = inf\n", DisplayName = "inf")]
    [DataRow("Value = 1e300\n", DisplayName = "out of range")]
    public void Deserialize_WhenDecimalFromUnconvertibleFloat_ShouldThrowTomlSerializationException(string toml)
    {
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<decimal>>(toml);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<OverflowException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that reading a decimal from a token that is neither a float, integer, nor string throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDecimalFromBoolean_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<decimal>>("Value = true\n");
        });
    }

    /// <summary>
    /// Verifies that every power-of-ten scale of a representative mantissa round-trips exactly under
    /// <see cref="TomlDecimalHandling.String" />, sweeping the full scale range of <see cref="decimal" />.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SerializeDeserialize_WhenDecimalScaleSweepUnderStringHandling_ShouldRoundTripExactly()
    {
        var options = new TomlSerializerOptions { DecimalHandling = TomlDecimalHandling.String };

        for (byte scale = 0; scale <= 28; scale++)
        {
            var value = new decimal(987654321, 123456789, 0, isNegative: false, scale);
            var text = TomlSerializer.Serialize(new ValueModel<decimal> { Value = value }, options);
            var actual = TomlSerializer.Deserialize<ValueModel<decimal>>(text, options).Value;

            Assert.AreEqual(value, actual, $"scale {scale}");
        }
    }

    /// <summary>
    /// A known-answer row pinning the canonical value text a decimal serializes to under a specific handling.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Input">The decimal value to serialize.</param>
    /// <param name="Handling">The decimal handling the row selects.</param>
    /// <param name="Expected">The expected canonical value text, excluding the <c>Value = </c> prefix.</param>
    public sealed record DecimalCanon(string Name, decimal Input, TomlDecimalHandling Handling, string Expected) : IKat;
}
