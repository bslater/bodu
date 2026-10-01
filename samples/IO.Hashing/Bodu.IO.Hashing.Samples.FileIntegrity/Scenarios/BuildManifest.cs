// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BuildManifest.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using Bodu.IO.Hashing.Checksums;
using Bodu.IO.Hashing.Extensions;

namespace Bodu.IO.Hashing.Samples.FileIntegrity.Scenarios;

/// <summary>
/// Builds a checksum manifest for the release folder with the stream extensions <c>ComputeHash(Stream)</c> and
/// <c>ComputeHashAsync(Stream)</c>, then shows the same extensions running over the BCL's own
/// <see cref="Crc32" /> and <see cref="XxHash64" />.
/// </summary>
public static class BuildManifest
{
    /// <summary>
    /// Hashes every release file from disk and returns the manifest lines.
    /// </summary>
    /// <param name="folder">The release folder to describe.</param>
    /// <returns>The manifest, one <c>hex  name</c> line per file.</returns>
    public static async Task<IReadOnlyList<string>> RunAsync(ReleaseFolder folder)
    {
        SampleConsole.Scenario(
            "Building a checksum manifest from files",
            what: "Computes the CRC-32 of each file in a three-file release folder straight from a FileStream, once " +
                  "with ComputeHash(Stream) and once with ComputeHashAsync(Stream), and writes the results as a " +
                  "manifest of digest and file-name pairs. Then hashes one file through the same extensions with System.IO.Hashing's " +
                  "Crc32 and XxHash64.",
            why: "NonCryptographicHashAlgorithm itself only hashes spans, so on its own every file would have to be " +
                 "read into memory first. The extensions stream the file through a pooled buffer instead, and because " +
                 "they are written against the System.IO.Hashing base class they serve the framework's hashes as " +
                 "well as Bodu's. Agreement between Bodu's CRC-32/ISO-HDLC and the framework's Crc32 shows the two " +
                 "are interchangeable in a manifest.",
            expect: "Three manifest lines, each with 'sync == async: True'. Bodu's CRC-32 of assets.pak equals the " +
                    "BCL Crc32 digest (True), and XxHash64 prints a 64-bit fingerprint of the same file.");

        var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
        var manifest = new List<string>();

        foreach (var name in ReleaseFolder.FileNames)
        {
            var path = folder.GetPath(name);

            // ComputeHash(Stream) resets the algorithm, reads the stream to its end, and returns the digest.
            byte[] digest;
            using (var stream = File.OpenRead(path))
                digest = crc.ComputeHash(stream);

            // The awaitable form, over a FileStream opened for asynchronous I/O.
            byte[] digestAsync;
            var asyncStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            await using (asyncStream.ConfigureAwait(false))
                digestAsync = await crc.ComputeHashAsync(asyncStream).ConfigureAwait(false);

            var line = $"{Convert.ToHexString(digest)}  {name}";
            manifest.Add(line);
            Console.WriteLine($"  {line,-24}  ({new FileInfo(path).Length,6} B, sync == async: {digest.AsSpan().SequenceEqual(digestAsync)})");
        }

        Console.WriteLine();

        // The extensions extend System.IO.Hashing.NonCryptographicHashAlgorithm, so the framework's hashes get them too.
        var assetsPath = folder.GetPath("assets.pak");
        byte[] bclCrc;
        using (var stream = File.OpenRead(assetsPath))
            bclCrc = new Crc32().ComputeHash(stream);

        byte[] fingerprint;
        using (var stream = File.OpenRead(assetsPath))
            fingerprint = new XxHash64().ComputeHash(stream);

        var manifestDigest = Convert.FromHexString(manifest[^1][..8]);
        Console.WriteLine($"  System.IO.Hashing.Crc32 of assets.pak    : {Convert.ToHexString(bclCrc)} (== manifest entry: {bclCrc.AsSpan().SequenceEqual(manifestDigest)})");
        Console.WriteLine($"  System.IO.Hashing.XxHash64 of assets.pak : {Convert.ToHexString(fingerprint)}");

        Console.WriteLine();
        return manifest;
    }
}
