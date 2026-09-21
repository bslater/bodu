// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GuidConvenience.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Samples.Text.Encoding.EncodingTour.Scenarios;

/// <summary>
/// Demonstrates the <see cref="Guid" /> convenience overloads: identifiers destined for URLs,
/// file names, or log lines encode directly — no manual <c>ToByteArray</c> plumbing — and each
/// base trades length against alphabet safety differently.
/// </summary>
public static class GuidConvenience
{
    /// <summary>
    /// Encodes one fixed Guid across the families that ship Guid overloads.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Guid overloads - shorter identifiers without byte plumbing",
            what: "Encodes one fixed Guid through the families that ship Guid overloads, printing each result "
                + "beside its character count, then decodes the Base58 form back to the original value.",
            why: "A Guid is sixteen bytes, but its usual text form spends 36 characters on them. That matters "
                + "wherever identifiers are pasted, typed, logged or put in a path - a shorter form is less to "
                + "wrap, less to mistype, and cheaper in a URL. The overloads exist so this does not require "
                + "round-tripping through ToByteArray by hand, which is where endianness bugs get introduced: "
                + "Guid's in-memory byte order is not its string order, and hand-rolled conversions routinely "
                + "encode one and decode the other.",
            expect: "The same value in five widths, from 36 characters down to 22. Base58 and URL-safe Base64 are "
                + "the two worth reaching for - Base58 for an identifier a human may re-type, URL-safe Base64 "
                + "when it only has to survive a query string. The round trip returns the original Guid, so "
                + "nothing about byte order was lost.");

        var id = new Guid("8f3b2b6e-4a41-4a83-9c2e-1d6f5a0b9c47");

        var hex = Base16.Encode(id);
        var b32 = Base32.Encode(id, Base32Variant.Crockford);
        var b58 = Base58.Encode(id);
        var b64 = Base64.Encode(id, Base64Variant.UrlSafe);

        Console.WriteLine($"  Guid.ToString()   : {id} (36 chars)"
            + "  (the baseline every row below is shortening)");
        Console.WriteLine($"  Base16            : {hex} ({hex.Length} chars)");
        Console.WriteLine($"  Base32 Crockford  : {b32} ({b32.Length} chars)");
        Console.WriteLine($"  Base58            : {b58} ({b58.Length} chars)"
            + "  (the shortest here, and its alphabet omits the characters people confuse when re-typing)");
        Console.WriteLine($"  Base64 UrlSafe    : {b64} ({b64.Length} chars)"
            + "  (swaps + and / for - and _, so the value needs no percent-escaping in a URL)");

        // And straight back to the same Guid.
        Console.WriteLine($"  round trip        : {Base58.DecodeGuid(b58) == id}"
            + "  (expected True - the overloads fix the byte order on both sides, which is where hand-rolled conversions go wrong)");

        Console.WriteLine();
    }
}
