// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingRegistry.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Samples.Text.Encoding.EncodingTour.Scenarios;

/// <summary>
/// Demonstrates <see cref="BinaryEncodings" />: the name-addressable registry over the whole
/// catalogue. When the encoding is chosen at runtime — a config value, a protocol header, a CLI
/// flag — <c>Get(name)</c> returns an <see cref="IBinaryEncoding" /> and the consuming code
/// stays codec-agnostic.
/// </summary>
public static class EncodingRegistry
{
    /// <summary>
    /// Drives several encodings through the shared interface, selected by name.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- BinaryEncodings registry: choose the codec at runtime ---");

        var payload = "Bodu!"u8.ToArray();

        // The consuming code never mentions a concrete codec.
        foreach (var name in new[] { "base16", "base32-crockford", "base58", "base64-urlsafe", "z85" })
        {
            IBinaryEncoding encoding = BinaryEncodings.Get(name);
            var text = name == "z85"
                ? encoding.Encode([0xDE, 0xAD, 0xBE, 0xEF]) // Z85 needs 4-byte alignment
                : encoding.Encode(payload);

            Console.WriteLine($"{encoding.Name,-16} -> '{text}'  ({encoding.Description})");
        }

        // IsValid + TryDecode make the interface safe for untrusted input.
        var base58 = BinaryEncodings.Get("base58");
        Console.WriteLine();
        Console.WriteLine($"base58.IsValid(\"Bodu58ok\") : {base58.IsValid("Bodu58ok")}");
        Console.WriteLine($"base58.IsValid(\"0OIl\")     : {base58.IsValid("0OIl")} (alphabet excludes 0, O, I, l)");

        Console.WriteLine();
    }
}
