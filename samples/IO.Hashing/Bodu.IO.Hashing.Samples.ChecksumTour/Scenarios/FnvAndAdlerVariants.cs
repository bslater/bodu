// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvAndAdlerVariants.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.Samples.ChecksumTour.Scenarios;

/// <summary>
/// Demonstrates the width variants within two non-cryptographic families over one fixed input: the
/// FNV-1a hash in its 32- and 64-bit widths, and the Adler checksum in its 32-bit (RFC 1950),
/// SIMD-friendly power-of-two-modulus (<c>Adler32C</c>), and 64-bit widths. Each is the same
/// <c>NonCryptographicHashAlgorithm</c> surface; only the digest width and mixing differ.
/// </summary>
public static class FnvAndAdlerVariants
{
    /// <summary>
    /// Hashes a fixed byte input with each FNV and Adler variant and prints the digest as hex.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- FNV-1a and Adler width variants over one input ---");

        // A fixed in-code input: identical bytes every run, so every digest is deterministic.
        var bytes = System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
        Console.WriteLine($"input: {bytes.Length} bytes");

        // Same NonCryptographicHashAlgorithm surface across both families; only the width and the
        // mixing (FNV multiply-xor vs Adler modular sums) differ between rows.
        foreach (var (name, algorithm) in new (string, System.IO.Hashing.NonCryptographicHashAlgorithm)[]
        {
            ("FNV-1a/32 ", new Fnv1a32()),
            ("FNV-1a/64 ", new Fnv1a64()),
            ("Adler-32  ", new Adler32()),
            ("Adler-32C ", new Adler32C()),
            ("Adler-64  ", new Adler64()),
        })
        {
            algorithm.Append(bytes);
            Console.WriteLine($"  {name}: {Convert.ToHexString(algorithm.GetHashAndReset())}");
        }

        Console.WriteLine("wider digests spread the same input over more state - fewer accidental collisions.");

        Console.WriteLine();
    }
}
