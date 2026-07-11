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
        Console.WriteLine("--- Checksum families over Data/pangrams.txt ---");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "pangrams.txt"));
        Console.WriteLine($"input: {bytes.Length} bytes");

        // The same NonCryptographicHashAlgorithm surface across all families.
        foreach (var (name, algorithm) in new (string, System.IO.Hashing.NonCryptographicHashAlgorithm)[]
        {
            ("CRC-32/ISO-HDLC", new Crc(CrcStandard.CRC32_ISOHDLC)),
            ("Adler-32       ", new Adler32()),
            ("Fletcher-32    ", new Fletcher32()),
            ("Fletcher-64    ", new Fletcher64()),
        })
        {
            algorithm.Append(bytes);
            Console.WriteLine($"  {name}: {Convert.ToHexString(algorithm.GetHashAndReset())}");
        }

        // Flip one bit and every family reports a different digest.
        var corrupted = (byte[])bytes.Clone();
        corrupted[100] ^= 0x01;

        var clean = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(bytes);
        var dirty = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(corrupted);
        Console.WriteLine($"one flipped bit  : {Convert.ToHexString(clean)} -> {Convert.ToHexString(dirty)} (detected: {!clean.AsSpan().SequenceEqual(dirty)})");

        Console.WriteLine();
    }
}
