// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalJsonConverterUnboundedTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that <see cref="IntervalJsonConverter{T}" /> round-trips unbounded and half-bounded intervals under every
/// <see cref="NumericsJsonPolicy" /> - the object form uses the <c>lowerUnbounded</c> / <c>upperUnbounded</c> markers and
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
    /// Verifies that the Strict object form writes an infinite bounded endpoint as a JSON string, as
    /// <c>System.Text.Json</c> writes a named floating-point literal, so the payload is valid JSON.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenEndpointIsInfinite_ShouldWriteItAsString()
    {
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        string upper = JsonSerializer.Serialize(Interval<double>.Closed(0.0, double.PositiveInfinity), options);
        string lower = JsonSerializer.Serialize(Interval<double>.OpenClosed(double.NegativeInfinity, 1.5), options);

        Assert.AreEqual("{\"lower\":0,\"upper\":\"Infinity\",\"lowerInclusive\":true,\"upperInclusive\":true}", upper);
        Assert.AreEqual("{\"lower\":\"-Infinity\",\"upper\":1.5,\"lowerInclusive\":false,\"upperInclusive\":true}", lower);
    }

    /// <summary>
    /// Verifies that an interval with an infinite bounded endpoint round-trips unchanged under the object policies,
    /// whichever side is infinite and whether it is open or closed.
    /// </summary>
    /// <param name="policy">The object policy under test.</param>
    /// <param name="shape">The name of the interval under test.</param>
    [TestMethod]
    [DataRow(NumericsJsonPolicy.Strict, "Closed to +inf")]
    [DataRow(NumericsJsonPolicy.Strict, "Open to +inf")]
    [DataRow(NumericsJsonPolicy.Strict, "Closed from -inf")]
    [DataRow(NumericsJsonPolicy.Strict, "AtLeast -inf")]
    [DataRow(NumericsJsonPolicy.Lenient, "Closed to +inf")]
    [DataRow(NumericsJsonPolicy.Lenient, "Open to +inf")]
    [DataRow(NumericsJsonPolicy.Lenient, "Closed from -inf")]
    [DataRow(NumericsJsonPolicy.Lenient, "AtLeast -inf")]
    public void ObjectPolicy_WhenEndpointIsInfinite_ShouldRoundTrip(NumericsJsonPolicy policy, string shape)
    {
        Interval<double> original = InfiniteEndpoint(shape);
        JsonSerializerOptions options = Options(policy);

        string json = JsonSerializer.Serialize(original, options);
        Interval<double> recovered = JsonSerializer.Deserialize<Interval<double>>(json, options);

        Assert.AreEqual(original, recovered, json);
    }

    /// <summary>
    /// Verifies that an interval whose closed side carries an infinite endpoint round-trips unchanged under the Compact
    /// string policy.
    /// </summary>
    /// <param name="shape">The name of the interval under test.</param>
    [TestMethod]
    [DataRow("Closed to +inf")]
    [DataRow("Closed from -inf")]
    public void CompactPolicy_WhenClosedEndpointIsInfinite_ShouldRoundTrip(string shape)
    {
        Interval<double> original = InfiniteEndpoint(shape);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(original, options);
        Interval<double> recovered = JsonSerializer.Deserialize<Interval<double>>(json, options);

        Assert.AreEqual(original, recovered, json);
    }

    /// <summary>
    /// Returns the interval with an infinite bounded endpoint named by <paramref name="shape" />.
    /// </summary>
    /// <param name="shape">The interval name.</param>
    /// <returns>The corresponding interval.</returns>
    private static Interval<double> InfiniteEndpoint(string shape) =>
        shape switch
        {
            "Closed to +inf" => Interval<double>.Closed(0.0, double.PositiveInfinity),
            "Open to +inf" => Interval<double>.Open(0.0, double.PositiveInfinity),
            "Closed from -inf" => Interval<double>.Closed(double.NegativeInfinity, 1.5),
            "AtLeast -inf" => Interval<double>.AtLeast(double.NegativeInfinity),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

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
