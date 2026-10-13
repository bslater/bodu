// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalJsonConverterTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

public partial class IntervalJsonConverterTests
{
    /// <summary>
    /// Verifies that the four half-bounded intervals over <see cref="double" /> anchored at the far infinity are written
    /// under every policy exactly as <see cref="Interval{T}.Empty" /> is, and read back as it.
    /// </summary>
    [TestMethod]
    public void Write_WhenHalfLineIsAnchoredAtFarInfinity_ForDouble_ShouldWriteTheEmptyInterval() =>
        AssertFarInfinityHalfLinesRoundTripAsEmpty<double>();

    /// <summary>
    /// Verifies that the four half-bounded intervals over <see cref="float" /> anchored at the far infinity are written
    /// under every policy exactly as <see cref="Interval{T}.Empty" /> is, and read back as it.
    /// </summary>
    [TestMethod]
    public void Write_WhenHalfLineIsAnchoredAtFarInfinity_ForSingle_ShouldWriteTheEmptyInterval() =>
        AssertFarInfinityHalfLinesRoundTripAsEmpty<float>();

    /// <summary>
    /// Verifies that the four half-bounded intervals over <see cref="Half" /> anchored at the far infinity are written
    /// under every policy exactly as <see cref="Interval{T}.Empty" /> is, and read back as it.
    /// </summary>
    [TestMethod]
    public void Write_WhenHalfLineIsAnchoredAtFarInfinity_ForHalf_ShouldWriteTheEmptyInterval() =>
        AssertFarInfinityHalfLinesRoundTripAsEmpty<Half>();

    /// <summary>
    /// Asserts that <c>AtLeast(+Infinity)</c>, <c>GreaterThan(+Infinity)</c>, <c>AtMost(-Infinity)</c> and
    /// <c>LessThan(-Infinity)</c> over <typeparamref name="T" /> serialize under every policy to the JSON of
    /// <see cref="Interval{T}.Empty" /> and deserialize to it.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    private static void AssertFarInfinityHalfLinesRoundTripAsEmpty<T>()
        where T : IFloatingPointIeee754<T>
    {
        Interval<T>[] shapes =
        [
            Interval<T>.AtLeast(T.PositiveInfinity),
            Interval<T>.GreaterThan(T.PositiveInfinity),
            Interval<T>.AtMost(T.NegativeInfinity),
            Interval<T>.LessThan(T.NegativeInfinity),
        ];

        foreach (NumericsJsonPolicy policy in new[] { NumericsJsonPolicy.Strict, NumericsJsonPolicy.Lenient, NumericsJsonPolicy.Compact })
        {
            JsonSerializerOptions options = new JsonSerializerOptions().AddNumericsJsonConverters(policy);
            string expected = JsonSerializer.Serialize(Interval<T>.Empty, options);

            foreach (Interval<T> shape in shapes)
            {
                string json = JsonSerializer.Serialize(shape, options);

                Assert.AreEqual(expected, json, $"{policy}: {shape}");
                Assert.AreEqual(Interval<T>.Empty, JsonSerializer.Deserialize<Interval<T>>(json, options), $"{policy}: {json}");
            }
        }
    }
}
