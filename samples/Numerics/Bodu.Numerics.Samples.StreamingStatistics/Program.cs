// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Samples.StreamingStatistics.Scenarios;

namespace Bodu.Numerics.Samples.StreamingStatistics;

/// <summary>
/// Entry point for the streaming-statistics sample: the single-pass accumulators from
/// <c>Bodu.Numerics</c> — <c>RunningStatistics&lt;T&gt;</c>, the fixed-window
/// <c>MovingSum&lt;T&gt;</c> / <c>MovingMinMax&lt;T&gt;</c>, the streaming
/// <c>RunningQuantile&lt;T&gt;</c>, and the exact <c>BigDecimal</c>. Everything runs offline and
/// deterministically over fixed input streams.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Numerics.Samples.StreamingStatistics");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        RunningStats.Run();
        SlidingWindows.Run();
        Quantiles.Run();
        BigDecimalArithmetic.Run();

        Console.WriteLine("Done.");
    }
}
