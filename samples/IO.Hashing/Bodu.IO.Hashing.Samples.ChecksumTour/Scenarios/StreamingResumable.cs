// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingResumable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing;
using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

namespace Bodu.IO.Hashing.Samples.ChecksumTour.Scenarios;

/// <summary>
/// Demonstrates the incremental surfaces: chunked <c>Append</c> equals the one-shot digest, a
/// <see cref="HashingStream" /> checksums bytes as they flow through ordinary stream I/O, and
/// <see cref="IResumableHashAlgorithm" /> extends a stored digest with new data without
/// replaying the original input — the append-only log pattern.
/// </summary>
public static class StreamingResumable
{
    /// <summary>
    /// Runs the chunked, stream-wrapped, and resumable variants over the committed file.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Streaming and resumable hashing ---");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "pangrams.txt");
        var bytes = File.ReadAllBytes(path);

        // One-shot reference digest.
        var oneShot = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(bytes);
        Console.WriteLine($"one-shot        : {Convert.ToHexString(oneShot)}");

        // Chunked Append over arbitrary split points produces the same digest.
        var chunked = new Crc(CrcStandard.CRC32_ISOHDLC);
        chunked.Append(bytes.AsSpan(0, 37));
        chunked.Append(bytes.AsSpan(37, 100));
        chunked.Append(bytes.AsSpan(137));
        Console.WriteLine($"chunked Append  : {Convert.ToHexString(chunked.GetHashAndReset())}");

        // HashingStream: checksum as a side effect of normal stream reads.
        using var hashing = new HashingStream(File.OpenRead(path), new Crc(CrcStandard.CRC32_ISOHDLC));
        hashing.CopyTo(Stream.Null);
        Console.WriteLine($"HashingStream   : {Convert.ToHexString(hashing.Algorithm.GetCurrentHash())}");

        // Resumable: extend yesterday's stored digest with today's appended records -
        // no need to re-read the original log.
        var day1 = "2026-07-10T09:00 deposit 100\n"u8.ToArray();
        var day2 = "2026-07-11T09:00 deposit 250\n"u8.ToArray();

        var storedDigest = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(day1);
        IResumableHashAlgorithm resumable = new Crc(CrcStandard.CRC32_ISOHDLC);
        var resumed = resumable.ComputeHashFrom(storedDigest, day2);

        var wholeLog = day1.Concat(day2).ToArray();
        var recomputed = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(wholeLog);
        Console.WriteLine($"resumable       : stored+day2 {Convert.ToHexString(resumed)} == full replay {Convert.ToHexString(recomputed)} -> {resumed.AsSpan().SequenceEqual(recomputed)}");

        Console.WriteLine();
    }
}
