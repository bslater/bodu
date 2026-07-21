// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParseAndFormat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.Fractions.Scenarios;

/// <summary>
/// Demonstrates the text surface of <see cref="Fraction{T}" />: parsing the canonical
/// "numerator/denominator" form (and the non-throwing <c>TryParse</c>), the several
/// <c>ToString</c> shapes, and the span/UTF-8 <c>TryFormat</c> path used by formatters.
/// </summary>
public static class ParseAndFormat
{
    /// <summary>
    /// Round-trips fractions through parse and format, showing the alternate string shapes.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Fraction<T>: parse and format ---");

        // Parse the canonical form; parsing also reduces, so "6/8" arrives as 3/4.
        var parsed = Fraction<int>.Parse("6/8", CultureInfo.InvariantCulture);
        Console.WriteLine($"Parse(\"6/8\")            : {parsed.ToString()}");

        // TryParse never throws - it reports failure through its bool return.
        var ok = Fraction<int>.TryParse("22/7", CultureInfo.InvariantCulture, out var approxPi);
        var bad = Fraction<int>.TryParse("not-a-fraction", CultureInfo.InvariantCulture, out _);
        Console.WriteLine($"TryParse(\"22/7\")        : {ok} -> {approxPi.ToString()}");
        Console.WriteLine($"TryParse(\"not-a-fraction\"): {bad}");

        // Alternate ToString shapes render the same value for different audiences.
        var improper = new Fraction<int>(7, 3);
        Console.WriteLine($"ToString()              : {improper.ToString()}");
        Console.WriteLine($"ToMixedNumberString()   : {improper.ToMixedNumberString(CultureInfo.InvariantCulture)}");
        Console.WriteLine($"ToPercentString()       : {new Fraction<int>(1, 4).ToPercentString(CultureInfo.InvariantCulture)}");

        // TryFormat writes into a caller-owned char span - the allocation-free formatting path.
        Span<char> chars = stackalloc char[16];
        improper.TryFormat(chars, out var charsWritten, default, CultureInfo.InvariantCulture);
        Console.WriteLine($"TryFormat(char)         : '{new string(chars[..charsWritten])}' ({charsWritten} chars)");

        // The UTF-8 overload writes bytes directly, for pipelines that never touch a string.
        Span<byte> bytes = stackalloc byte[16];
        improper.TryFormat(bytes, out var bytesWritten, default, CultureInfo.InvariantCulture);
        Console.WriteLine($"TryFormat(utf8)         : '{Encoding.UTF8.GetString(bytes[..bytesWritten])}' ({bytesWritten} bytes)");

        Console.WriteLine();
    }
}
