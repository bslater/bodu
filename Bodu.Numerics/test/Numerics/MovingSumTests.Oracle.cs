// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingSumTests.Oracle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Extensions;
using Bodu.Test;

namespace Bodu.Numerics;

public partial class MovingSumTests
{
    /// <summary>
    /// Verifies that over seeded random streams and a spread of capacities, the window matches a naive
    /// recomputation of the last-N sum and mean after every add.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Add_WhenSweepingSeededStreams_ShouldMatchNaiveLastNOracle()
    {
        foreach (var capacity in new[] { 1, 2, 3, 7, 64 })
        {
            var random = new Random(1000 + capacity);
            var window = new MovingSum<double>(capacity);
            var samples = new List<double>();

            for (var n = 0; n < 500; n++)
            {
                var sample = (random.NextDouble() * 200.0) - 100.0;
                samples.Add(sample);
                window.Add(sample);

                var tail = samples.TakeLast(Math.Min(samples.Count, capacity)).ToArray();
                var expected = tail.Sum();

                Assert.AreEqual(Math.Min(samples.Count, capacity), window.Count, $"Count diverged (capacity {capacity}, n = {n}).");
                Assert.AreEqual(expected, window.Sum, Math.Abs(expected) * 1e-9 + 1e-9, $"Sum diverged (capacity {capacity}, n = {n}).");
                Assert.AreEqual(tail.Average(), window.Mean, Math.Abs(tail.Average()) * 1e-9 + 1e-9, $"Mean diverged (capacity {capacity}, n = {n}).");
            }
        }
    }

    /// <summary>
    /// Verifies that full windows agree with the <c>Windowed</c> sequence operator's snapshot-recomputed sums over
    /// the same stream.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Add_WhenComparedAgainstWindowedOperator_ShouldMatchEveryFullWindowSum()
    {
        const int Capacity = 5;
        var random = new Random(77);
        var samples = Enumerable.Range(0, 300).Select(_ => (random.NextDouble() * 20.0) - 10.0).ToArray();

        var expectedSums = samples.Windowed(Capacity, w => w.Sum()).ToArray();

        var window = new MovingSum<double>(Capacity);
        var actualSums = new List<double>();

        foreach (var sample in samples)
        {
            window.Add(sample);
            if (window.IsFull)
                actualSums.Add(window.Sum);
        }

        Assert.AreEqual(expectedSums.Length, actualSums.Count);
        for (var i = 0; i < expectedSums.Length; i++)
            Assert.AreEqual(expectedSums[i], actualSums[i], Math.Abs(expectedSums[i]) * 1e-9 + 1e-9, $"Window sum diverged at index {i}.");
    }

    /// <summary>
    /// Verifies that the periodic exact rebuild bounds floating-point drift: alternating huge and tiny samples whose
    /// naive subtract-on-evict sum would drift stays equal to an exact recomputation at every full turnover.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Add_WhenAlternatingHugeAndTinySamples_ShouldRebuildAwayDrift()
    {
        const int Capacity = 4;
        var window = new MovingSum<double>(Capacity);
        var samples = new List<double>();

        for (var n = 0; n < 400; n++)
        {
            var sample = n % 2 == 0 ? 1e16 : 1.0;
            samples.Add(sample);
            window.Add(sample);

            // At every full window turnover the sum has just been rebuilt exactly, so it must equal the exact
            // last-N recomputation with no accumulated tolerance.
            if (samples.Count > Capacity && (samples.Count - Capacity) % Capacity == 0)
            {
                var exact = samples.TakeLast(Capacity).Sum();
                Assert.AreEqual(exact, window.Sum, $"Rebuilt sum diverged at n = {n}.");
            }
        }
    }
}
