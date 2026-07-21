// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TranscodeAndFallback.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text;

namespace Bodu.Core.Samples.TextEncoding.Scenarios;

/// <summary>
/// Demonstrates <see cref="EncodingExtensions" />: re-encoding bytes from one encoding to another
/// (<c>Transcode</c>), writing and stripping a byte-order-mark (<c>GetBytesWithPreamble</c> / <c>StripPreamble</c>),
/// and choosing what happens when a character cannot be represented in the target encoding (the replacement
/// versus exception fallback policies).
/// </summary>
public static class TranscodeAndFallback
{
    // A phrase whose 'é' costs two bytes in UTF-8 and one UTF-16 code unit, so the byte counts differ visibly.
    private const string Phrase = "Hello, Bodu café";

    /// <summary>
    /// Transcodes between UTF-16 and UTF-8, round-trips a preamble, and contrasts the two fallback policies.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- EncodingExtensions: transcode, preamble, fallback ---");

        // --- Transcode: UTF-16LE bytes -> UTF-8 bytes, and back, purely at the byte level. ---
        var utf16 = Encoding.Unicode.GetBytes(Phrase);                 // UTF-16LE, 2 bytes per BMP code unit
        var utf8 = Encoding.Unicode.Transcode(utf16, Encoding.UTF8);   // re-encode without an intermediate string
        var roundTrip = Encoding.UTF8.Transcode(utf8, Encoding.Unicode);

        Console.WriteLine($"UTF-16 bytes     : {utf16.Length}");
        Console.WriteLine($"-> UTF-8 bytes   : {utf8.Length} (é is 2 bytes in UTF-8)");
        Console.WriteLine($"-> back to UTF-16: {roundTrip.Length}  round-trips: {utf16.SequenceEqual(roundTrip)}");

        // --- Preamble handling: emit a BOM-prefixed blob, then confirm and strip it. ---
        var withBom = Encoding.UTF8.GetBytesWithPreamble(Phrase);
        var stripped = Encoding.UTF8.StripPreamble(withBom);
        Console.WriteLine($"UTF-8 +preamble  : {withBom.Length} bytes (HasPreamble={Encoding.UTF8.HasPreamble()}, preamble={Encoding.UTF8.GetPreambleLength()}B)");
        Console.WriteLine($"after StripPreamble: {stripped.Length} bytes");

        // --- Fallback policy: ASCII cannot represent 'é'. The two policies disagree on what to do. ---
        // Replacement fallback substitutes a placeholder and keeps going.
        var asciiReplace = Encoding.ASCII.WithReplacementFallbacks(encoderReplacement: "?");
        var replaced = asciiReplace.GetString(asciiReplace.GetBytes(Phrase));
        Console.WriteLine($"ASCII replacement: \"{replaced}\"");

        // Exception fallback refuses instead, throwing on the first un-encodable character.
        var asciiThrow = Encoding.ASCII.WithExceptionFallbacks();
        try
        {
            _ = asciiThrow.GetBytes(Phrase);
        }
        catch (EncoderFallbackException ex)
        {
            Console.WriteLine($"ASCII exception  : threw {ex.GetType().Name} on '{ex.CharUnknown}' (UsesExceptionFallbacks={asciiThrow.UsesExceptionFallbacks()})");
        }

        Console.WriteLine();
    }
}
