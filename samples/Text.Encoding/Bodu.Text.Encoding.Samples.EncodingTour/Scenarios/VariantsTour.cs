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
        Console.WriteLine("--- One payload, every family and variant ---");

        var payload = "Bodu!"u8.ToArray();

        Console.WriteLine($"payload  : 5 bytes 'Bodu!'");
        Console.WriteLine($"Base16   : {Base16.Encode(payload)}");
        Console.WriteLine($"Base32   : {Base32.Encode(payload)}");
        Console.WriteLine($"Base45   : {Base45.Encode(payload)}");
        Console.WriteLine($"Base58   : {Base58.Encode(payload)}");
        Console.WriteLine($"Base62   : {Base62.Encode(payload)}");
        Console.WriteLine($"Base64   : {Base64.Encode(payload)}");
        Console.WriteLine($"Base85   : {Base85.Encode(payload)}");

        // Variants change the alphabet (and padding rules), not the API.
        Console.WriteLine();
        Console.WriteLine($"Base32 Standard  : {Base32.Encode(payload, Base32Variant.Standard)}");
        Console.WriteLine($"Base32 HexExt    : {Base32.Encode(payload, Base32Variant.HexExtended)}");
        Console.WriteLine($"Base32 Crockford : {Base32.Encode(payload, Base32Variant.Crockford)}");
        Console.WriteLine($"Base32 ZBase32   : {Base32.Encode(payload, Base32Variant.ZBase32)}");
        Console.WriteLine($"Base85 Ascii85   : {Base85.Encode(payload, Base85Variant.Ascii85)}");
        Console.WriteLine($"Base85 Z85       : {Base85.Encode([0xDE, 0xAD, 0xBE, 0xEF], Base85Variant.Z85)} (Z85 needs 4-byte alignment)");

        // Decode needs the same variant - alphabets are not interchangeable.
        var crockford = Base32.Encode(payload, Base32Variant.Crockford);
        var roundTripped = Base32.Decode(crockford, Base32Variant.Crockford);
        Console.WriteLine();
        Console.WriteLine($"decode with matching variant: {roundTripped.AsSpan().SequenceEqual(payload)}");

        Console.WriteLine();
    }
}
