// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RegistryComparison.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Samples.Text.Encoding.CustomEncoding.Scenarios;

/// <summary>
/// Demonstrates the payoff of implementing <see cref="IBinaryEncoding" />: the custom codec is
/// a drop-in peer of the built-in catalogue. Code written against the interface — here a tiny
/// comparison harness — drives <see cref="Base36Encoding" /> and the registry's encodings
/// identically.
/// </summary>
public static class RegistryComparison
{
    /// <summary>
    /// Runs the custom codec side by side with catalogue encodings through one interface.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "IBinaryEncoding - the custom codec as a peer of the catalogue",
            what: "Puts the custom Base36 codec into an array alongside four registry encodings, drives all five "
                + "through the interface to encode and round-trip one payload, and asks the custom codec for its "
                + "worst-case encoded length.",
            why: "This is the reason to implement the library's interface rather than exposing a pair of static "
                + "methods. Anything written against IBinaryEncoding - a config-driven pipeline, a comparison "
                + "harness, a test suite, the registry itself - accepts the custom codec without knowing it "
                + "exists. GetMaxEncodedLength is part of that bargain: a caller that wants to encode into a "
                + "buffer it owns needs to size it before calling, and it can only do that if every codec answers "
                + "the question the same way.",
            expect: "One loop, five codecs, each reporting its own name and round-tripping correctly - the "
                + "harness never branches on which one it holds. Character counts fall as the alphabet grows, "
                + "and the Base36 row sits between Crockford Base32 and Base58 as its alphabet size predicts. "
                + "GetMaxEncodedLength returns an upper bound rather than the exact length, which is what makes "
                + "it safe to size a buffer with before the data is known.");

        var payload = new byte[] { 0x27, 0x10, 0xFF, 0x00, 0x42, 0x9C };

        // The harness only knows the interface; Base36 slots in beside the registry.
        IBinaryEncoding[] encodings =
        [
            BinaryEncodings.Get("base16"),
            BinaryEncodings.Get("base32-crockford"),
            new Base36Encoding(),
            BinaryEncodings.Get("base58"),
            BinaryEncodings.Get("base64"),
        ];

        Console.WriteLine($"  payload: 6 bytes");
        foreach (var encoding in encodings)
        {
            var text = encoding.Encode(payload);
            var restored = encoding.Decode(text).AsSpan().SequenceEqual(payload);
            Console.WriteLine($"  {encoding.Name,-16} {text,-14} ({text.Length} chars, round trip {restored})");
        }

        Console.WriteLine("  (every row round trips, and the custom codec is reached through the same interface as the built-ins)");

        // GetMaxEncodedLength budgets the destination for Try* calls, custom codec included.
        var base36 = new Base36Encoding();
        Console.WriteLine($"  base36.GetMaxEncodedLength(6) = {base36.GetMaxEncodedLength(6)} chars (actual: {base36.Encode(payload).Length})"
            + "  (an upper bound, not the exact length - it has to be safe for the worst-case payload of that size)");

        Console.WriteLine();
    }
}
