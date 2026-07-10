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
        Console.WriteLine("--- IBinaryEncoding: custom codec next to the catalogue ---");

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

        Console.WriteLine($"payload: 6 bytes");
        foreach (var encoding in encodings)
        {
            var text = encoding.Encode(payload);
            var restored = encoding.Decode(text).AsSpan().SequenceEqual(payload);
            Console.WriteLine($"  {encoding.Name,-16} {text,-14} ({text.Length} chars, round trip {restored})");
        }

        // GetMaxEncodedLength budgets the destination for Try* calls, custom codec included.
        var base36 = new Base36Encoding();
        Console.WriteLine($"base36.GetMaxEncodedLength(6) = {base36.GetMaxEncodedLength(6)} chars (actual: {base36.Encode(payload).Length})");

        Console.WriteLine();
    }
}
