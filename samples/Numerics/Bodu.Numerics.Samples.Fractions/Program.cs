// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Samples.Fractions.Scenarios;

namespace Bodu.Numerics.Samples.Fractions;

/// <summary>
/// Entry point for the <c>Fraction&lt;T&gt;</c> sample: the exact-rational type from
/// <c>Bodu.Numerics</c> — canonical construction and arithmetic, parse/format round-trips,
/// generic-math participation as an <c>INumber&lt;T&gt;</c>, and continued-fraction expansion.
/// Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Numerics.Samples.Fractions");
        Console.WriteLine("===============================");
        Console.WriteLine();

        ExactArithmetic.Run();
        ParseAndFormat.Run();
        GenericMath.Run();
        ContinuedFractions.Run();

        Console.WriteLine("Done.");
    }
}
