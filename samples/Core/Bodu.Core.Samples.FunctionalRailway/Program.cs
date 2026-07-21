// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Core.Samples.FunctionalRailway.Scenarios;

namespace Bodu.Core.Samples.FunctionalRailway;

/// <summary>
/// Entry point for the functional-railway sample: the <c>Bodu.Functional</c> seam —
/// <c>Option&lt;T&gt;</c> for absence, <c>Result</c>/<c>Result&lt;T&gt;</c> for fallible pipelines,
/// <c>Either&lt;TLeft, TRight&gt;</c> for a typed choice, <c>Memoizer</c> for cached pure functions,
/// and the Task-based async companions. Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static async Task Main()
    {
        Console.WriteLine("Bodu.Core.Samples.FunctionalRailway");
        Console.WriteLine("===================================");
        Console.WriteLine();

        OptionBasics.Run();
        ResultRailway.Run();
        EitherChoice.Run();
        Memoization.Run();
        await AsyncRailway.RunAsync();

        Console.WriteLine("Done.");
    }
}
