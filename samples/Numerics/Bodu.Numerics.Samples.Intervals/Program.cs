// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Samples.Intervals.Scenarios;

namespace Bodu.Numerics.Samples.Intervals;

/// <summary>
/// Entry point for the interval-algebra sample: the <c>Bodu.Numerics</c> interval types —
/// <c>Interval&lt;T&gt;</c> over a continuous domain, <c>DiscreteInterval&lt;T&gt;</c> over the
/// integers, and the normalized <c>IntervalSet&lt;T&gt;</c>, together with the two-piece result
/// types <c>IntervalPair&lt;T&gt;</c> / <c>DiscreteIntervalPair&lt;T&gt;</c>. Everything runs
/// offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Numerics.Samples.Intervals");
        Console.WriteLine("===============================");
        Console.WriteLine();

        IntervalBasics.Run();
        SetAlgebra.Run();
        DiscreteIntervals.Run();
        IntervalSets.Run();

        Console.WriteLine("Done.");
    }
}
