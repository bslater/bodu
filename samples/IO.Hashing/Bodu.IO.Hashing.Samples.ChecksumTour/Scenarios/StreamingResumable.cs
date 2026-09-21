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
        SampleConsole.Scenario(
            "Streaming and resumable hashing",
            what: "Computes one digest four ways: a one-shot call, chunked Append calls, through a HashingStream, " +
                  "and by saving state after day one and resuming on day two.",
            why: "Feeding a hash in pieces has to give the same answer as feeding it whole, or the API is unusable " +
                 "for anything that does not fit in memory. Resumability goes further and is rarer: saving the " +
                 "internal state lets a digest span a process restart, so an append-only log can be checksummed " +
                 "incrementally forever instead of re-reading it from the beginning each night. HashingStream is " +
                 "the same capability shaped as a pass-through, so bytes can be hashed while they are being " +
                 "copied rather than in a separate pass.",
            expect: "The first three routes produce byte-identical digests. The resumed digest equals a full " +
                    "replay over both days' data, which is the property that makes stored state trustworthy - not " +
                    "merely that it produced some stable value.");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "pangrams.txt");
        var bytes = File.ReadAllBytes(path);

        // One-shot reference digest.
        var oneShot = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(bytes);
        Console.WriteLine($"  one-shot        : {Convert.ToHexString(oneShot)}"
            + "  (the reference digest the next three routes must match)");

        // Chunked Append over arbitrary split points produces the same digest.
        var chunked = new Crc(CrcStandard.CRC32_ISOHDLC);
        chunked.Append(bytes.AsSpan(0, 37));
        chunked.Append(bytes.AsSpan(37, 100));
        chunked.Append(bytes.AsSpan(137));
        Console.WriteLine($"  chunked Append  : {Convert.ToHexString(chunked.GetHashAndReset())}"
            + "  (expected to equal the one-shot digest - the split points are arbitrary and must not be observable)");

        // HashingStream: checksum as a side effect of normal stream reads.
        using var hashing = new HashingStream(File.OpenRead(path), new Crc(CrcStandard.CRC32_ISOHDLC));
        hashing.CopyTo(Stream.Null);
        Console.WriteLine($"  HashingStream   : {Convert.ToHexString(hashing.Algorithm.GetCurrentHash())}"
            + "  (same digest again - the bytes were hashed while being copied, not in a second pass over the file)");

        // Resumable: extend yesterday's stored digest with today's appended records -
        // no need to re-read the original log.
        var day1 = "2026-07-10T09:00 deposit 100\n"u8.ToArray();
        var day2 = "2026-07-11T09:00 deposit 250\n"u8.ToArray();

        var storedDigest = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(day1);
        IResumableHashAlgorithm resumable = new Crc(CrcStandard.CRC32_ISOHDLC);
        var resumed = resumable.ComputeHashFrom(storedDigest, day2);

        var wholeLog = day1.Concat(day2).ToArray();
        var recomputed = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(wholeLog);
        Console.WriteLine($"  resumable       : stored+day2 {Convert.ToHexString(resumed)} == full replay {Convert.ToHexString(recomputed)} -> {resumed.AsSpan().SequenceEqual(recomputed)}"
            + "  (expected True - day one's bytes were never re-read, so the stored digest alone carried the history forward)");

        Console.WriteLine();
    }
}
