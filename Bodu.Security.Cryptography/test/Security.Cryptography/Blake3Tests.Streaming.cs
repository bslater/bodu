// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.Streaming.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Self-consistency tests that exercise BLAKE3's chunk-stack and tree-merge logic without depending on an
/// external reference implementation. Three independent code paths — one-shot, byte-at-a-time, and a
/// pseudo-random chunking pattern — must produce identical 32-byte digests for the same input.
/// </summary>
/// <remarks>
/// These cases supplement the official <c>test_vectors.json</c> known-answer tests by catching tree-merge
/// bugs that surface only at irregular chunk boundaries. They are categorised as <c>Regression</c> so the
/// default BVT tier stays fast.
/// </remarks>
public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that hashing a fixed input via one <see cref="HashAlgorithm.ComputeHash(byte[])" /> call,
    /// a byte-by-byte <see cref="HashAlgorithm.TransformBlock" /> stream, and a pseudo-random chunking
    /// pattern all yield the same digest, across input lengths that span single-chunk, multi-chunk,
    /// odd-boundary, and multi-level tree-merge scenarios.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(63)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(1023)]
    [DataRow(1024)]
    [DataRow(1025)]
    [DataRow(2047)]
    [DataRow(2048)]
    [DataRow(2049)]
    [DataRow(3072)]
    [DataRow(4095)]
    [DataRow(4096)]
    [DataRow(4097)]
    [DataRow(8193)]
    [DataRow(16384)]
    [DataRow(16385)]
    [DataRow(65537)]
    public void ChunkingPattern_ShouldNotAffectDigest_AcrossChunkAndTreeBoundaries(int inputLength)
    {
        byte[] input = BuildDeterministicInput(inputLength);

        byte[] oneShotHash;
        using (var hasher = new Blake3())
            oneShotHash = hasher.ComputeHash(input);

        byte[] byteAtATimeHash = HashByteByByte(input);
        byte[] randomChunkHash = HashWithPseudoRandomChunks(input, seed: 1337);

        CollectionAssert.AreEqual(oneShotHash, byteAtATimeHash,
            $"One-shot vs byte-at-a-time digests diverged for input length {inputLength}.");
        CollectionAssert.AreEqual(oneShotHash, randomChunkHash,
            $"One-shot vs random-chunking digests diverged for input length {inputLength}.");
    }

    private static byte[] BuildDeterministicInput(int length)
    {
        byte[] buffer = new byte[length];
        // Same generator pattern as the official BLAKE3 test_vectors.json corpus would expose, but kept
        // local to this test so the assertion is purely an internal consistency check.
        for (int i = 0; i < length; i++)
            buffer[i] = (byte)((i * 251) & 0xFF);
        return buffer;
    }

    private static byte[] HashByteByByte(byte[] input)
    {
        using var hasher = new Blake3();
        byte[] one = new byte[1];
        for (int i = 0; i < input.Length; i++)
        {
            one[0] = input[i];
            hasher.TransformBlock(one, 0, 1, null, 0);
        }
        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hasher.Hash!;
    }

    private static byte[] HashWithPseudoRandomChunks(byte[] input, int seed)
    {
        var rng = new Random(seed);
        using var hasher = new Blake3();
        int offset = 0;
        while (offset < input.Length)
        {
            // 0–127 byte chunks straddle the 64-byte block and the 1024-byte chunk boundary at irregular
            // intervals, which is what the tree-merge logic must tolerate.
            int chunk = Math.Min(rng.Next(0, 128), input.Length - offset);
            if (chunk == 0) continue;
            hasher.TransformBlock(input, offset, chunk, null, 0);
            offset += chunk;
        }
        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hasher.Hash!;
    }
}
