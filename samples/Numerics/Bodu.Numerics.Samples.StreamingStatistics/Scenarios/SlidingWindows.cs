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
        Console.WriteLine("--- MovingSum / MovingMinMax: fixed windows ---");

        // Both windows hold the last 3 values; older values fall out automatically.
        var sum = new MovingSum<double>(capacity: 3);
        var minMax = new MovingMinMax<double>(capacity: 3);

        var series = new[] { 10.0, 12.0, 8.0, 20.0, 6.0, 6.0 };

        Console.WriteLine("value  windowSum  windowMean  windowMin  windowMax  full");
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
        Console.WriteLine($"final window sum            : {Fmt(sum.Sum)} over last {sum.Count} of capacity {sum.Capacity}");

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
