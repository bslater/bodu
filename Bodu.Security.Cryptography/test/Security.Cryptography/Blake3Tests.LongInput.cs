// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.LongInput.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Long-input self-consistency tests that exercise <see cref="Blake3" />'s tree-merge logic well beyond
/// the published BLAKE3 reference-vector corpus, which tops out at 102 400 bytes.
/// </summary>
/// <remarks>
/// Independent code paths — one-shot <see cref="HashAlgorithm.ComputeHash(byte[])" />, fixed-size streaming
/// via <c>TransformBlock</c>, and pseudo-random chunk streaming — must produce identical 256-bit digests
/// for the same input. The fixed input pattern <c>(i % 251)</c> matches the official BLAKE3
/// <c>test_vectors.json</c> generator, so the resulting digest can be cross-checked against any external
/// BLAKE3 implementation by anyone running this corpus offline. Categorised as <c>Regression</c> so the
/// default BVT tier stays fast.
/// </remarks>
public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that <see cref="Blake3" /> produces identical digests for a 102 400-byte input
    /// (the length of the largest published BLAKE3 reference vector) when hashed via one-shot,
    /// 1024-byte-block streaming, and pseudo-random-chunk streaming.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ChunkingPattern_At102400Bytes_ShouldYieldIdenticalDigests()
    {
        const int InputLength = 102_400;

        byte[] input = new byte[InputLength];
        for (int i = 0; i < InputLength; i++)
            input[i] = (byte)(i % 251);

        byte[] oneShotHash;
        using (var hasher = new Blake3())
            oneShotHash = hasher.ComputeHash(input);

        byte[] blockStreamHash = HashInFixedBlocks(input, 1024);
        byte[] randomChunkHash = HashWithPseudoRandomChunks(input, seed: 2027);

        CollectionAssert.AreEqual(
            oneShotHash,
            blockStreamHash,
            "One-shot and 1024-byte-block streaming digests diverged at 102400 bytes.");

        CollectionAssert.AreEqual(
            oneShotHash,
            randomChunkHash,
            "One-shot and pseudo-random-chunk streaming digests diverged at 102400 bytes.");
    }

    private static byte[] HashInFixedBlocks(byte[] input, int blockSize)
    {
        using var hasher = new Blake3();
        int offset = 0;
        while (offset < input.Length)
        {
            int chunk = Math.Min(blockSize, input.Length - offset);
            hasher.TransformBlock(input, offset, chunk, null, 0);
            offset += chunk;
        }

        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hasher.Hash!;
    }
}
