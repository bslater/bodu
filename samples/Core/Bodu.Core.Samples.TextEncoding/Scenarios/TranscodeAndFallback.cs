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
        SampleConsole.Scenario(
            "EncodingExtensions - transcode, preamble, fallback",
            what: "Transcodes a phrase between UTF-16 and UTF-8 and back, adds and strips a preamble, then encodes " +
                  "a non-ASCII character to ASCII under both the replacement and the exception fallback.",
            why: "The fallback choice is the one that matters, because the two behaviours fail in opposite " +
                 "directions. Replacement never throws and silently substitutes '?' - fine for a log line, " +
                 "catastrophic for a name, an identifier or anything that will be compared or stored. The " +
                 "exception fallback refuses instead, telling you which character it could not represent. " +
                 "Defaulting to replacement is how mojibake gets written to a database and only noticed later.",
            expect: "UTF-16 takes 32 bytes to UTF-8's 17 for the same 16 characters, because ASCII costs one byte " +
                    "in UTF-8 and two in UTF-16, and the round trip is byte-identical. Under ASCII, the same " +
                    "input either becomes \"caf?\" or throws EncoderFallbackException naming the character.");

        // --- Transcode: UTF-16LE bytes -> UTF-8 bytes, and back, purely at the byte level. ---
        var utf16 = Encoding.Unicode.GetBytes(Phrase);                 // UTF-16LE, 2 bytes per BMP code unit
        var utf8 = Encoding.Unicode.Transcode(utf16, Encoding.UTF8);   // re-encode without an intermediate string
        var roundTrip = Encoding.UTF8.Transcode(utf8, Encoding.Unicode);

        Console.WriteLine($"  UTF-16 bytes     : {utf16.Length}  (expected 32 - two bytes per character, whether or not the character needs them)");
        Console.WriteLine($"  -> UTF-8 bytes   : {utf8.Length}  (expected 17 - one byte per ASCII character plus two for the é; almost half the size for this text)");
        Console.WriteLine($"  -> back to UTF-16: {roundTrip.Length}  round-trips: {utf16.SequenceEqual(roundTrip)}  (expected True - both encodings cover the whole of Unicode, so transcoding between them loses nothing)");

        // --- Preamble handling: emit a BOM-prefixed blob, then confirm and strip it. ---
        var withBom = Encoding.UTF8.GetBytesWithPreamble(Phrase);
        var stripped = Encoding.UTF8.StripPreamble(withBom);
        Console.WriteLine($"  UTF-8 +preamble  : {withBom.Length} bytes  (3 more than the text: the preamble is data, and it is why an unstripped file starts with an invisible character)");
        Console.WriteLine($"  after StripPreamble: {stripped.Length} bytes  (back to 17 - strip before comparing or parsing, never after)");

        // --- Fallback policy: ASCII cannot represent 'é'. The two policies disagree on what to do. ---
        // Replacement fallback substitutes a placeholder and keeps going.
        var asciiReplace = Encoding.ASCII.WithReplacementFallbacks(encoderReplacement: "?");
        var replaced = asciiReplace.GetString(asciiReplace.GetBytes(Phrase));
        Console.WriteLine($"  ASCII replacement: \"{replaced}\"  (the é became ? and nothing was raised - silent, irreversible, and the default)");

        // Exception fallback refuses instead, throwing on the first un-encodable character.
        var asciiThrow = Encoding.ASCII.WithExceptionFallbacks();
        try
        {
            _ = asciiThrow.GetBytes(Phrase);
        }
        catch (EncoderFallbackException ex)
        {
            Console.WriteLine($"  ASCII exception  : threw {ex.GetType().Name} on \u0027{ex.CharUnknown}\u0027  (the same input, refused rather than mangled, and it names the offending character)");
        }

        Console.WriteLine();
    }
}
