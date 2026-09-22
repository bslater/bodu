// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Samples.ComplexNumbers.Scenarios;

namespace Bodu.Numerics.Samples.ComplexNumbers;

/// <summary>
/// Entry point for the complex-number sample: <c>Complex&lt;T&gt;</c> from <c>Bodu.Numerics</c> — the generic
/// counterpart of the <c>double</c>-only <c>System.Numerics.Complex</c>. Every arithmetic and transcendental
/// result is printed beside the BCL's answer for the same operation, so a correct run is verifiable by eye.
/// Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Numerics.Samples.ComplexNumbers");
        Console.WriteLine("====================================");
        Console.WriteLine();

        ComplexBasics.Run();
        PolarAndFunctions.Run();
        GenericPrecision.Run();

        Console.WriteLine("Done.");
    }
}
