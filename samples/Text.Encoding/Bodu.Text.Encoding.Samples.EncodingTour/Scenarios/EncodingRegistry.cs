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
        SampleConsole.Scenario(
            "BinaryEncodings registry - choosing the codec at runtime",
            what: "Resolves five encodings by name from the registry, encodes through the shared IBinaryEncoding "
                + "interface without naming a concrete type, and then validates two strings against the Base58 "
                + "alphabet.",
            why: "The codec is often not a compile-time decision: it arrives in a config file, a protocol header "
                + "or a command-line flag. Without a registry that turns into a switch statement that has to be "
                + "extended every time the catalogue grows, and which silently does the wrong thing for a name it "
                + "does not recognise. Resolving by name keeps the consuming code closed to that change. IsValid "
                + "matters for the same reason: input chosen at runtime is input you did not write, so the "
                + "interface has to offer a way to ask before committing.",
            expect: "Five encodings driven through one interface, each reporting its own name and description. "
                + "The validity checks show the alphabet doing real work - Base58 excludes 0, O, I and l "
                + "precisely because they are the characters a human transcribing a value gets wrong, so a string "
                + "containing them is rejected rather than decoded into something plausible.");

        var payload = "Bodu!"u8.ToArray();

        // The consuming code never mentions a concrete codec.
        foreach (var name in new[] { "base16", "base32-crockford", "base58", "base64-urlsafe", "z85" })
        {
            IBinaryEncoding encoding = BinaryEncodings.Get(name);
            var text = name == "z85"
                ? encoding.Encode([0xDE, 0xAD, 0xBE, 0xEF]) // Z85 needs 4-byte alignment
                : encoding.Encode(payload);

            Console.WriteLine($"  {encoding.Name,-16} -> '{text}'  ({encoding.Description})");
        }

        Console.WriteLine("  (the loop above never names a concrete codec - adding an encoding to the catalogue needs no change here)");

        // IsValid + TryDecode make the interface safe for untrusted input.
        var base58 = BinaryEncodings.Get("base58");
        Console.WriteLine();
        Console.WriteLine($"  base58.IsValid(\"Bodu58ok\") : {base58.IsValid("Bodu58ok")}"
            + "  (expected True - every character is in the Base58 alphabet)");
        Console.WriteLine($"  base58.IsValid(\"0OIl\")     : {base58.IsValid("0OIl")}"
            + "  (expected False - these four are exactly the characters Base58 omits, because humans confuse them with O/0 and I/1/l)");

        Console.WriteLine();
    }
}
