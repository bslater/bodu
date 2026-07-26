// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that <see cref="ComplexJsonConverter{T}" /> round-trips <see cref="Complex{T}" /> under each
/// <see cref="NumericsJsonPolicy" /> and rejects malformed payloads.
/// </summary>
[TestClass]
public class ComplexJsonConverterTests
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
    /// Verifies that the Strict policy writes the canonical object form.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        string json = JsonSerializer.Serialize(new Complex<double>(3.0, -4.0), Options());

        Assert.AreEqual("{\"real\":3,\"imaginary\":-4}", json);
    }

    /// <summary>
    /// Verifies that the Compact policy writes the bracketed string form.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializing_ShouldEmitBracketedString()
    {
        string json = JsonSerializer.Serialize(new Complex<double>(3.0, -4.0), Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("\"\\u003C3; -4\\u003E\"", json);
    }

    /// <summary>
    /// Verifies that the Strict policy round-trips a value through its canonical object shape.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        var value = new Complex<double>(1.25, -2.5);

        string json = JsonSerializer.Serialize(value, Options());
        Complex<double> result = JsonSerializer.Deserialize<Complex<double>>(json, Options());

        Assert.AreEqual(value, result);
    }

    /// <summary>
    /// Verifies that the Compact policy round-trips a value through its bracketed string shape.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveValue()
    {
        var value = new Complex<double>(1.25, -2.5);

        string json = JsonSerializer.Serialize(value, Options(NumericsJsonPolicy.Compact));
        Complex<double> result = JsonSerializer.Deserialize<Complex<double>>(json, Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual(value, result);
    }

    /// <summary>
    /// Verifies that a non-finite component serializes as a named string literal and reads back to the same value.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenComponentIsNonFinite_ShouldRoundTripAsNamedLiteral()
    {
        var value = new Complex<double>(double.PositiveInfinity, double.NaN);

        string json = JsonSerializer.Serialize(value, Options());
        Complex<double> result = JsonSerializer.Deserialize<Complex<double>>(json, Options());

        Assert.IsTrue(json.Contains("\"Infinity\"", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("\"NaN\"", StringComparison.Ordinal));
        Assert.IsTrue(double.IsPositiveInfinity(result.Real));
        Assert.IsTrue(double.IsNaN(result.Imaginary));
    }

    /// <summary>
    /// Verifies that the object form accepts numeric-string component values.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenComponentIsNumericString_ShouldParse()
    {
        Complex<double> result = JsonSerializer.Deserialize<Complex<double>>(
            "{\"real\":\"3\",\"imaginary\":\"-4\"}", Options());

        Assert.AreEqual(new Complex<double>(3.0, -4.0), result);
    }

    /// <summary>
    /// Verifies that a duplicate property in the object form is rejected.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenDuplicateProperty_ShouldThrowJsonException()
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Complex<double>>(
                "{\"real\":1,\"real\":2,\"imaginary\":3}", Options());
        });
    }

    /// <summary>
    /// Verifies that a missing required property in the object form is rejected.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenMissingProperty_ShouldThrowJsonException()
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Complex<double>>("{\"real\":1}", Options());
        });
    }

    /// <summary>
    /// Verifies that a <see cref="Complex{T}" /> over <see cref="float" /> round-trips through the object shape.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenComponentTypeIsFloat_ShouldRoundTrip()
    {
        var value = new Complex<float>(1.5f, -2.5f);

        string json = JsonSerializer.Serialize(value, Options());
        Complex<float> result = JsonSerializer.Deserialize<Complex<float>>(json, Options());

        Assert.AreEqual(value, result);
    }
}
