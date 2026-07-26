// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies the policy-specific read behaviour of <see cref="ComplexJsonConverter{T}" />.
/// </summary>
[TestClass]
public class ComplexJsonConverterPolicyTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the Numerics converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(NumericsJsonPolicy policy) =>
        new JsonSerializerOptions().AddNumericsJsonConverters(policy);

    /// <summary>
    /// Verifies that the Lenient policy accepts the canonical object form.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingObjectForm_ShouldParse()
    {
        Complex<double> result = JsonSerializer.Deserialize<Complex<double>>(
            "{\"real\":3,\"imaginary\":-4}", Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(new Complex<double>(3.0, -4.0), result);
    }

    /// <summary>
    /// Verifies that the Lenient policy also accepts the compact bracketed string form.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingCompactString_ShouldParse()
    {
        Complex<double> result = JsonSerializer.Deserialize<Complex<double>>(
            "\"<3; -4>\"", Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(new Complex<double>(3.0, -4.0), result);
    }

    /// <summary>
    /// Verifies that the object form skips an unknown property rather than failing.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenUnknownProperty_ShouldIgnoreIt()
    {
        Complex<double> result = JsonSerializer.Deserialize<Complex<double>>(
            "{\"real\":3,\"note\":\"ignored\",\"imaginary\":-4}", Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual(new Complex<double>(3.0, -4.0), result);
    }

    /// <summary>
    /// Verifies that the Compact policy rejects a non-string token.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingObjectToken_ShouldThrowJsonException()
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Complex<double>>(
                "{\"real\":3,\"imaginary\":-4}", Options(NumericsJsonPolicy.Compact));
        });
    }
}
