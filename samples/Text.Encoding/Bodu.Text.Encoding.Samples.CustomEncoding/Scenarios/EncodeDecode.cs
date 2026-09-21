// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodeDecode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Samples.Text.Encoding.CustomEncoding.Scenarios;

/// <summary>
/// Demonstrates the custom <see cref="Base36Encoding" /> through its own surface: encode,
/// decode, round trips, the leading-zero contract, validation, and the Try pattern for
/// untrusted input.
/// </summary>
public static class EncodeDecode
{
    /// <summary>
    /// Exercises the Base36 codec over fixed payloads.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Base36Encoding - the custom codec through its own surface",
            what: "Encodes and decodes a payload, checks a value whose leading bytes are zero, validates two "
                + "strings, decodes into a stack-allocated buffer with the Try pattern, and shows Decode "
                + "throwing on input outside the alphabet.",
            why: "Base36 is a big-integer encoding rather than a bit-packing one: the payload is treated as a "
                + "single number and divided repeatedly by 36. That has one consequence worth designing around - "
                + "a number has no way to remember how many leading zeros preceded it, so a naive implementation "
                + "silently drops them and decodes to fewer bytes than it encoded. Base58 solves this by emitting "
                + "one alphabet character per leading zero byte, and this codec adopts the same rule with '0'. "
                + "The Try pattern matters for the same class of reason: text that arrives from outside is not "
                + "known to be well-formed, and a failed decode should be a return value rather than an exception "
                + "on a hot path.",
            expect: "The round trip returns the original bytes, and the zero-prefixed value restores all three "
                + "bytes rather than one - that is the leading-zero rule working. Validation accepts alphabet "
                + "characters and rejects punctuation, TryDecode reports success and the byte count without "
                + "allocating, and Decode throws for the same input TryDecode would have refused.");

        var base36 = new Base36Encoding();
        var payload = "Bodu!"u8.ToArray();

        var encoded = base36.Encode(payload);
        var decoded = base36.Decode(encoded);

        Console.WriteLine($"  payload      : 5 bytes 'Bodu!'");
        Console.WriteLine($"  encoded      : {encoded}");
        Console.WriteLine($"  round trip   : {decoded.AsSpan().SequenceEqual(payload)}"
            + "  (expected True - the basic contract every codec owes: decode(encode(x)) == x)");

        // Leading zero bytes survive as leading '0' characters (the Base58 '1' rule, for '0').
        var withZeros = new byte[] { 0x00, 0x00, 0xFF };
        Console.WriteLine($"  [00 00 FF]   : '{base36.Encode(withZeros)}' -> {base36.Decode(base36.Encode(withZeros)).Length} bytes restored"
            + "  (expected 3, not 1 - the two leading '0' characters carry the zero bytes a big-integer encoding would otherwise lose)");

        // Validation and the Try pattern reject bad input without exceptions.
        Console.WriteLine($"  IsValid('9Z'): {base36.IsValid("9Z")}, IsValid('a-b'): {base36.IsValid("a-b")}"
            + "  (expected True then False - the hyphen is outside the 36-character alphabet)");

        Span<byte> buffer = stackalloc byte[8];
        var ok = base36.TryDecode("HELLO42", buffer, out var written);
        Console.WriteLine($"  TryDecode    : {ok}, {written} bytes into a stack buffer"
            + "  (no allocation and no exception - the shape to use for input arriving from outside)");

        try
        {
            base36.Decode("not base36!");
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"  Decode throws: {ex.Message}"
                + "  (the same input TryDecode would have refused, surfaced as an exception by the throwing overload)");
        }

        Console.WriteLine();
    }
}
