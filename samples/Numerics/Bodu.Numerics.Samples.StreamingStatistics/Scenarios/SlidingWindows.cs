// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SlidingWindows.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.StreamingStatistics.Scenarios;

/// <summary>
/// Demonstrates the fixed-window accumulators <see cref="MovingSum{T}" /> and
/// <see cref="MovingMinMax{T}" />: each keeps only the most recent <c>Capacity</c> values, so as new
/// values arrive the oldest drop out of the window and the running sum / extremes update in O(1).
/// </summary>
public static class SlidingWindows
{
    /// <summary>
    /// Streams a fixed series through a sum window and a min/max window, printing each step.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "MovingSum / MovingMinMax - fixed windows",
            what: "Pushes six values through a capacity-3 window, printing the running sum, mean, minimum and " +
                  "maximum after each, plus whether the window has filled yet.",
            why: "The naive sliding window recomputes over its contents on every push, which is O(k) per value " +
                 "and turns a hot loop quadratic. These keep the answer incrementally: the sum adds the arrival " +
                 "and subtracts the departure, and the min/max holds a monotonic deque so an extreme leaving the " +
                 "window is replaced in amortised constant time. The full flag matters because a partially filled " +
                 "window is a real state - reporting its mean as if the window were complete is a common bug at " +
                 "the start of a series.",
            expect: "The first two rows report full=False, since fewer than three values have arrived. Once full, " +
                    "each push evicts the oldest: the sum drops by the departing value and the window minimum " +
                    "rises when the smallest value leaves rather than lingering.");

        // Both windows hold the last 3 values; older values fall out automatically.
        var sum = new MovingSum<double>(capacity: 3);
        var minMax = new MovingMinMax<double>(capacity: 3);

        var series = new[] { 10.0, 12.0, 8.0, 20.0, 6.0, 6.0 };

        Console.WriteLine("  value  windowSum  windowMean  windowMin  windowMax  full");
        foreach (var value in series)
        {
            // Each Add pushes one value in and, once full, evicts the oldest.
            sum.Add(value);
            minMax.Add(value);

            Console.WriteLine(
                $"{Fmt(value),5}  {Fmt(sum.Sum),9}  {Fmt(sum.Mean),10}  " +
                $"{Fmt(minMax.Minimum),9}  {Fmt(minMax.Maximum),9}  {sum.IsFull}");
        }

        // After six pushes the window holds only the final three values {20, 6, 6}.
        Console.WriteLine($"  final window sum            : {Fmt(sum.Sum)} over last {sum.Count} of capacity {sum.Capacity}  (only the last 3 of the six pushed values contribute - the earlier ones were evicted, not merely down-weighted)");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats a double with two fixed decimals under the invariant culture for stable output.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant-culture fixed-point rendering.</returns>
    private static string Fmt(double value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);
}
