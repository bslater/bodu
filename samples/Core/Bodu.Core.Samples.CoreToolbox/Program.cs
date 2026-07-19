// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Core.Samples.CoreToolbox.Scenarios;

namespace Bodu.Core.Samples.CoreToolbox;

/// <summary>
/// Entry point for the core-toolbox sample: a guided tour of the general-purpose building blocks in
/// <c>Bodu.Core</c> — the <c>SequenceGenerator</c> catalogue, the pooled <c>PooledBufferBuilder&lt;T&gt;</c>,
/// the LINQ-style enumerable operators, the string / comparable / numeric extension surfaces,
/// <c>WeekPattern</c>, and the <c>Bodu.Threading</c> async primitives. Everything runs offline with fixed
/// inputs, so the output is identical on every run.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static async Task Main()
    {
        Console.WriteLine("Bodu.Core.Samples.CoreToolbox");
        Console.WriteLine("=============================");
        Console.WriteLine();

        SequenceGenerators.Run();
        PooledBuffers.Run();
        EnumerableOperators.Run();
        StringTransforms.Run();
        WeekPatterns.Run();
        await AsyncPrimitives.RunAsync();

        Console.WriteLine("Done.");
    }
}
