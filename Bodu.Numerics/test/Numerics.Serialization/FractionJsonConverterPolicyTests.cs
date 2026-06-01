// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization;

/// <summary>
/// Verifies that <see cref="FractionJsonConverter{T}" /> honours each <see cref="NumericsJsonPolicy" /> on both the
/// read and write paths.
/// </summary>
[TestClass]
public class FractionJsonConverterPolicyTests
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
    /// Verifies that the attribute-driven default serializer emits the canonical object form.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        var fraction = new Fraction<int>(3, 4);

        var json = JsonSerializer.Serialize(fraction);

        Assert.AreEqual("{\"numerator\":3,\"denominator\":4}", json);
    }

    /// <summary>
    /// Verifies that the shipped <c>[JsonConverter]</c> attribute remains present so default (no-options)
    /// serialization picks the strict shape with no consumer configuration.
    /// </summary>
    [TestMethod]
    public void Fraction_WhenInspected_ShouldStillDeclareJsonConverterAttribute()
    {
        Assert.IsTrue(typeof(Fraction<int>).IsDefined(typeof(JsonConverterAttribute), inherit: false));
        Assert.IsTrue(typeof(Fraction<BigInteger>).IsDefined(typeof(JsonConverterAttribute), inherit: false));
    }

    /// <summary>
    /// Verifies that the canonical object round-trips a fraction through write and read under the Strict policy.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4)]
    [DataRow(-7, 2)]
    [DataRow(5, 1)]
    public void StrictPolicy_WhenRoundTrippingFraction_ShouldPreserveValue(int numerator, int denominator)
    {
        var original = new Fraction<int>(numerator, denominator);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(original, options);
        Fraction<int> recovered = JsonSerializer.Deserialize<Fraction<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that <see cref="NumericsJsonPolicy.Compact" /> writes the <c>"numerator/denominator"</c> string form.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingFraction_ShouldEmitStringForm()
    {
        var fraction = new Fraction<int>(3, 4);

        var json = JsonSerializer.Serialize(fraction, Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("\"3/4\"", json);
    }

    /// <summary>
    /// Verifies that <see cref="NumericsJsonPolicy.Compact" /> round-trips a fraction through write and read.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTrippingFraction_ShouldPreserveValue()
    {
        var original = new Fraction<int>(-7, 8);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(original, options);
        Fraction<int> recovered = JsonSerializer.Deserialize<Fraction<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that compact reads reject a JSON object (the strict / lenient form).
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingObjectForm_ShouldThrowJsonException()
    {
        var json = "{\"numerator\":3,\"denominator\":4}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that compact reads reject a non-string token.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingNumericToken_ShouldThrowJsonException()
    {
        var json = "123";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that compact reads reject a string that is not a valid <see cref="Fraction{T}" /> representation.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingMalformedString_ShouldThrowJsonException()
    {
        var json = "\"not a fraction\"";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that the strict policy rejects a JSON token at the top level — only the canonical object is accepted.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingCompactString_ShouldThrowJsonException()
    {
        var json = "\"3/4\"";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that the strict policy rejects a duplicate <c>"numerator"</c> property.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingDuplicateNumerator_ShouldThrowJsonException()
    {
        var json = "{\"numerator\":3,\"numerator\":5,\"denominator\":4}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that the strict policy rejects a missing <c>"denominator"</c> property.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingMissingDenominator_ShouldThrowJsonException()
    {
        var json = "{\"numerator\":3}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that the strict policy rejects a zero denominator with <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingZeroDenominator_ShouldThrowJsonException()
    {
        var json = "{\"numerator\":3,\"denominator\":0}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that property name matching is case-insensitive under Strict (<c>"Numerator"</c> matches).
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenPropertyNamesUsePascalCase_ShouldStillRead()
    {
        var json = "{\"Numerator\":3,\"Denominator\":4}";

        Fraction<int> result = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual(new Fraction<int>(3, 4), result);
    }

    /// <summary>
    /// Verifies that Strict accepts a JSON numeric string at the numerator and denominator properties (necessary for
    /// magnitudes that exceed the JSON-number primitive range).
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenPropertyValuesAreStrings_ShouldStillRead()
    {
        var json = "{\"numerator\":\"3\",\"denominator\":\"4\"}";

        Fraction<int> result = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual(new Fraction<int>(3, 4), result);
    }

    /// <summary>
    /// Verifies that Strict skips unknown properties on the object rather than rejecting them.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenObjectCarriesUnknownProperty_ShouldIgnoreIt()
    {
        var json = "{\"numerator\":3,\"denominator\":4,\"unused\":\"x\"}";

        Fraction<int> result = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual(new Fraction<int>(3, 4), result);
    }

    /// <summary>
    /// Verifies that <see cref="NumericsJsonPolicy.Lenient" /> accepts the compact string form as a tolerant fallback.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingCompactString_ShouldSucceed()
    {
        var json = "\"3/4\"";

        Fraction<int> result = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(new Fraction<int>(3, 4), result);
    }

    /// <summary>
    /// Verifies that <see cref="NumericsJsonPolicy.Lenient" /> still accepts the canonical object form.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingCanonicalObject_ShouldSucceed()
    {
        var json = "{\"numerator\":3,\"denominator\":4}";

        Fraction<int> result = JsonSerializer.Deserialize<Fraction<int>>(json, Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(new Fraction<int>(3, 4), result);
    }

    /// <summary>
    /// Verifies that a <see cref="BigInteger" />-backed fraction round-trips through the Strict policy, preserving
    /// magnitudes that exceed the JSON-number primitive range.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenBackedByBigInteger_ShouldPreserveValue()
    {
        var original = new Fraction<BigInteger>(BigInteger.Pow(10, 30), 3);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(original, options);
        Fraction<BigInteger> recovered = JsonSerializer.Deserialize<Fraction<BigInteger>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that a <see cref="BigInteger" />-backed fraction round-trips through the Compact policy.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenBackedByBigInteger_ShouldPreserveValue()
    {
        var original = new Fraction<BigInteger>(BigInteger.Pow(10, 30), 3);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(original, options);
        Fraction<BigInteger> recovered = JsonSerializer.Deserialize<Fraction<BigInteger>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the converter ctor validates the policy enum value.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPolicyIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new FractionJsonConverter<int>((NumericsJsonPolicy)999);
        });
    }

    /// <summary>
    /// Verifies that the factory ctor validates the policy enum value.
    /// </summary>
    [TestMethod]
    public void FactoryConstructor_WhenPolicyIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new FractionJsonConverterFactory((NumericsJsonPolicy)999);
        });
    }
}
