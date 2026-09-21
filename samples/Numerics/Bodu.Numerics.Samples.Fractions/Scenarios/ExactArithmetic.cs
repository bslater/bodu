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
        SampleConsole.Scenario(
            "Fraction<T> - exact rational arithmetic",
            what: "Reduces a fraction on construction, runs the four operators, checks the additive and " +
                  "multiplicative identities, and adds tenths - then repeats with a numerator of 10^20.",
            why: "Binary floating point cannot represent one tenth, so 0.1 + 0.2 != 0.3 in double and every " +
                 "subsequent comparison inherits that error. A rational keeps a numerator and a denominator, so " +
                 "tenths are exact and stay exact however long the chain gets. The second half is the other half " +
                 "of the argument: backed by BigInteger the numerator can exceed what a long holds, so precision " +
                 "does not quietly fall off a cliff at a magnitude nobody tested.",
            expect: "2/4 reduces to 1/2 at construction, not on demand, so equality and hashing work on the " +
                    "canonical form. 1/10 + 2/10 is exactly 3/10 rather than 0.30000000000000004, and the " +
                    "10^20 sum is exact to the last digit.");

        // The two-argument constructor reduces to canonical form immediately: 2/4 becomes 1/2.
        var oneHalf = new Fraction<int>(2, 4);
        var oneThird = new Fraction<int>(1, 3);
        Console.WriteLine($"  2/4 reduces to        : {oneHalf}  (reduced at construction, so equality and GetHashCode work on the canonical form rather than on 2 and 4)");

        // Operators evaluate exactly and return an already-reduced result.
        Console.WriteLine($"  1/2 + 1/3             : {oneHalf + oneThird}");
        Console.WriteLine($"  1/2 - 1/3             : {oneHalf - oneThird}");
        Console.WriteLine($"  1/2 * 1/3             : {oneHalf * oneThird}");
        Console.WriteLine($"  1/2 / 1/3             : {oneHalf / oneThird}");

        // Zero and One are the additive and multiplicative identities.
        Console.WriteLine($"  identities            : Zero={Fraction<int>.Zero}, One={Fraction<int>.One}  (Zero and One come from INumber<T>, which is what lets generic algorithms seed an accumulator)");
        Console.WriteLine($"  1/2 + Zero, 1/2 * One : {oneHalf + Fraction<int>.Zero}, {oneHalf * Fraction<int>.One}");

        // The classic floating-point trap: 1/10 + 2/10 is exactly 3/10 here, never 0.30000000000000004.
        var sum = new Fraction<int>(1, 10) + new Fraction<int>(2, 10);
        Console.WriteLine($"  1/10 + 2/10 exactly   : {sum}  (expected 3/10 exactly - the same sum in double gives 0.30000000000000004, and every later comparison inherits that)");

        // A BigInteger backing type never overflows, so wide numerators stay exact.
        var big = new Fraction<BigInteger>(BigInteger.Pow(10, 20), 7)
                + new Fraction<BigInteger>(1, 7);
        Console.WriteLine($"  (10^20)/7 + 1/7        : {big}  (BigInteger-backed, so precision does not fall off a cliff at a magnitude nobody thought to test)");

        Console.WriteLine();
    }
}
