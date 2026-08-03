// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendableOutput.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Demonstrates extendable-output functions (XOFs), which squeeze an arbitrary number of output bytes from
/// a fixed input. Shows <see cref="Shake" /> at two lengths and <see cref="AsconXof128" /> squeezing, and
/// confirms that a longer XOF output is a prefix-extension of a shorter one.
/// </summary>
public static class ExtendableOutput
{
    private static readonly byte[] Message = Encoding.ASCII.GetBytes("extend me to any length");

    /// <summary>
    /// Produces XOF output at several lengths and prints each as lowercase hex.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Extendable-output functions (XOFs) ---");
        Console.WriteLine($"message: \"extend me to any length\"");
        Console.WriteLine();

        // SHAKE128 is a HashAlgorithm whose output length is chosen at construction (in bits).
        using (var shake16 = new Shake(outputBits: 16 * 8, securityLevel: 128)) // securityLevel selects the SHAKE variant.
            Console.WriteLine($"  SHAKE128 (16 bytes)  : {Hex.ToHex(shake16.ComputeHash(Message))}");

        using (var shake32 = new Shake(outputBits: 32 * 8, securityLevel: 128))
            Console.WriteLine($"  SHAKE128 (32 bytes)  : {Hex.ToHex(shake32.ComputeHash(Message))}");

        // Ascon-XOF128 absorbs input, then squeezes any number of bytes on demand.
        using (var ascon = new AsconXof128())
        {
            ascon.Absorb(Message);
            var out16 = new byte[16];
            var out48 = new byte[48];
            ascon.Squeeze(out16);

            // Re-initialise before the second, longer squeeze over the same input.
            ascon.Initialize();
            ascon.Absorb(Message);
            ascon.Squeeze(out48);

            Console.WriteLine($"  Ascon-XOF128 (16 B)  : {Hex.ToHex(out16)}");
            Console.WriteLine($"  Ascon-XOF128 (48 B)  : {Hex.ToHex(out48)}");

            // The XOF stream is a prefix: the first 16 bytes of the 48-byte squeeze equal the 16-byte squeeze.
            var prefixMatches = out48.AsSpan(0, 16).SequenceEqual(out16);
            Console.WriteLine($"  48-byte output extends 16-byte output? {prefixMatches}");
        }

        Console.WriteLine();
    }
}
