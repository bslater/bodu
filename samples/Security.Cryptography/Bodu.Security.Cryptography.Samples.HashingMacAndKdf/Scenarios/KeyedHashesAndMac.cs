// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedHashesAndMac.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Produces authentication tags with the library's keyed hashes and one-time MAC — SipHash-64,
/// SipHash-128, keyed BLAKE2b, and Poly1305 — each over a fixed message under a fixed key, so the printed
/// tags are reproducible.
/// </summary>
public static class KeyedHashesAndMac
{
    private static readonly byte[] Message = Encoding.ASCII.GetBytes("authenticate me");

    // SipHash uses a 128-bit (16-byte) key; this is the RFC/reference key 00 01 02 … 0f.
    private static readonly byte[] SipKey =
        [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f];

    // A fixed 32-byte key for the BLAKE2b keyed-MAC mode.
    private static readonly byte[] Blake2Key =
        Enumerable.Range(0, 32).Select(i => (byte)(0x40 + i)).ToArray();

    // Poly1305 takes a 256-bit (32-byte) one-time key: the r half is clamped, the s half is the addend.
    private static readonly byte[] PolyKey =
        Enumerable.Range(0, 32).Select(i => (byte)(0x80 + i)).ToArray();

    /// <summary>
    /// Computes and prints the tag for the fixed message under each keyed construction.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Keyed hashes and a one-time MAC (fixed keys) ---");
        Console.WriteLine($"message: \"authenticate me\"");
        Console.WriteLine();

        // SipHash is a keyed pseudo-random function; the key is set through the KeyedHashAlgorithm surface.
        using (var sip64 = new SipHash64 { Key = SipKey })
            Console.WriteLine($"  SipHash-64      : {Hex.ToHex(sip64.ComputeHash(Message))}");

        using (var sip128 = new SipHash128 { Key = SipKey })
            Console.WriteLine($"  SipHash-128     : {Hex.ToHex(sip128.ComputeHash(Message))}");

        // Supplying a non-empty Key switches BLAKE2b into its keyed-MAC mode.
        using (var blake2Mac = new Blake2b(256) { Key = Blake2Key })
            Console.WriteLine($"  BLAKE2b-MAC-256 : {Hex.ToHex(blake2Mac.ComputeHash(Message))}");

        // Poly1305 is a one-time authenticator: a fresh instance (and key) per message.
        using (var poly = new Poly1305 { Key = PolyKey })
            Console.WriteLine($"  Poly1305        : {Hex.ToHex(poly.ComputeHash(Message))}");

        Console.WriteLine();
    }
}
