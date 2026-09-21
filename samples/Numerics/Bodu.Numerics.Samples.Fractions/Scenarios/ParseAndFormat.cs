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
        SampleConsole.Scenario(
            "Fraction<T> - parse and format",
            what: "Parses fraction text, shows TryParse on a good and a malformed input, then renders one value " +
                  "as a plain fraction, a mixed number and a percentage, and writes it through both span formatters.",
            why: "A rational is only useful in a system if it survives the round trip to text and back, which is " +
                  "what makes it safe in configuration, a wire format or a database column. Parse reduces on the " +
                  "way in, so 6/8 and 3/4 are the same value and the text form does not have to be canonical. The " +
                  "span and UTF-8 formatters exist so that writing one into a buffer costs no intermediate string.",
            expect: "6/8 parses to 3/4 - reduced on construction. TryParse returns False for nonsense rather than " +
                    "throwing. The same 7/3 renders three ways, and both TryFormat overloads report the same three " +
                    "units written, one in chars and one in UTF-8 bytes.");

        // Parse the canonical form; parsing also reduces, so "6/8" arrives as 3/4.
        var parsed = Fraction<int>.Parse("6/8", CultureInfo.InvariantCulture);
        Console.WriteLine($"  Parse(\"6/8\")            : {parsed.ToString()}  (expected 3/4 - reduced on the way in, so stored text does not have to be canonical)");

        // TryParse never throws - it reports failure through its bool return.
        var ok = Fraction<int>.TryParse("22/7", CultureInfo.InvariantCulture, out var approxPi);
        var bad = Fraction<int>.TryParse("not-a-fraction", CultureInfo.InvariantCulture, out _);
        Console.WriteLine($"  TryParse(\"22/7\")        : {ok} -> {approxPi.ToString()}  (already in lowest terms, so it parses unchanged)");
        Console.WriteLine($"  TryParse(\"not-a-fraction\"): {bad}  (expected False - malformed input is a return value, not an exception, which is what makes this safe on untrusted text)");

        // Alternate ToString shapes render the same value for different audiences.
        var improper = new Fraction<int>(7, 3);
        Console.WriteLine($"  ToString()              : {improper.ToString()}");
        Console.WriteLine($"  ToMixedNumberString()   : {improper.ToMixedNumberString(CultureInfo.InvariantCulture)}  (the same 7/3 for a human reader; the value is unchanged, only its rendering)");
        Console.WriteLine($"  ToPercentString()       : {new Fraction<int>(1, 4).ToPercentString(CultureInfo.InvariantCulture)}");

        // TryFormat writes into a caller-owned char span - the allocation-free formatting path.
        Span<char> chars = stackalloc char[16];
        improper.TryFormat(chars, out var charsWritten, default, CultureInfo.InvariantCulture);
        Console.WriteLine($"  TryFormat(char)         : '{new string(chars[..charsWritten])}' ({charsWritten} chars)  (writes into a caller-supplied span - no intermediate string allocated)");

        // The UTF-8 overload writes bytes directly, for pipelines that never touch a string.
        Span<byte> bytes = stackalloc byte[16];
        improper.TryFormat(bytes, out var bytesWritten, default, CultureInfo.InvariantCulture);
        Console.WriteLine($"  TryFormat(utf8)         : '{Encoding.UTF8.GetString(bytes[..bytesWritten])}' ({bytesWritten} bytes)  (the same via UTF-8 bytes, for writing straight to a wire format or a file)");

        Console.WriteLine();
    }
}
