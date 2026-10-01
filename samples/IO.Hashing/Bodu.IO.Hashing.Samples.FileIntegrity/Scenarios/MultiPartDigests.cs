// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiPartDigests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

namespace Bodu.IO.Hashing.Samples.FileIntegrity.Scenarios;

/// <summary>
/// Splits a release file into numbered parts, the way large downloads and backups are delivered, and digests the
/// parts in order with <c>AppendData(Stream)</c> and <c>AppendDataAsync(Stream)</c> so the whole file is verified
/// without ever being reassembled.
/// </summary>
public static class MultiPartDigests
{
    /// <summary>The size of each part, in bytes; the last part holds the remainder.</summary>
    private const int PartSize = 100 * 1024;

    /// <summary>
    /// Splits <c>assets.pak</c> into parts and compares the part-by-part digests with the whole-file digest.
    /// </summary>
    /// <param name="folder">The release folder holding the file.</param>
    /// <returns>A task that completes when the scenario has printed its results.</returns>
    public static async Task RunAsync(ReleaseFolder folder)
    {
        SampleConsole.Scenario(
            "Digesting a file delivered in parts",
            what: "Splits the 256 KiB assets.pak into assets.pak.001 to .003, digests the parts in order on one " +
                  "CRC instance with AppendData(Stream), again with AppendDataAsync(Stream) while printing the running " +
                  "value after each part, and once more with the parts out of order.",
            why: "Split archives, chunked uploads, and rotated logs arrive as several files that together form one " +
                 "artefact. AppendData feeds a stream into the running state without finalizing it, so the parts can " +
                 "be checked against the whole file's published digest one after another, with no temporary copy " +
                 "of the reassembled file.",
            expect: "Both in-order digests equal the whole-file digest (True). GetCurrentHash shows the running " +
                    "value changing after each part without ending the computation. Out of order the digest differs " +
                    "(False), because a CRC depends on byte order, so a checker that finds parts by wildcard must sort " +
                    "them first.");

        var wholePath = folder.GetPath("assets.pak");
        var parts = await SplitAsync(wholePath).ConfigureAwait(false);

        var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
        byte[] whole;
        using (var stream = File.OpenRead(wholePath))
            whole = crc.ComputeHash(stream);

        Console.WriteLine($"  assets.pak, whole file   : {Convert.ToHexString(whole)}");

        // Synchronous: each part's stream is appended to the same running state; the digest is read once at the end.
        foreach (var part in parts)
        {
            using var stream = File.OpenRead(part);
            crc.AppendData(stream);
        }

        var syncDigest = crc.GetHashAndReset();
        Console.WriteLine($"  AppendData, in order     : {Convert.ToHexString(syncDigest)} (== whole: {syncDigest.AsSpan().SequenceEqual(whole)})");

        // Asynchronous, with progress: GetCurrentHash reads the running value without finalizing or resetting it.
        foreach (var part in parts)
        {
            var stream = new FileStream(part, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            await using (stream.ConfigureAwait(false))
                await crc.AppendDataAsync(stream).ConfigureAwait(false);

            Console.WriteLine($"    after {Path.GetFileName(part),-17}: {Convert.ToHexString(crc.GetCurrentHash())} ({new FileInfo(part).Length,6} B)");
        }

        var asyncDigest = crc.GetHashAndReset();
        Console.WriteLine($"  AppendDataAsync, in order: {Convert.ToHexString(asyncDigest)} (== whole: {asyncDigest.AsSpan().SequenceEqual(whole)})");

        // The same parts in the wrong order describe a different byte sequence.
        foreach (var part in new[] { parts[1], parts[0], parts[2] })
        {
            using var stream = File.OpenRead(part);
            crc.AppendData(stream);
        }

        var shuffled = crc.GetHashAndReset();
        Console.WriteLine($"  AppendData, out of order : {Convert.ToHexString(shuffled)} (== whole: {shuffled.AsSpan().SequenceEqual(whole)})");

        Console.WriteLine();
    }

    /// <summary>
    /// Splits a file into numbered parts of <see cref="PartSize" /> bytes beside the original.
    /// </summary>
    /// <param name="path">The file to split.</param>
    /// <returns>The part paths, in order.</returns>
    private static async Task<IReadOnlyList<string>> SplitAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        var parts = new List<string>();
        for (var offset = 0; offset < bytes.Length; offset += PartSize)
        {
            var partPath = $"{path}.{parts.Count + 1:000}";
            await File.WriteAllBytesAsync(partPath, bytes.AsMemory(offset, Math.Min(PartSize, bytes.Length - offset)).ToArray()).ConfigureAwait(false);
            parts.Add(partPath);
        }

        return parts;
    }
}
