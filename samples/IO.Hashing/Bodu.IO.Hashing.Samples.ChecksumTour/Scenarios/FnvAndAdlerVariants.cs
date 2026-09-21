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
        SampleConsole.Scenario(
            "FNV-1a and Adler width variants over one input",
            what: "Hashes the same input with FNV-1a at 32 and 64 bits, and with Adler-32, Adler-32C and Adler-64.",
            why: "Width is a collision-probability decision. By the birthday bound a 32-bit digest reaches a 50% " +
                 "chance of some collision at roughly 77,000 items - fine for a hash-table bucket, not fine as a " +
                 "content identifier for a large corpus. Moving to 64 bits pushes that to billions. Adler-32C is " +
                 "the worked example of why the variant matters: same width, different parameters, and the " +
                 "digests differ - so two systems must agree on the exact variant, not just the family and size.",
            expect: "Five different digests from one input. Adler-32 and Adler-32C differ despite sharing a width, " +
                    "and the 64-bit forms visibly carry the 32-bit halves in their structure.");

        // A fixed in-code input: identical bytes every run, so every digest is deterministic.
        var bytes = System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
        Console.WriteLine($"  input: {bytes.Length} bytes"
            + "  (a fixed in-code string, so the digests below are stable across runs and machines)");

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

        Console.WriteLine("  (Adler-32 and Adler-32C share a width yet disagree - the variant is part of the contract, not just the family and size)");

        Console.WriteLine("  wider digests spread the same input over more state - fewer accidental collisions.");

        Console.WriteLine();
    }
}
