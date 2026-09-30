// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Self-consistency tests that exercise BLAKE3's chunk-stack and tree-merge logic without depending on an
/// external reference implementation. Three independent code paths - one-shot, byte-at-a-time, and a
/// pseudo-random chunking pattern - must produce identical 32-byte digests for the same input.
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

    /// <summary>
    /// Verifies that a large write that starts part-way into a chunk - after a first write of the given length - hashes
    /// as a stream of 100-byte writes does: the large write first completes the open chunk block by block, then hashes
    /// whole subtrees, and leaves its tail to be finished block by block.
    /// </summary>
    /// <param name="firstWrite">The length of the write before the large one.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(1)]
    [DataRow(63)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(1000)]
    [DataRow(1023)]
    [DataRow(1024)]
    [DataRow(1025)]
    [DataRow(2047)]
    [DataRow(4096)]
    public void ChunkingPattern_WhenLargeWriteStartsInsideAChunk_ShouldMatchSmallWrites(int firstWrite)
    {
        byte[] input = BuildDeterministicInput(40 * 1024 + 7);

        using var hasher = new Blake3();
        hasher.TransformBlock(input, 0, firstWrite, null, 0);
        hasher.TransformBlock(input, firstWrite, input.Length - firstWrite - 3, null, 0);
        hasher.TransformFinalBlock(input, input.Length - 3, 3);

        CollectionAssert.AreEqual(HashInFixedBlocks(input, 100), hasher.Hash, $"first write {firstWrite}");
    }

    /// <summary>
    /// Verifies that one-shot hashing, which takes whole subtrees at once, matches a stream of 100-byte writes, which
    /// never does, for inputs one byte either side of every power-of-two number of chunks up to 512 and of the 64-chunk
    /// batch multiples beyond it.
    /// </summary>
    /// <param name="chunks">The number of whole chunks.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(32)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(128)]
    [DataRow(192)]
    [DataRow(256)]
    [DataRow(512)]
    public void ChunkingPattern_WhenLengthStraddlesASubtreeBoundary_ShouldMatchSmallWrites(int chunks)
    {
        foreach (int delta in new[] { -1, 0, 1 })
        {
            byte[] input = BuildDeterministicInput((chunks * 1024) + delta);

            using var hasher = new Blake3();
            CollectionAssert.AreEqual(HashInFixedBlocks(input, 100), hasher.ComputeHash(input), $"{chunks} chunks {delta:+0;-0;+0} bytes");
        }
    }

    /// <summary>
    /// Verifies that a stream of writes of whole-chunk and ragged lengths, each large enough to hash subtrees of its
    /// own, matches a stream of 100-byte writes: each write's subtrees must line up with those the earlier writes
    /// left open.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ChunkingPattern_WhenLargeWritesFollowEachOther_ShouldMatchSmallWrites()
    {
        int[][] patterns =
        [
            [3072, 5120, 65536 + 17, 1025],
            [1025, 1025, 1025, 1025, 1025, 1025, 1025],
            [4096, 4096, 4096, 4096, 4096],
            [64 * 1024, 64 * 1024, 1, 64 * 1024],
            [2048 + 1, 8192 - 1, 32768 + 1024],
        ];

        foreach (int[] pattern in patterns)
        {
            byte[] input = BuildDeterministicInput(pattern.Sum());

            using var hasher = new Blake3();
            int offset = 0;
            foreach (int length in pattern)
            {
                hasher.TransformBlock(input, offset, length, null, 0);
                offset += length;
            }

            hasher.TransformFinalBlock([], 0, 0);

            CollectionAssert.AreEqual(HashInFixedBlocks(input, 100), hasher.Hash, string.Join(", ", pattern));
        }
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
        hasher.TransformFinalBlock([], 0, 0);
        return hasher.Hash!;
    }

    private static byte[] HashWithPseudoRandomChunks(byte[] input, int seed)
    {
        var rng = new Random(seed);
        using var hasher = new Blake3();
        int offset = 0;
        while (offset < input.Length)
        {
            // 0-127 byte chunks straddle the 64-byte block and the 1024-byte chunk boundary at irregular
            // intervals, which is what the tree-merge logic must tolerate.
            int chunk = Math.Min(rng.Next(0, 128), input.Length - offset);
            if (chunk == 0) continue;
            hasher.TransformBlock(input, offset, chunk, null, 0);
            offset += chunk;
        }
        hasher.TransformFinalBlock([], 0, 0);
        return hasher.Hash!;
    }
}
