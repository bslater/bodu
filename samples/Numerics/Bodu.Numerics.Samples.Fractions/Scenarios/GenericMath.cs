// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GenericMath.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Fractions.Scenarios;

/// <summary>
/// Demonstrates that <see cref="Fraction{T}" /> implements <see cref="INumber{TSelf}" />, so it
/// drops straight into any generic algorithm written against the .NET generic-math interfaces —
/// the same <c>Sum</c> method serves <see cref="int" /> and <see cref="Fraction{T}" /> alike.
/// </summary>
public static class GenericMath
{
    /// <summary>
    /// Sums a sequence with a single generic method, first over integers then over fractions.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Fraction<T>: generic math (INumber<T>) ---");

        // The same generic Sum below works for any INumber<T> - here plain integers.
        var ints = new[] { 1, 2, 3, 4, 5 };
        Console.WriteLine($"Sum<int>(1..5)          : {Sum(ints)}");

        // ...and here the exact harmonic series 1/1 + 1/2 + 1/3 + 1/4 + 1/5, with no rounding.
        var unitFractions = new[]
        {
            new Fraction<int>(1, 1),
            new Fraction<int>(1, 2),
            new Fraction<int>(1, 3),
            new Fraction<int>(1, 4),
            new Fraction<int>(1, 5),
        };

        var harmonic = Sum(unitFractions);
        Console.WriteLine($"Sum<Fraction>(H_5)      : {harmonic}");
        Console.WriteLine($"H_5 as double           : {harmonic.ToDouble().ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Adds every element of a sequence, starting from the type's additive identity.
    /// </summary>
    /// <typeparam name="T">Any numeric type implementing <see cref="INumber{TSelf}" />.</typeparam>
    /// <param name="values">The values to add.</param>
    /// <returns>The exact sum of <paramref name="values" />.</returns>
    private static T Sum<T>(IEnumerable<T> values)
        where T : INumber<T>
    {
        // T.Zero and operator+ both come from INumber<T>, so no per-type overloads are needed.
        var accumulator = T.Zero;
        foreach (var value in values)
        {
            accumulator += value;
        }

        return accumulator;
    }
}
