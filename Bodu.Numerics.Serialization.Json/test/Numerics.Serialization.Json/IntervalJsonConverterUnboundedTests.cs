// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalJsonConverterUnboundedTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that <see cref="IntervalJsonConverter{T}" /> round-trips unbounded and half-bounded intervals under every
/// <see cref="NumericsJsonPolicy" /> — the object form uses the <c>lowerUnbounded</c> / <c>upperUnbounded</c> markers and
/// the compact form uses the infinity glyphs.
/// </summary>
[TestClass]
public class IntervalJsonConverterUnboundedTests
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
    /// Verifies that each unbounded and half-bounded interval shape round-trips under the Strict object policy.
    /// </summary>
    [TestMethod]
    [DataRow("AtLeast")]
    [DataRow("GreaterThan")]
    [DataRow("AtMost")]
    [DataRow("LessThan")]
    [DataRow("All")]
    public void StrictPolicy_WhenRoundTrippingUnbounded_ShouldPreserveValue(string shape)
    {
        Interval<int> original = Unbounded(shape);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        string json = JsonSerializer.Serialize(original, options);
        Interval<int> recovered = JsonSerializer.Deserialize<Interval<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that each unbounded and half-bounded interval shape round-trips under the Compact string policy.
    /// </summary>
    [TestMethod]
    [DataRow("AtLeast")]
    [DataRow("GreaterThan")]
    [DataRow("AtMost")]
    [DataRow("LessThan")]
    [DataRow("All")]
    public void CompactPolicy_WhenRoundTrippingUnbounded_ShouldPreserveValue(string shape)
    {
        Interval<int> original = Unbounded(shape);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(original, options);
        Interval<int> recovered = JsonSerializer.Deserialize<Interval<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the Strict object form emits the upper-unbounded marker for a lower-bounded interval.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenSerializingAtLeast_ShouldEmitUpperUnboundedMarker()
    {
        string json = JsonSerializer.Serialize(Interval<int>.AtLeast(1), Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual("{\"lower\":1,\"upperUnbounded\":true,\"lowerInclusive\":true}", json);
    }

    /// <summary>
    /// Verifies that the Strict object form emits both unbounded markers for the whole line.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenSerializingAll_ShouldEmitBothUnboundedMarkers()
    {
        string json = JsonSerializer.Serialize(Interval<int>.All, Options(NumericsJsonPolicy.Strict));

        Assert.AreEqual("{\"lowerUnbounded\":true,\"upperUnbounded\":true}", json);
    }

    /// <summary>
    /// Returns the interval named by <paramref name="shape" />.
    /// </summary>
    /// <param name="shape">The factory name.</param>
    /// <returns>The corresponding unbounded interval.</returns>
    private static Interval<int> Unbounded(string shape) =>
        shape switch
        {
            "AtLeast" => Interval<int>.AtLeast(1),
            "GreaterThan" => Interval<int>.GreaterThan(1),
            "AtMost" => Interval<int>.AtMost(5),
            "LessThan" => Interval<int>.LessThan(5),
            "All" => Interval<int>.All,
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
}
