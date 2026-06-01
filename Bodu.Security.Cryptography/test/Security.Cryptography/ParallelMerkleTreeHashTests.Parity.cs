// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.Parity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Parity tests that verify <see cref="ParallelMerkleTreeHash" /> produces output that is
/// byte-for-byte identical to <see cref="MerkleTreeHash" /> for the same input, block size,
/// and fan-out.
/// </summary>
/// <remarks>
/// <para>
/// These tests establish that both implementations share the same tree semantics, padding
/// strategy, and combination algorithm. They live here rather than in
/// <see cref="MerkleTreeHashTestsBase{THasher}" /> because they intrinsically need both
/// implementation types simultaneously — the base class is generic over a single hasher type.
/// </para>
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Core parity — sync vs sync
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a single full block produces the same root in both implementations.
    /// </summary>
    [TestMethod]
    public void Parity_WhenSingleFullBlock_ShouldMatchSequentialResult() =>
        AssertParityWithSequential(MakeData(4), blockSize: 4, fanOut: 2);

    /// <summary>
    /// Verifies that two full blocks produce the same root in both implementations.
    /// </summary>
    [TestMethod]
    public void Parity_WhenTwoFullBlocks_ShouldMatchSequentialResult() =>
        AssertParityWithSequential(MakeData(8), blockSize: 4, fanOut: 2);

    /// <summary>
    /// Verifies that a partial tail block is handled identically by both implementations,
    /// confirming that zero-padding is applied consistently.
    /// </summary>
    [TestMethod]
    public void Parity_WhenPartialTailBlock_ShouldMatchSequentialResult() =>
        AssertParityWithSequential(MakeData(9), blockSize: 4, fanOut: 2);

    /// <summary>
    /// Verifies parity across a wide range of input lengths, block sizes, and fan-out values —
    /// exercising every tree shape: single leaf, exact groups, uneven remainders, and deep trees.
    /// </summary>
    [TestMethod]
    [DataRow(1, 2, 1)]    // single byte — only a tail block
    [DataRow(4, 2, 1)]    // one byte zero-padded into one full leaf
    [DataRow(4, 2, 4)]    // exactly one block
    [DataRow(4, 2, 5)]    // 1 full + 1 partial
    [DataRow(4, 2, 8)]    // 2 exact blocks
    [DataRow(4, 2, 9)]    // 2 full + 1 partial (remainder at level 1)
    [DataRow(4, 2, 12)]   // 3 exact blocks (2 + 1 remainder at level 1)
    [DataRow(4, 2, 16)]   // perfect 4-leaf binary tree
    [DataRow(4, 2, 17)]   // 4 full + 1 partial — complex remainder propagation
    [DataRow(4, 3, 12)]   // 3 leaves in one fanOut=3 group
    [DataRow(4, 3, 13)]   // 3 full + 1 partial with fanOut=3
    [DataRow(4, 4, 16)]   // 4 leaves in one fanOut=4 group
    [DataRow(8, 2, 25)]   // prime length with blockSize=8
    [DataRow(16, 3, 100)] // larger blocks, multi-level tree
    public void Parity_WhenVariousConfigurationsUsed_ShouldMatchSequentialResult(
        int blockSize, int fanOut, int dataLength) =>
        AssertParityWithSequential(MakeData(dataLength), blockSize, fanOut);

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Multi-use — parity preserved across repeated calls
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a reused parallel instance produces the same root as a fresh sequential
    /// instance on every call in a sequence of alternating inputs.
    /// </summary>
    [TestMethod]
    public void Parity_WhenInstanceReusedAcrossMultipleCalls_ShouldMatchSequentialEachTime()
    {
        const int blockSize = 4;
        const int fanOut = 2;

        byte[][] inputs =
        [
            MakeData(4),
            MakeData(8),
            MakeData(9),
            MakeData(12),
            MakeData(4), // repeat of first — confirms no contamination from longer runs
        ];

        using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);

        foreach (var data in inputs)
        {
            using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
            var expected = sequential.ComputeHash(data);
            var actual = parallel.ComputeHash(data);

            CollectionAssert.AreEqual(expected, actual,
                $"Parity failure on reuse: length={data.Length}. " +
                $"Sequential={Convert.ToHexString(expected)}, Parallel={Convert.ToHexString(actual)}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Async parity
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that <c>ComputeHashAsync</c> produces the same result as the sequential
    /// <see cref="MerkleTreeHash" /> implementation for data delivered via a
    /// <see cref="MemoryStream" />.
    /// </summary>
    [TestMethod]
    [DataRow(4, 2, 8)]
    [DataRow(4, 2, 9)]
    [DataRow(4, 3, 12)]
    [DataRow(8, 2, 50)]
    public async Task Parity_WhenAsyncOverloadUsed_ShouldMatchSequentialResult(
        int blockSize, int fanOut, int dataLength)
    {
        var data = MakeData(dataLength);

        using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
        var expected = sequential.ComputeHash(data);

        using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        var actual = await parallel.ComputeHashAsync(new MemoryStream(data));

        CollectionAssert.AreEqual(expected, actual,
            $"Async parity mismatch: blockSize={blockSize}, fanOut={fanOut}, length={dataLength}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Diagnostics — must not perturb the result
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that attaching a <see cref="MerkleTreeDiagnostics" /> instance to the parallel
    /// hasher does not alter the root compared to the sequential reference.
    /// </summary>
    [TestMethod]
    [DataRow(4, 2, 8)]
    [DataRow(4, 2, 12)]
    [DataRow(4, 3, 12)]
    public void Parity_WhenDiagnosticsAttachedPerCall_ShouldStillMatchSequentialResult(
        int blockSize, int fanOut, int dataLength)
    {
        var data = MakeData(dataLength);

        using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
        var expected = sequential.ComputeHash(data);

        var diagnostics = new MerkleTreeDiagnostics();
        using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        var actual = parallel.ComputeHash(data, diagnostics);

        CollectionAssert.AreEqual(expected, actual,
            "Diagnostics must not affect the computed root hash.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Large-input parity
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies parity for a large, intentionally uneven input that forces multi-level tree
    /// reduction with a padded tail block.
    /// </summary>
    /// <remarks>
    /// 10,007 bytes at <c>blockSize=256</c>: 39 full blocks plus a 23-byte tail. With
    /// <c>fanOut=4</c> this produces a 3-level tree: 40 → 10 → 3 → 1 root.
    /// </remarks>
    [TestMethod]
    public void Parity_WhenLargeUnevenInput_ShouldMatchSequentialResult() =>
        AssertParityWithSequential(MakeData(10_007), blockSize: 256, fanOut: 4);

    /// <summary>
    /// Verifies parity for an input whose length is an exact multiple of <c>blockSize</c> but
    /// whose leaf count (17, prime) produces uneven groups at every level with <c>fanOut=3</c>.
    /// </summary>
    [TestMethod]
    public void Parity_WhenInputIsExactMultipleOfBlockSize_ShouldMatchSequentialResult() =>
        AssertParityWithSequential(MakeData(64 * 17), blockSize: 64, fanOut: 3);

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Computes the root via <see cref="MerkleTreeHash" /> (the reference implementation) and
    /// asserts that <see cref="ParallelMerkleTreeHash" /> produces a byte-identical result for
    /// the same input and configuration.
    /// </summary>
    private static void AssertParityWithSequential(byte[] data, int blockSize, int fanOut)
    {
        using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
        var expected = sequential.ComputeHash(data);

        using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        var actual = parallel.ComputeHash(data);

        CollectionAssert.AreEqual(expected, actual,
            $"Parity failure: blockSize={blockSize}, fanOut={fanOut}, length={data.Length}. " +
            $"Sequential={Convert.ToHexString(expected)}, Parallel={Convert.ToHexString(actual)}");
    }
}
