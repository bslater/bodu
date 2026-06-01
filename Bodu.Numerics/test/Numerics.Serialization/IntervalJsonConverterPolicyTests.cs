// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalJsonConverterPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization;

/// <summary>
/// Verifies that <see cref="IntervalJsonConverter{T}" /> honours each <see cref="NumericsJsonPolicy" /> on both the
/// read and write paths.
/// </summary>
[TestClass]
public class IntervalJsonConverterPolicyTests
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
    /// Verifies that the attribute-driven default serializer emits the canonical object form for a non-empty interval.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializing_ShouldEmitCanonicalObjectShape()
    {
        Interval<int> interval = Interval.ClosedOpen(1, 5);

        var json = JsonSerializer.Serialize(interval);

        Assert.AreEqual("{\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":false}", json);
    }

    /// <summary>
    /// Verifies that the attribute-driven default serializer emits the single-property empty marker for
    /// <see cref="Interval{T}.Empty" />.
    /// </summary>
    [TestMethod]
    public void DefaultAttribute_WhenSerializingEmpty_ShouldEmitEmptyMarker()
    {
        var json = JsonSerializer.Serialize(Interval<int>.Empty);

        Assert.AreEqual("{\"empty\":true}", json);
    }

    /// <summary>
    /// Verifies that the shipped <c>[JsonConverter]</c> attribute remains present so default (no-options)
    /// serialization picks the strict shape with no consumer configuration.
    /// </summary>
    [TestMethod]
    public void Interval_WhenInspected_ShouldStillDeclareJsonConverterAttribute()
    {
        Assert.IsTrue(typeof(Interval<int>).IsDefined(typeof(JsonConverterAttribute), inherit: false));
        Assert.IsTrue(typeof(Interval<decimal>).IsDefined(typeof(JsonConverterAttribute), inherit: false));
    }

    /// <summary>
    /// Verifies that the canonical object round-trips a non-empty interval through write and read under the Strict
    /// policy, preserving endpoint inclusion semantics for every bracket combination.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTrippingClosed_ShouldPreserveValue()
    {
        Interval<int> original = Interval.Closed(1, 5);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(original, options);
        Interval<int> recovered = JsonSerializer.Deserialize<Interval<int>>(json, options);

        Assert.AreEqual(original, recovered);
        Assert.IsTrue(recovered.LowerInclusive);
        Assert.IsTrue(recovered.UpperInclusive);
    }

    /// <summary>
    /// Verifies that the canonical object round-trips an open-open interval.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTrippingOpen_ShouldPreserveValue()
    {
        Interval<int> original = Interval.Open(1, 5);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(original, options);
        Interval<int> recovered = JsonSerializer.Deserialize<Interval<int>>(json, options);

        Assert.AreEqual(original, recovered);
        Assert.IsFalse(recovered.LowerInclusive);
        Assert.IsFalse(recovered.UpperInclusive);
    }

    /// <summary>
    /// Verifies that the empty marker round-trips back to <see cref="Interval{T}.Empty" /> under Strict.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTrippingEmpty_ShouldPreserveValue()
    {
        Interval<int> original = Interval<int>.Empty;
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(original, options);
        Interval<int> recovered = JsonSerializer.Deserialize<Interval<int>>(json, options);

        Assert.AreEqual(original, recovered);
        Assert.IsTrue(recovered.IsEmpty);
    }

    /// <summary>
    /// Verifies that Strict rejects a JSON token at the top level — only the canonical object is accepted.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingCompactString_ShouldThrowJsonException()
    {
        var json = "\"[1, 5]\"";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that Strict rejects an object missing the <c>"lowerInclusive"</c> property.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingMissingLowerInclusive_ShouldThrowJsonException()
    {
        var json = "{\"lower\":1,\"upper\":5,\"upperInclusive\":true}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that Strict rejects an object missing the <c>"lower"</c> property.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingMissingLower_ShouldThrowJsonException()
    {
        var json = "{\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that Strict rejects a duplicate <c>"lower"</c> property.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingDuplicateLower_ShouldThrowJsonException()
    {
        var json = "{\"lower\":1,\"lower\":2,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that Strict rejects an <c>"empty": true</c> marker that carries sibling endpoint properties.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenReadingEmptyMarkerWithEndpoints_ShouldThrowJsonException()
    {
        var json = "{\"empty\":true,\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that Strict rejects a non-boolean <c>"lowerInclusive"</c> value.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenLowerInclusiveIsNotBoolean_ShouldThrowJsonException()
    {
        var json = "{\"lower\":1,\"upper\":5,\"lowerInclusive\":\"yes\",\"upperInclusive\":true}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));
        });
    }

    /// <summary>
    /// Verifies that Strict skips unknown properties on the object rather than rejecting them.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenObjectCarriesUnknownProperty_ShouldIgnoreIt()
    {
        var json = "{\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true,\"unused\":\"x\"}";

        Interval<int> result = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual(Interval.Closed(1, 5), result);
    }

    /// <summary>
    /// Verifies that Strict accepts a numeric string at the endpoint properties so backings that exceed the
    /// JSON-number primitive range survive without precision loss.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenEndpointValuesAreStrings_ShouldStillRead()
    {
        var json = "{\"lower\":\"1\",\"upper\":\"5\",\"lowerInclusive\":true,\"upperInclusive\":true}";

        Interval<int> result = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual(Interval.Closed(1, 5), result);
    }

    /// <summary>
    /// Verifies that <see cref="NumericsJsonPolicy.Compact" /> writes the ISO 31-11 bracket form.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingClosed_ShouldEmitClosedBracketForm()
    {
        Interval<int> interval = Interval.Closed(1, 5);

        var json = JsonSerializer.Serialize(interval, Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("\"[1, 5]\"", json);
    }

    /// <summary>
    /// Verifies that Compact writes the open-bracket form for a half-open interval.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingClosedOpen_ShouldEmitMixedBracketForm()
    {
        Interval<int> interval = Interval.ClosedOpen(1, 5);

        var json = JsonSerializer.Serialize(interval, Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("\"[1, 5)\"", json);
    }

    /// <summary>
    /// Verifies that Compact writes the empty-set glyph for <see cref="Interval{T}.Empty" />.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingEmpty_ShouldEmitEmptyGlyph()
    {
        var json = JsonSerializer.Serialize(Interval<int>.Empty, Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("\"\\u2205\"", json);
    }

    /// <summary>
    /// Verifies that Compact round-trips a non-empty interval through write and read.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTrippingInterval_ShouldPreserveValue()
    {
        Interval<int> original = Interval.OpenClosed(2, 8);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(original, options);
        Interval<int> recovered = JsonSerializer.Deserialize<Interval<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that Compact round-trips <see cref="Interval{T}.Empty" />.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenRoundTrippingEmpty_ShouldPreserveValue()
    {
        Interval<int> original = Interval<int>.Empty;
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Compact);

        var json = JsonSerializer.Serialize(original, options);
        Interval<int> recovered = JsonSerializer.Deserialize<Interval<int>>(json, options);

        Assert.AreEqual(original, recovered);
        Assert.IsTrue(recovered.IsEmpty);
    }

    /// <summary>
    /// Verifies that Compact reads reject a JSON object — only the bracket string form is accepted.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingObjectForm_ShouldThrowJsonException()
    {
        var json = "{\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that Compact reads reject a string that is not a valid <see cref="Interval{T}" /> representation.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenReadingMalformedString_ShouldThrowJsonException()
    {
        var json = "\"not an interval\"";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Compact));
        });
    }

    /// <summary>
    /// Verifies that <see cref="NumericsJsonPolicy.Lenient" /> accepts the <c>"min"</c>/<c>"max"</c> aliases.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenUsingMinMaxAliases_ShouldSucceed()
    {
        var json = "{\"min\":1,\"max\":5,\"lowerInclusive\":true,\"upperInclusive\":true}";

        Interval<int> result = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(Interval.Closed(1, 5), result);
    }

    /// <summary>
    /// Verifies that Lenient defaults missing endpoint-inclusion flags to closed.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenInclusiveFlagsMissing_ShouldDefaultToClosed()
    {
        var json = "{\"lower\":1,\"upper\":5}";

        Interval<int> result = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(Interval.Closed(1, 5), result);
    }

    /// <summary>
    /// Verifies that Lenient accepts the compact bracket form as a tolerant top-level fallback.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingBracketString_ShouldSucceed()
    {
        var json = "\"[1, 5)\"";

        Interval<int> result = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(Interval.ClosedOpen(1, 5), result);
    }

    /// <summary>
    /// Verifies that Lenient still accepts the canonical object form.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingCanonicalObject_ShouldSucceed()
    {
        var json = "{\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true}";

        Interval<int> result = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Lenient));

        Assert.AreEqual(Interval.Closed(1, 5), result);
    }

    /// <summary>
    /// Verifies that Lenient still rejects an empty marker that carries sibling endpoint properties.
    /// </summary>
    [TestMethod]
    public void LenientPolicy_WhenReadingEmptyMarkerWithEndpoints_ShouldThrowJsonException()
    {
        var json = "{\"empty\":true,\"lower\":1,\"upper\":5}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json, Options(NumericsJsonPolicy.Lenient));
        });
    }

    /// <summary>
    /// Verifies that a decimal-backed interval round-trips through the Strict policy.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenBackedByDecimal_ShouldPreserveValue()
    {
        Interval<decimal> original = Interval.Closed(1.5m, 9.75m);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        var json = JsonSerializer.Serialize(original, options);
        Interval<decimal> recovered = JsonSerializer.Deserialize<Interval<decimal>>(json, options);

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
            _ = new IntervalJsonConverter<int>((NumericsJsonPolicy)999);
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
            _ = new IntervalJsonConverterFactory((NumericsJsonPolicy)999);
        });
    }
}
