// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetJsonConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Verifies that <see cref="IntervalSetJsonConverter{T}" /> serializes an interval set as a JSON array of its pieces and
/// round-trips empty, multi-piece, and unbounded (complement) sets.
/// </summary>
[TestClass]
public class IntervalSetJsonConverterTests
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
    /// Verifies that the Compact form of a two-piece set is a JSON array of the pieces' bracket strings.
    /// </summary>
    [TestMethod]
    public void CompactPolicy_WhenSerializingTwoPieces_ShouldEmitArrayOfBracketForms()
    {
        var set = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(8, 9));

        string json = JsonSerializer.Serialize(set, Options(NumericsJsonPolicy.Compact));

        Assert.AreEqual("[\"[1, 3]\",\"[8, 9]\"]", json);
    }

    /// <summary>
    /// Verifies that the empty set serializes as the empty JSON array and round-trips.
    /// </summary>
    [TestMethod]
    public void StrictPolicy_WhenRoundTrippingEmpty_ShouldPreserveEmptyArray()
    {
        IntervalSet<int> original = IntervalSet<int>.Empty;
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        string json = JsonSerializer.Serialize(original, options);
        IntervalSet<int> recovered = JsonSerializer.Deserialize<IntervalSet<int>>(json, options);

        Assert.AreEqual("[]", json);
        Assert.IsTrue(recovered.IsEmpty);
        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that a multi-piece set round-trips under both the Strict and Compact policies.
    /// </summary>
    [TestMethod]
    [DataRow(NumericsJsonPolicy.Strict)]
    [DataRow(NumericsJsonPolicy.Compact)]
    public void RoundTrip_WhenMultiPiece_ShouldPreserveValue(NumericsJsonPolicy policy)
    {
        var original = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.OpenClosed(5, 8));
        JsonSerializerOptions options = Options(policy);

        string json = JsonSerializer.Serialize(original, options);
        IntervalSet<int> recovered = JsonSerializer.Deserialize<IntervalSet<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that a set containing unbounded pieces — produced by <see cref="IntervalSet{T}.Complement" /> — round-trips.
    /// </summary>
    [TestMethod]
    [DataRow(NumericsJsonPolicy.Strict)]
    [DataRow(NumericsJsonPolicy.Compact)]
    public void RoundTrip_WhenComplementHasUnboundedPieces_ShouldPreserveValue(NumericsJsonPolicy policy)
    {
        IntervalSet<int> original = IntervalSet<int>.Of(Interval<int>.Closed(0, 5)).Complement();
        JsonSerializerOptions options = Options(policy);

        string json = JsonSerializer.Serialize(original, options);
        IntervalSet<int> recovered = JsonSerializer.Deserialize<IntervalSet<int>>(json, options);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that deserializing a non-array token throws <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTokenIsNotArray_ShouldThrowJsonException()
    {
        JsonSerializerOptions options = Options(NumericsJsonPolicy.Strict);

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<IntervalSet<int>>("{\"lower\":1}", options);
        });
    }
}
