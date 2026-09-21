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
        SampleConsole.Scenario(
            "CheckDigitAlgorithm polymorphism - a custom scheme beside the built-ins",
            what: "Drives SkuCheckDigit, Luhn and Damm through one loop typed against the abstract base class, "
                + "printing each scheme's name and the digit it issues for the same payload.",
            why: "This is the reason for deriving the library's base class instead of writing a standalone helper. "
                + "An issuing pipeline, a validation middleware or a test harness can be written once against "
                + "CheckDigitAlgorithm and accept any scheme - including one the library has never heard of. It "
                + "also means the custom scheme inherits the streaming Append surface and the Reset lifecycle for "
                + "free, and can be held to the same contract tests as the built-in catalogue.",
            expect: "Three different digits for one payload, which is the correct outcome: each scheme is a "
                + "different function, so a value issued under one is not valid under another. The caller never "
                + "branches on which scheme it holds.");

        var payload = "31415926";

        // The harness only knows the abstract base.
        foreach (CheckDigitAlgorithm algorithm in new CheckDigitAlgorithm[] { new SkuCheckDigit(), new Luhn(), new Damm() })
        {
            algorithm.Append(payload);
            Console.WriteLine($"  {algorithm.AlgorithmName,-8}: {payload} -> check '{algorithm.GetCurrentCheckDigit()}'");

            // Reset clears the accumulated state so the same instance can issue the next value.
            algorithm.Reset();
        }

        Console.WriteLine("  (three schemes, three digits - a SKU-731 value is not a valid Luhn value, so the scheme travels with the identifier)");
        Console.WriteLine("  one issuing pipeline, any scheme - the same pattern the contract tests verify.");

        Console.WriteLine();
    }
}
