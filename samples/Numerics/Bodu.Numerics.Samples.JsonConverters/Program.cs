// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Samples.JsonConverters.Scenarios;

namespace Bodu.Numerics.Samples.JsonConverters;

/// <summary>
/// Entry point for the JSON-serialization sample: the companion
/// <c>Bodu.Numerics.Serialization.Json</c> package that teaches <c>System.Text.Json</c> the
/// <c>Bodu.Numerics</c> types. Scenarios cover registering the converter set, the policy-selected
/// wire shapes, the <c>Fraction&lt;T&gt;</c> JSON helpers, and a nested POCO graph. Everything runs
/// offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Numerics.Samples.JsonConverters");
        Console.WriteLine("====================================");
        Console.WriteLine();

        RegisterConverters.Run();
        PolicyShapes.Run();
        FractionExtensions.Run();
        NestedGraph.Run();

        Console.WriteLine("Done.");
    }
}
