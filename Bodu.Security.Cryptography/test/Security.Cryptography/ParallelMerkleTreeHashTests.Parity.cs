using System;
using System.IO;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Parity tests that verify <see cref="ParallelMerkleTreeHash"/> produces output that is
    /// byte-for-byte identical to <see cref="MerkleTreeHash"/> for the same input, block size,
    /// and fan-out. These tests establish that both implementations share the same tree semantics,
    /// padding strategy, and combination algorithm.
    /// </summary>
    public partial class ParallelMerkleTreeHashTests
    {
        // -----------------------------------------------------------------------------------------
        // Core parity — sync vs sync
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a single full block produces the same root in both implementations.
        /// </summary>
        [TestMethod]
        public void Parity_WhenSingleFullBlock_ShouldMatchSequentialResult()
        {
            AssertParityWithSequential(MakeData(4), blockSize: 4, fanOut: 2);
        }

        /// <summary>
        /// Verifies that two full blocks produce the same root in both implementations.
        /// </summary>
        [TestMethod]
        public void Parity_WhenTwoFullBlocks_ShouldMatchSequentialResult()
        {
            AssertParityWithSequential(MakeData(8), blockSize: 4, fanOut: 2);
        }

        /// <summary>
        /// Verifies that a partial tail block is handled identically by both implementations,
        /// confirming that zero-padding is applied consistently.
        /// </summary>
        [TestMethod]
        public void Parity_WhenPartialTailBlock_ShouldMatchSequentialResult()
        {
            AssertParityWithSequential(MakeData(9), blockSize: 4, fanOut: 2);
        }

        /// <summary>
        /// Verifies parity for a range of input lengths, block sizes, and fan-out values that
        /// exercise every tree shape: single leaf, exact groups, uneven remainders, and deep trees.
        /// </summary>
        [TestMethod]
        [DataRow(1, 2, 1)]   // single byte — only a tail block
        [DataRow(4, 2, 1)]   // one-byte input padded into 1 full block
        [DataRow(4, 2, 4)]   // single exact block
        [DataRow(4, 2, 5)]   // 1 full + 1 partial
        [DataRow(4, 2, 8)]   // 2 exact blocks
        [DataRow(4, 2, 9)]   // 2 full + 1 partial (remainder at level 1)
        [DataRow(4, 2, 12)]   // 3 exact blocks (2+1 at level 1)
        [DataRow(4, 2, 16)]   // perfect 4-leaf binary tree
        [DataRow(4, 2, 17)]   // 4 full + 1 partial (complex remainder propagation)
        [DataRow(4, 3, 12)]   // 3 leaves in one fanOut=3 group
        [DataRow(4, 3, 13)]   // 3 full + 1 partial with fanOut=3
        [DataRow(4, 4, 16)]   // 4 leaves in one fanOut=4 group
        [DataRow(8, 2, 25)]   // prime length with blockSize=8
        [DataRow(16, 3, 100)]  // larger blocks, multi-level tree
        public void Parity_WhenVariousConfigurationsUsed_ShouldMatchSequentialResult(
            int blockSize, int fanOut, int dataLength)
        {
            AssertParityWithSequential(MakeData(dataLength), blockSize, fanOut);
        }

        // -----------------------------------------------------------------------------------------
        // Parity — multi-use
        // -----------------------------------------------------------------------------------------

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
            {
                MakeData(4),
                MakeData(8),
                MakeData(9),
                MakeData(12),
                MakeData(4),   // same as first — ensures no contamination from longer runs
			};

            using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);

            foreach (byte[] data in inputs)
            {
                using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
                byte[] expected = sequential.ComputeHash(data);
                byte[] actual = parallel.ComputeHash(data);

                CollectionAssert.AreEqual(expected, actual,
                    $"Parity failure on reuse: length={data.Length}. " +
                    $"Sequential={Convert.ToHexString(expected)}, Parallel={Convert.ToHexString(actual)}");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Parity — async vs sequential
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that ComputeHashAsync produces the same result as the sequential
        /// <see cref="MerkleTreeHash"/> implementation for data delivered via a MemoryStream.
        /// </summary>
        [TestMethod]
        [DataRow(4, 2, 8)]
        [DataRow(4, 2, 9)]
        [DataRow(4, 3, 12)]
        [DataRow(8, 2, 50)]
        public async Task Parity_WhenAsyncOverloadUsed_ShouldMatchSequentialResult(
            int blockSize, int fanOut, int dataLength)
        {
            byte[] data = MakeData(dataLength);

            using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
            byte[] expected = sequential.ComputeHash(data);

            using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
            byte[] actual = await parallel.ComputeHashAsync(new MemoryStream(data));

            CollectionAssert.AreEqual(expected, actual,
                $"Async parity mismatch: blockSize={blockSize}, fanOut={fanOut}, length={dataLength}");
        }

        // -----------------------------------------------------------------------------------------
        // Parity — with per-call diagnostics
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that attaching a <see cref="MerkleTreeDiagnostics"/> instance per call does not
        /// alter the root hash compared to the sequential reference implementation.
        /// </summary>
        [TestMethod]
        [DataRow(4, 2, 8)]
        [DataRow(4, 2, 12)]
        [DataRow(4, 3, 12)]
        public void Parity_WhenDiagnosticsAttachedPerCall_ShouldStillMatchSequentialResult(
            int blockSize, int fanOut, int dataLength)
        {
            byte[] data = MakeData(dataLength);

            using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
            byte[] expected = sequential.ComputeHash(data);

            var diagnostics = new MerkleTreeDiagnostics();
            using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
            byte[] actual = parallel.ComputeHash(data, diagnostics);

            CollectionAssert.AreEqual(expected, actual,
                "Diagnostics must not affect the computed root hash.");
        }

        // -----------------------------------------------------------------------------------------
        // Parity — large input
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies parity between both implementations for a large, intentionally uneven input
        /// that forces multi-level tree reduction with a padded tail block.
        /// </summary>
        [TestMethod]
        public void Parity_WhenLargeUnevenInput_ShouldMatchSequentialResult()
        {
            // 10,007 bytes at blockSize=256: 39 full blocks + 1 tail block of 23 bytes.
            // fanOut=4 produces a 3-level tree: 40 → 10 → 3 → 1 root.
            AssertParityWithSequential(MakeData(10_007), blockSize: 256, fanOut: 4);
        }

        /// <summary>
        /// Verifies parity for an input whose length is an exact multiple of blockSize.
        /// </summary>
        [TestMethod]
        public void Parity_WhenInputIsExactMultipleOfBlockSize_ShouldMatchSequentialResult()
        {
            // 17 leaves (prime) with fanOut=3 produces uneven groups at every level.
            AssertParityWithSequential(MakeData(64 * 17), blockSize: 64, fanOut: 3);
        }

        // -----------------------------------------------------------------------------------------
        // Helper
        // -----------------------------------------------------------------------------------------

        private static void AssertParityWithSequential(byte[] data, int blockSize, int fanOut)
        {
            using var sequential = new MerkleTreeHash(Factory, blockSize, fanOut);
            byte[] expected = sequential.ComputeHash(data);

            using var parallel = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
            byte[] actual = parallel.ComputeHash(data);

            CollectionAssert.AreEqual(expected, actual,
                $"Parity failure: blockSize={blockSize}, fanOut={fanOut}, length={data.Length}. " +
                $"Sequential={Convert.ToHexString(expected)}, Parallel={Convert.ToHexString(actual)}");
        }
    }
}