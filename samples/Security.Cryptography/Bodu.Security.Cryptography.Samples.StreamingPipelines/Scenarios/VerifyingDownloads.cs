// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VerifyingDownloads.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.StreamingPipelines.Scenarios;

/// <summary>
/// Checks a downloaded artefact against the digests its publisher lists, the way a package manager or an updater
/// does: <c>VerifyHashAsync</c> over the content stream for BLAKE2b and the BCL's SHA-256, <c>AppendDataAsync</c> to
/// digest a download that arrives in parts, and <c>TryVerifyHash</c> / <c>TryVerifyHashAsync</c> for manifest
/// entries that cannot be trusted to be well formed.
/// </summary>
public static class VerifyingDownloads
{
    /// <summary>The length of the artefact's header part, in bytes.</summary>
    private const int HeaderLength = 512;

    /// <summary>
    /// Publishes a manifest for the payload, then verifies intact, multi-part, tampered, and malformed inputs.
    /// </summary>
    /// <returns>A task that completes when the scenario has printed its results.</returns>
    public static async Task RunAsync()
    {
        SampleConsole.Scenario(
            "Verifying a download against a published manifest",
            what: "Publishes BLAKE2b-256 and SHA-256 hex digests for the 200 KiB artefact, verifies the artefact stream against each with VerifyHashAsync, digests it again as two separately delivered parts with AppendDataAsync, verifies a copy with one flipped byte, and then feeds TryVerifyHash and TryVerifyHashAsync a missing digest, a malformed one, and a stream that has already been closed.",
            why: "A digest only protects a download if the comparison is exact and leaks nothing: VerifyHashAsync hashes the stream as it reads and compares in constant time, so neither the content nor a timing difference tells an attacker how close a forged digest came. The manifest is input too. A missing or garbled entry should be reported as unverifiable, and the Try forms return false for it where the plain forms throw.",
            expect: "Both digests verify True and the two-part digest equals the published one. The tampered copy prints False under both algorithms. Every Try call prints False without throwing, while VerifyHash given a null digest throws ArgumentNullException.");

        var artefact = SamplePayload.Create();

        // The publisher's side: hex digests, as a release page or a checksum file would list them.
        string blake2Hex;
        using (var blake2 = new Blake2b(256))
            blake2Hex = Hex.ToHex(blake2.ComputeHash(artefact));

        var sha256Hex = Hex.ToHex(SHA256.HashData(artefact));

        Console.WriteLine($"  manifest: BLAKE2b-256 {blake2Hex[..16]}...  SHA-256 {sha256Hex[..16]}...");
        Console.WriteLine();

        // The consumer's side. Each check reads the content stream once and finalizes the hash.
        Console.WriteLine($"  VerifyHashAsync, BLAKE2b-256 : {await VerifyAsync(new Blake2b(256), artefact, blake2Hex).ConfigureAwait(false)}");
        Console.WriteLine($"  VerifyHashAsync, SHA-256     : {await VerifyAsync(SHA256.Create(), artefact, sha256Hex).ConfigureAwait(false)}");

        // A download that arrives in parts: AppendDataAsync feeds each part without finalizing, so the parts never
        // need to be concatenated in memory. TransformFinalBlock closes the digest once the last part is in.
        using (var incremental = new Blake2b(256))
        {
            using var header = new MemoryStream(artefact, 0, HeaderLength);
            using var body = new MemoryStream(artefact, HeaderLength, artefact.Length - HeaderLength);
            await incremental.AppendDataAsync(header).ConfigureAwait(false);
            await incremental.AppendDataAsync(body, bufferSize: 64 * 1024).ConfigureAwait(false);
            incremental.TransformFinalBlock([], 0, 0);
            Console.WriteLine($"  AppendDataAsync, two parts   : {Hex.ToHex(incremental.Hash!) == blake2Hex}");
        }

        // One flipped byte anywhere in the content changes the digest completely.
        var tampered = (byte[])artefact.Clone();
        tampered[123_456] ^= 0x01;
        Console.WriteLine($"  tampered copy, BLAKE2b-256   : {await VerifyAsync(new Blake2b(256), tampered, blake2Hex).ConfigureAwait(false)}");
        Console.WriteLine($"  tampered copy, SHA-256       : {await VerifyAsync(SHA256.Create(), tampered, sha256Hex).ConfigureAwait(false)}");
        Console.WriteLine();

        // Manifest entries that cannot be trusted. The Try forms report every failure as false.
        using var checker = new Blake2b(256);
        string? missingHex = null;
        var closedStream = new MemoryStream(artefact);
        closedStream.Dispose();

        Console.WriteLine($"  TryVerifyHash, no digest listed      : {checker.TryVerifyHash(artefact, missingHex!)}");
        Console.WriteLine($"  TryVerifyHash, malformed digest      : {checker.TryVerifyHash(artefact, "not-a-hex-digest")}");
        Console.WriteLine($"  TryVerifyHashAsync, closed stream    : {await checker.TryVerifyHashAsync(closedStream, blake2Hex).ConfigureAwait(false)}");

        // The throwing form treats a missing digest as a programming error rather than a verification result.
        string thrown;
        try
        {
            checker.VerifyHash(artefact, missingHex!);
            thrown = "no exception";
        }
        catch (ArgumentNullException ex)
        {
            thrown = $"{ex.GetType().Name} ({ex.ParamName})";
        }

        Console.WriteLine($"  VerifyHash, no digest listed         : {thrown}");

        Console.WriteLine();
    }

    /// <summary>
    /// Verifies <paramref name="content" />, read as a stream, against a published hex digest.
    /// </summary>
    /// <param name="algorithm">The hash algorithm; disposed when the check completes.</param>
    /// <param name="content">The downloaded bytes.</param>
    /// <param name="expectedHex">The published digest, as hexadecimal text.</param>
    /// <returns><see langword="true" /> when the digest matches; otherwise <see langword="false" />.</returns>
    private static async Task<bool> VerifyAsync(HashAlgorithm algorithm, byte[] content, string expectedHex)
    {
        using (algorithm)
        {
            using var stream = new MemoryStream(content);
            return await algorithm.VerifyHashAsync(stream, expectedHex).ConfigureAwait(false);
        }
    }
}
