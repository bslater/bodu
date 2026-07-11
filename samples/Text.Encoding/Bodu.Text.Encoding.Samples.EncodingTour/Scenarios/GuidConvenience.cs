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
        Console.WriteLine("--- Guid overloads: shorter identifiers, no byte plumbing ---");

        var id = new Guid("8f3b2b6e-4a41-4a83-9c2e-1d6f5a0b9c47");

        var hex = Base16.Encode(id);
        var b32 = Base32.Encode(id, Base32Variant.Crockford);
        var b58 = Base58.Encode(id);
        var b64 = Base64.Encode(id, Base64Variant.UrlSafe);

        Console.WriteLine($"Guid.ToString()   : {id} (36 chars)");
        Console.WriteLine($"Base16            : {hex} ({hex.Length} chars)");
        Console.WriteLine($"Base32 Crockford  : {b32} ({b32.Length} chars)");
        Console.WriteLine($"Base58            : {b58} ({b58.Length} chars)");
        Console.WriteLine($"Base64 UrlSafe    : {b64} ({b64.Length} chars)");

        // And straight back to the same Guid.
        Console.WriteLine($"round trip        : {Base58.DecodeGuid(b58) == id}");

        Console.WriteLine();
    }
}
