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
        SampleConsole.Scenario(
            "StringExtensions / ComparableExtensions / NumericExtensions",
            what: "Converts one phrase through five casing conventions, strips diacritics, truncates, then clamps " +
                  "and range-tests values and runs the integer helpers.",
            why: "Each of these is a one-liner people rewrite per project and get subtly wrong. Casing conversions " +
                 "have to decide what counts as a word boundary before they can be consistent; slug and " +
                 "diacritic-stripping have to be stable, because a URL that changes is a broken link. Clamp and " +
                 "IsBetween replace the min/max and double-comparison idioms where the argument order is easy to " +
                 "invert and the inclusivity easy to get backwards.",
            expect: "The same phrase yields five different shapes from one parse of its word boundaries. " +
                    "RemoveDiacritics keeps the letters and drops the marks rather than dropping the characters. " +
                    "Clamp returns the nearer bound, so 42 becomes 10 and -3 becomes 0.");

        // --- StringExtensions: convert one human phrase into every common casing convention. ---
        const string phrase = "Hello, World! Foo Bar";
        Console.WriteLine($"  ToSlug           : {phrase.ToSlug()}  (URL-safe and stable - the same input must always give the same slug, or every link built from it breaks)");
        Console.WriteLine($"  ToKebabCase      : {phrase.ToKebabCase()}  (same word boundaries as the slug, applied to a different separator)");
        Console.WriteLine($"  ToSnakeCase      : {phrase.ToSnakeCase()}  (one parse of word boundaries drives every casing form, which is what keeps them consistent)");
        Console.WriteLine($"  ToPascalCase     : {phrase.ToPascalCase()}  (boundaries become capitals rather than separators)");
        Console.WriteLine($"  ToConstantCase   : {phrase.ToConstantCase()}  (snake case upper-cased - the conventional shape for an environment variable)");

        // RemoveDiacritics folds accented characters down to their base ASCII letters.
        Console.WriteLine($"  RemoveDiacritics : {"Crème brûlée à la mode".RemoveDiacritics()}  (keeps the letters and drops the marks: e-acute becomes e, not nothing)");

        // Truncate caps the length, appending an ellipsis when it actually had to cut.
        Console.WriteLine($"  Truncate(12)     : {"The quick brown fox".Truncate(12)}  (a hard cut at 12 characters - no ellipsis is added unless one is asked for)");

        // --- ComparableExtensions: range helpers that read as English. ---
        Console.WriteLine($"  Clamp 42 to 0..10: {42.Clamp(0, 10)}  (expected 10 - returns the nearer bound; this replaces Math.Min(Math.Max(..)), where the nesting is easy to invert)");
        Console.WriteLine($"  Clamp -3 to 0..10: {(-3).Clamp(0, 10)}  (expected 0 - the other bound, from the same call)");
        Console.WriteLine($"  5 IsBetween 1..10: {5.IsBetween(1, 10)}  (expected True - inclusive at both ends, stated once rather than as two comparisons that can disagree)");
        Console.WriteLine($"  15 IsBetween 1.10: {15.IsBetween(1, 10)}");

        // --- NumericExtensions: number-theory helpers. ---
        Console.WriteLine($"  17 IsPrime       : {17.IsPrime()}  (expected True)");
        Console.WriteLine($"  18 IsPrime       : {18.IsPrime()}  (expected False - even, so it fails at the first check)");
        Console.WriteLine($"  GCD(48, 36)      : {48.GreatestCommonDivisor(36)}  (expected 12)");
        Console.WriteLine($"  LCM(4, 6)        : {4.LeastCommonMultiple(6)}  (expected 12 - coincidentally the same value as the GCD above, from unrelated inputs)");

        // RoundToSignificantDigits counts significant figures, not decimal places: 3.14159... -> 3.14,
        // but 12345.6789 at three figures would round to 12300.
        Console.WriteLine($"  RoundToSig(3)    : {3.14159265.RoundToSignificantDigits(3)}  (three SIGNIFICANT figures, not three decimal places - the distinction that matters for very large or very small values)");

        Console.WriteLine();
    }
}
