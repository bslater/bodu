// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographicHashes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Hashes one fixed message through a spread of the library's unkeyed cryptographic hashes — BLAKE2b,
/// BLAKE3, Tiger, the three Skein sizes, and Whirlpool — each consumed through the standard
/// <see cref="HashAlgorithm.ComputeHash(byte[])" /> surface and printed as lowercase hex.
/// </summary>
public static class CryptographicHashes
{
    // A single fixed message keeps every digest reproducible run to run.
    private static readonly byte[] Message =
        Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <summary>
    /// Computes and prints the digest of the fixed message under each algorithm.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Cryptographic hashes over a fixed message ---");
        Console.WriteLine($"message: \"The quick brown fox jumps over the lazy dog\"");
        Console.WriteLine();

        // Every one of these derives from HashAlgorithm, so a single helper drives them all.
        HashWith("BLAKE2b-512", new Blake2b());
        HashWith("BLAKE3-256", new Blake3());
        HashWith("Tiger/192", new Tiger());
        HashWith("Skein-256", new Skein256());
        HashWith("Skein-512", new Skein512());
        HashWith("Skein-1024", new Skein1024());
        HashWith("Whirlpool", new Whirlpool());

        Console.WriteLine();
    }

    /// <summary>
    /// Hashes the fixed message with the supplied algorithm, disposes it, and prints the labelled digest.
    /// </summary>
    /// <param name="label">A short human-readable label including the output size.</param>
    /// <param name="algorithm">The hash algorithm to drive.</param>
    private static void HashWith(string label, HashAlgorithm algorithm)
    {
        using (algorithm)
        {
            var digest = algorithm.ComputeHash(Message);
            Console.WriteLine($"  {label,-12} ({digest.Length * 8,4} bits) : {Hex.ToHex(digest)}");
        }
    }
}
