// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ContinuedFractions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Fractions.Scenarios;

/// <summary>
/// Demonstrates the continued-fraction and rational-approximation surface of
/// <see cref="Fraction{T}" />: expanding a rational into its simple-continued-fraction
/// coefficients and reconstructing it, approximating a real number to a bounded denominator,
/// and snapping an existing fraction to a smaller denominator.
/// </summary>
public static class ContinuedFractions
{
    /// <summary>
    /// Expands, reconstructs, and approximates rationals, printing each intermediate form.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Fraction<T>: continued fractions and approximation ---");

        // ToContinuedFraction yields the simple-continued-fraction coefficients [a0; a1, a2, ...].
        var value = new Fraction<int>(415, 93);
        var coefficients = value.ToContinuedFraction();
        Console.WriteLine($"415/93 expands to       : [{string.Join("; ", coefficients)}]");

        // FromContinuedFraction is the exact inverse - the coefficients reconstruct the value.
        var rebuilt = Fraction<int>.FromContinuedFraction(coefficients);
        Console.WriteLine($"reconstructed           : {rebuilt} (matches: {rebuilt == value})");

        // Approximate finds the closest fraction whose denominator does not exceed the bound.
        // Pi under a denominator of at most 100 lands on 311/99 - closer than the famous 22/7.
        var piApprox = Fraction<int>.Approximate(Math.PI, 100);
        Console.WriteLine($"Approximate(Pi, <=100)  : {piApprox} = {piApprox.ToDouble().ToString("F6", CultureInfo.InvariantCulture)}");

        // Raising the bound to 1000 admits more denominators and lands on the sharper convergent 355/113.
        var piSharper = Fraction<int>.Approximate(Math.PI, 1000);
        Console.WriteLine($"Approximate(Pi, <=1000) : {piSharper} = {piSharper.ToDouble().ToString("F6", CultureInfo.InvariantCulture)}");

        // LimitDenominator snaps an exact fraction to the nearest one under a denominator cap.
        var exact = new Fraction<int>(415, 93);
        var limited = exact.LimitDenominator(20);
        Console.WriteLine($"415/93 limited to d<=20 : {limited} = {limited.ToDouble().ToString("F6", CultureInfo.InvariantCulture)}");

        Console.WriteLine();
    }
}
