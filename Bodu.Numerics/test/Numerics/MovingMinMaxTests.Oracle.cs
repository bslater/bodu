// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingMinMaxTests.Oracle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

public partial class MovingMinMaxTests
{
    /// <summary>
    /// Verifies that over seeded random streams and a spread of capacities, the window matches a naive
    /// recomputation of the last-N extrema after every add.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Add_WhenSweepingSeededStreams_ShouldMatchNaiveLastNOracle()
    {
        foreach (var capacity in new[] { 1, 2, 3, 7, 64 })
        {
            var random = new Random(2000 + capacity);
            var window = new MovingMinMax<int>(capacity);
            var samples = new List<int>();

            for (var n = 0; n < 500; n++)
            {
                var sample = random.Next(-100, 101);
                samples.Add(sample);
                window.Add(sample);

                var tail = samples.TakeLast(Math.Min(samples.Count, capacity)).ToArray();

                Assert.AreEqual(Math.Min(samples.Count, capacity), window.Count, $"Count diverged (capacity {capacity}, n = {n}).");
                Assert.AreEqual(tail.Min(), window.Minimum, $"Minimum diverged (capacity {capacity}, n = {n}).");
                Assert.AreEqual(tail.Max(), window.Maximum, $"Maximum diverged (capacity {capacity}, n = {n}).");
            }
        }
    }

    /// <summary>
    /// Verifies thousands of adds — many complete deque wrap-arounds and sequence-number windows — against the naive
    /// last-N oracle, over a mix of alternating high/low values and long monotonic runs.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Add_WhenRunningThousandsOfAdds_ShouldMatchNaiveLastNOracle()
    {
        foreach (var capacity in new[] { 1, 2, 3, 4, 5 })
        {
            var window = new MovingMinMax<int>(capacity);
            var samples = new List<int>(5_000);

            for (var n = 0; n < 5_000; n++)
            {
                // Phases: alternating high/low, a long ascending run, then a long descending run.
                var phase = n / 1_000 % 3;
                var sample = phase switch
                {
                    0 => n % 2 == 0 ? 250 : -250,
                    1 => n % 1_000,
                    _ => 1_000 - (n % 1_000),
                };

                samples.Add(sample);
                window.Add(sample);

                var tail = samples.TakeLast(Math.Min(samples.Count, capacity)).ToArray();

                Assert.AreEqual(tail.Min(), window.Minimum, $"Minimum diverged (capacity {capacity}, n = {n}).");
                Assert.AreEqual(tail.Max(), window.Maximum, $"Maximum diverged (capacity {capacity}, n = {n}).");
            }
        }
    }

    /// <summary>
    /// Verifies the extrema against adversarial sawtooth sequences, whose repeated rises and falls exercise both
    /// deques' expiry and back-popping paths together.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Add_WhenSweepingSawtoothSequences_ShouldMatchNaiveLastNOracle()
    {
        foreach (var capacity in new[] { 2, 3, 5, 8 })
        {
            var window = new MovingMinMax<int>(capacity);
            var samples = new List<int>();

            for (var n = 0; n < 300; n++)
            {
                // Sawtooth with a period longer than the window: 0, 1, 2, 3, 4, 5, 6, 0, 1, 2, ...
                var sample = n % 7;
                samples.Add(sample);
                window.Add(sample);

                var tail = samples.TakeLast(Math.Min(samples.Count, capacity)).ToArray();

                Assert.AreEqual(tail.Min(), window.Minimum, $"Minimum diverged (capacity {capacity}, n = {n}).");
                Assert.AreEqual(tail.Max(), window.Maximum, $"Maximum diverged (capacity {capacity}, n = {n}).");
            }
        }
    }
}
