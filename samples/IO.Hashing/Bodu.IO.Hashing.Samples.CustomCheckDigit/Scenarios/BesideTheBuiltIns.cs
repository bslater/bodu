// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BesideTheBuiltIns.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Samples.CustomCheckDigit.Scenarios;

/// <summary>
/// Demonstrates the payoff of deriving <see cref="CheckDigitAlgorithm" />: the custom scheme is
/// a drop-in peer of the built-ins. Code written against the base class — here a small issuing
/// harness — drives <see cref="SkuCheckDigit" />, <see cref="Luhn" />, and
/// <see cref="Damm" /> identically.
/// </summary>
public static class BesideTheBuiltIns
{
    /// <summary>
    /// Runs the custom scheme through a base-class-typed harness beside two built-ins.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CheckDigitAlgorithm polymorphism: custom beside built-in ---");

        var payload = "31415926";

        // The harness only knows the abstract base.
        foreach (CheckDigitAlgorithm algorithm in new CheckDigitAlgorithm[] { new SkuCheckDigit(), new Luhn(), new Damm() })
        {
            algorithm.Append(payload);
            Console.WriteLine($"  {algorithm.AlgorithmName,-8}: {payload} -> check '{algorithm.GetCurrentCheckDigit()}'");
            algorithm.Reset();
        }

        Console.WriteLine("one issuing pipeline, any scheme - the same pattern the contract tests verify.");

        Console.WriteLine();
    }
}
