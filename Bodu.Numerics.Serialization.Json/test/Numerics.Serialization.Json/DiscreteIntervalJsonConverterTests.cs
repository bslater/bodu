// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that <see cref="DiscreteIntervalJsonConverter{T}" /> round-trips discrete intervals — bounded, empty, and
/// unbounded — through the delegated continuous-interval wire format under each <see cref="NumericsJsonPolicy" />.
/// </summary>
[TestClass]
public class DiscreteIntervalJsonConverterTests
{
    /// <summary>
    /// Builds a <see cref="JsonSerializerOptions" /> seeded with the Numerics converters under
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy under test.</param>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions Options(NumericsJsonPolicy policy) =>
        new JsonSerializerOptions().ConfigureForBoduNumerics(policy);

    /// <summary>
    /// Verifies that the Compact form of a bounded discrete interval is the canonical closed bracket string.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingClosed_ShouldEmitClosedBracketForm()
    {
        string json = JsonSerializer.Serialize(DiscreteInterval<int>.Closed(1, 5), Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("\"[1, 5]\"", json);
    }

    /// <summary>
    /// Verifies that bounded, empty, singleton, and unbounded discrete intervals round-trip under the Strict policy.
    /// </summary>
    [TestMethod]
    [DataRow("Closed")]
    [DataRow("Empty")]
    [DataRow("OpenEmpty")]
    [DataRow("Singleton")]
    [DataRow("AtLeast")]
    [DataRow("AtMost")]
    [DataRow("All")]
    public void StrictPolicy_WhenRoundTripping_ShouldPreserveValue(string shape)
    {
        DiscreteInterval<int> original = Shape(shape);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        string json = JsonSerializer.Serialize(original, options);
        DiscreteInterval<int> recovered = JsonSerializer.Deserialize<DiscreteInterval<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the same discrete-interval shapes round-trip under the Compact policy.
    /// </summary>
    [TestMethod]
    [DataRow("Closed")]
    [DataRow("Empty")]
    [DataRow("Singleton")]
    [DataRow("AtLeast")]
    [DataRow("AtMost")]
    [DataRow("All")]
    public void CompactPolicy_WhenRoundTripping_ShouldPreserveValue(string shape)
    {
        DiscreteInterval<int> original = Shape(shape);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Compact);

        string json = JsonSerializer.Serialize(original, options);
        DiscreteInterval<int> recovered = JsonSerializer.Deserialize<DiscreteInterval<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that an open discrete interval whose bounds admit no integer round-trips as the empty interval.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTrippingOpenWithNoInteger_ShouldYieldEmpty()
    {
        DiscreteInterval<int> original = DiscreteInterval<int>.Open(1, 2);
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        DiscreteInterval<int> recovered =
            JsonSerializer.Deserialize<DiscreteInterval<int>>(JsonSerializer.Serialize(original, options), options);

        Assert.IsTrue(original.IsEmpty);
        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Returns the discrete interval named by <paramref name="shape" />.
    /// </summary>
    /// <param name="shape">The factory name.</param>
    /// <returns>The corresponding discrete interval.</returns>
    private static DiscreteInterval<int> Shape(string shape) =>
        shape switch
        {
            "Closed" => DiscreteInterval<int>.Closed(1, 5),
            "Empty" => DiscreteInterval<int>.Empty,
            "OpenEmpty" => DiscreteInterval<int>.Open(1, 2),
            "Singleton" => DiscreteInterval<int>.Singleton(7),
            "AtLeast" => DiscreteInterval<int>.AtLeast(5),
            "AtMost" => DiscreteInterval<int>.AtMost(5),
            "All" => DiscreteInterval<int>.All,
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
}
