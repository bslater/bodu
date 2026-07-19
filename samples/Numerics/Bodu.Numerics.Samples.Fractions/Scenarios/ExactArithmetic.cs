// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExactArithmetic.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Fractions.Scenarios;

/// <summary>
/// Demonstrates <see cref="Fraction{T}" /> as an exact rational number: every value is held in
/// canonical (fully reduced) form on creation, and the four arithmetic operators stay exact — no
/// floating-point drift, no manual reduction.
/// </summary>
public static class ExactArithmetic
{
    /// <summary>
    /// Builds fractions from integer components and combines them with the arithmetic operators.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Fraction<T>: exact rational arithmetic ---");

        // The two-argument constructor reduces to canonical form immediately: 2/4 becomes 1/2.
        var oneHalf = new Fraction<int>(2, 4);
        var oneThird = new Fraction<int>(1, 3);
        Console.WriteLine($"2/4 reduces to        : {oneHalf}");

        // Operators evaluate exactly and return an already-reduced result.
        Console.WriteLine($"1/2 + 1/3             : {oneHalf + oneThird}");
        Console.WriteLine($"1/2 - 1/3             : {oneHalf - oneThird}");
        Console.WriteLine($"1/2 * 1/3             : {oneHalf * oneThird}");
        Console.WriteLine($"1/2 / 1/3             : {oneHalf / oneThird}");

        // Zero and One are the additive and multiplicative identities.
        Console.WriteLine($"identities            : Zero={Fraction<int>.Zero}, One={Fraction<int>.One}");
        Console.WriteLine($"1/2 + Zero, 1/2 * One : {oneHalf + Fraction<int>.Zero}, {oneHalf * Fraction<int>.One}");

        // The classic floating-point trap: 1/10 + 2/10 is exactly 3/10 here, never 0.30000000000000004.
        var sum = new Fraction<int>(1, 10) + new Fraction<int>(2, 10);
        Console.WriteLine($"1/10 + 2/10 exactly   : {sum}");

        // A BigInteger backing type never overflows, so wide numerators stay exact.
        var big = new Fraction<BigInteger>(BigInteger.Pow(10, 20), 7)
                + new Fraction<BigInteger>(1, 7);
        Console.WriteLine($"(10^20)/7 + 1/7        : {big}");

        Console.WriteLine();
    }
}
