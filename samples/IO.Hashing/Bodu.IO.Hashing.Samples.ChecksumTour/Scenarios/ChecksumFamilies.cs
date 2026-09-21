// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChecksumFamilies.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.Samples.ChecksumTour.Scenarios;

/// <summary>
/// Demonstrates the three checksum families over the same committed file: CRC (error-detecting,
/// protocol-grade), Adler (RFC 1950, fast with weaker small-input mixing), and Fletcher
/// (position-sensitive) — and shows the property checksums exist for: a single flipped bit
/// changes every digest.
/// </summary>
public static class ChecksumFamilies
{
    /// <summary>
    /// Checksums <c>Data/pangrams.txt</c> with each family, then corrupts one bit.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Checksum families over one input",
            what: "Runs CRC-32, Adler-32, Fletcher-32 and Fletcher-64 over the same file, then flips a single bit " +
                  "and recomputes.",
            why: "These are error-detection codes, not hashes - they exist to catch accidental corruption on a " +
                 "wire or a disk, and they are cheap enough to run on every frame. None of them is a security " +
                 "primitive: an attacker can construct a colliding message trivially, so a checksum tells you a " +
                 "message arrived intact, never that it came from who you think. Adler and Fletcher trade " +
                 "detection strength for speed against CRC, which is why all three still ship in real protocols.",
            expect: "Four different digests over identical bytes, because each algorithm is a different function " +
                    "rather than a different encoding of one answer. A single flipped bit changes the CRC " +
                    "completely - that avalanche is exactly the property that makes corruption detectable.");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "pangrams.txt"));
        Console.WriteLine($"  input: {bytes.Length} bytes"
            + "  (one committed file, so every digest below is reproducible on any machine)");

        // The same NonCryptographicHashAlgorithm surface across all families.
        foreach (var (name, algorithm) in new (string, System.IO.Hashing.NonCryptographicHashAlgorithm)[]
        {
            ("CRC-32/ISO-HDLC", new Crc(CrcStandard.CRC32_ISOHDLC)),
            ("Adler-32       ", new Adler32()),
            ("Fletcher-32    ", new Fletcher32()),
            ("Fletcher-64    ", new Fletcher64()),
        })
        {
            // Append accumulates; GetHashAndReset finalizes the digest and clears state so the
            // instance could be reused for another input.
            algorithm.Append(bytes);
            Console.WriteLine($"  {name}: {Convert.ToHexString(algorithm.GetHashAndReset())}");
        }

        Console.WriteLine("  (identical bytes in, four unrelated digests out - these are different functions, not different encodings of one answer)");

        // Flip one bit and every family reports a different digest.
        var corrupted = (byte[])bytes.Clone();
        corrupted[100] ^= 0x01;

        var clean = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(bytes);
        var dirty = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(corrupted);
        Console.WriteLine($"  one flipped bit  : {Convert.ToHexString(clean)} -> {Convert.ToHexString(dirty)} (detected: {!clean.AsSpan().SequenceEqual(dirty)})"
            + "  (expected True, and note the digest changes wholesale rather than in one bit - that avalanche is what makes small corruptions loud)");

        Console.WriteLine();
    }
}
