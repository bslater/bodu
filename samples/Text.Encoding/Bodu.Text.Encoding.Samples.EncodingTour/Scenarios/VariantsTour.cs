// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VariantsTour.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Samples.Text.Encoding.EncodingTour.Scenarios;

/// <summary>
/// Demonstrates the catalogue: one payload through every base family, and one family
/// (Base32/Base58/Base64/Base85) through its published variants — the alphabet is a parameter,
/// not a different API, so switching between RFC 4648 standard and Crockford or between
/// Ascii85 and Z85 is one enum argument.
/// </summary>
public static class VariantsTour
{
    /// <summary>
    /// Encodes the same payload across families and variants.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "One payload, every family and variant",
            what: "Encodes the same five bytes with each base family, then re-encodes one payload through four "
                + "Base32 alphabets and two Base85 alphabets, and decodes a Crockford string back with the "
                + "matching variant.",
            why: "A binary-to-text encoding exists to move bytes through a channel that only carries text - a "
                + "URL, a JSON string, an email header, a QR code. The families differ in how much they cost you: "
                + "Base16 doubles the size and is trivially readable, Base64 is about 4/3 and is the default "
                + "almost everywhere, Base85 is about 5/4 but uses punctuation that many channels mangle. The "
                + "variants exist because the alphabet is a channel decision, not an algorithm one - Crockford "
                + "drops the characters humans confuse, URL-safe Base64 drops the two that need percent-escaping. "
                + "Here they are one enum argument rather than a different API.",
            expect: "Encoded length grows as the alphabet shrinks, which is the whole trade. The four Base32 rows "
                + "encode identical bytes to visibly different text - the alphabet is part of the contract, so "
                + "decoding with the wrong variant yields wrong bytes or an error rather than a helpful guess. "
                + "Z85 is shown over four bytes because it requires 4-byte alignment.");

        var payload = "Bodu!"u8.ToArray();

        Console.WriteLine($"  payload  : 5 bytes 'Bodu!'"
            + "  (one fixed input, so every row below differs only by the encoding applied to it)");
        Console.WriteLine($"  Base16   : {Base16.Encode(payload)}"
            + "  (two characters per byte - the most expensive and the easiest to read by eye)");
        Console.WriteLine($"  Base32   : {Base32.Encode(payload)}");
        Console.WriteLine($"  Base45   : {Base45.Encode(payload)}");
        Console.WriteLine($"  Base58   : {Base58.Encode(payload)}");
        Console.WriteLine($"  Base62   : {Base62.Encode(payload)}");
        Console.WriteLine($"  Base64   : {Base64.Encode(payload)}");
        Console.WriteLine($"  Base85   : {Base85.Encode(payload)}"
            + "  (the most compact here, at the cost of punctuation many channels escape or mangle)");

        // Variants change the alphabet (and padding rules), not the API.
        Console.WriteLine();
        Console.WriteLine($"  Base32 Standard  : {Base32.Encode(payload, Base32Variant.Standard)}");
        Console.WriteLine($"  Base32 HexExt    : {Base32.Encode(payload, Base32Variant.HexExtended)}");
        Console.WriteLine($"  Base32 Crockford : {Base32.Encode(payload, Base32Variant.Crockford)}"
            + "  (excludes I, L, O and U, so a human re-typing the value cannot confuse them with 1 and 0)");
        Console.WriteLine($"  Base32 ZBase32   : {Base32.Encode(payload, Base32Variant.ZBase32)}");
        Console.WriteLine($"  Base85 Ascii85   : {Base85.Encode(payload, Base85Variant.Ascii85)}");
        Console.WriteLine($"  Base85 Z85       : {Base85.Encode([0xDE, 0xAD, 0xBE, 0xEF], Base85Variant.Z85)} (Z85 needs 4-byte alignment)");

        // Decode needs the same variant - alphabets are not interchangeable.
        var crockford = Base32.Encode(payload, Base32Variant.Crockford);
        var roundTripped = Base32.Decode(crockford, Base32Variant.Crockford);
        Console.WriteLine();
        Console.WriteLine($"  decode with matching variant: {roundTripped.AsSpan().SequenceEqual(payload)}"
            + "  (expected True - and note the variant must be passed again, because the alphabet is not recoverable from the text)");

        Console.WriteLine();
    }
}
