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
        Console.WriteLine("--- Base36Encoding: encode / decode / round trip ---");

        var base36 = new Base36Encoding();
        var payload = "Bodu!"u8.ToArray();

        var encoded = base36.Encode(payload);
        var decoded = base36.Decode(encoded);

        Console.WriteLine($"payload      : 5 bytes 'Bodu!'");
        Console.WriteLine($"encoded      : {encoded}");
        Console.WriteLine($"round trip   : {decoded.AsSpan().SequenceEqual(payload)}");

        // Leading zero bytes survive as leading '0' characters (the Base58 '1' rule, for '0').
        var withZeros = new byte[] { 0x00, 0x00, 0xFF };
        Console.WriteLine($"[00 00 FF]   : '{base36.Encode(withZeros)}' -> {base36.Decode(base36.Encode(withZeros)).Length} bytes restored");

        // Validation and the Try pattern reject bad input without exceptions.
        Console.WriteLine($"IsValid('9Z'): {base36.IsValid("9Z")}, IsValid('a-b'): {base36.IsValid("a-b")}");

        Span<byte> buffer = stackalloc byte[8];
        var ok = base36.TryDecode("HELLO42", buffer, out var written);
        Console.WriteLine($"TryDecode    : {ok}, {written} bytes into a stack buffer");

        try
        {
            base36.Decode("not base36!");
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Decode throws: {ex.Message}");
        }

        Console.WriteLine();
    }
}
