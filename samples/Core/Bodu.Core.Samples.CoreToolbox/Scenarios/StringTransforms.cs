// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringTransforms.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Core.Samples.CoreToolbox.Scenarios;

/// <summary>
/// Demonstrates three of the general-purpose extension surfaces in <c>Bodu.Extensions</c>:
/// <c>StringExtensions</c> (casing conventions, slugging, diacritic folding, truncation),
/// <c>ComparableExtensions</c> (<c>Clamp</c> / <c>IsBetween</c> over any <see cref="IComparable{T}" />), and
/// <c>NumericExtensions</c> (primality, GCD / LCM, significant-figure rounding).
/// </summary>
public static class StringTransforms
{
    /// <summary>
    /// Runs each transform over a fixed input and prints the before/after (or computed) value.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- StringExtensions / ComparableExtensions / NumericExtensions ---");

        // --- StringExtensions: convert one human phrase into every common casing convention. ---
        const string phrase = "Hello, World! Foo Bar";
        Console.WriteLine($"ToSlug           : {phrase.ToSlug()}");
        Console.WriteLine($"ToKebabCase      : {phrase.ToKebabCase()}");
        Console.WriteLine($"ToSnakeCase      : {phrase.ToSnakeCase()}");
        Console.WriteLine($"ToPascalCase     : {phrase.ToPascalCase()}");
        Console.WriteLine($"ToConstantCase   : {phrase.ToConstantCase()}");

        // RemoveDiacritics folds accented characters down to their base ASCII letters.
        Console.WriteLine($"RemoveDiacritics : {"Crème brûlée à la mode".RemoveDiacritics()}");

        // Truncate caps the length, appending an ellipsis when it actually had to cut.
        Console.WriteLine($"Truncate(12)     : {"The quick brown fox".Truncate(12)}");

        // --- ComparableExtensions: range helpers that read as English. ---
        Console.WriteLine($"Clamp 42 to 0..10: {42.Clamp(0, 10)}");
        Console.WriteLine($"Clamp -3 to 0..10: {(-3).Clamp(0, 10)}");
        Console.WriteLine($"5 IsBetween 1..10: {5.IsBetween(1, 10)}");
        Console.WriteLine($"15 IsBetween 1.10: {15.IsBetween(1, 10)}");

        // --- NumericExtensions: number-theory helpers. ---
        Console.WriteLine($"17 IsPrime       : {17.IsPrime()}");
        Console.WriteLine($"18 IsPrime       : {18.IsPrime()}");
        Console.WriteLine($"GCD(48, 36)      : {48.GreatestCommonDivisor(36)}");
        Console.WriteLine($"LCM(4, 6)        : {4.LeastCommonMultiple(6)}");

        // RoundToSignificantDigits counts significant figures, not decimal places: 3.14159... -> 3.14,
        // but 12345.6789 at three figures would round to 12300.
        Console.WriteLine($"RoundToSig(3)    : {3.14159265.RoundToSignificantDigits(3)}");

        Console.WriteLine();
    }
}
