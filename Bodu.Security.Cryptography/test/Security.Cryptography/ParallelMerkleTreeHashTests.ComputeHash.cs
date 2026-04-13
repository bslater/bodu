using System;

namespace Bodu.Security.Cryptography
{
    public partial class ParallelMerkleTreeHashTests
    {
        // -----------------------------------------------------------------------------------------
        // Argument validation
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a null byte array with a non-zero count throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenArrayIsNullWithNonZeroCount_ShouldThrowExactly()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                hasher.ComputeHash(null!, 0, 1);
            });
        }

        /// <summary>
        /// Verifies that a negative offset throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenOffsetIsNegative_ShouldThrowExactly()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                hasher.ComputeHash(MakeData(4), -1, 4);
            });
        }

        /// <summary>
        /// Verifies that a negative count throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCountIsNegative_ShouldThrowExactly()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                hasher.ComputeHash(MakeData(4), 0, -1);
            });
        }

        /// <summary>
        /// Verifies that an offset and count that exceed the array bounds throws
        /// <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenOffsetPlusCountExceedsArrayLength_ShouldThrowExactly()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                hasher.ComputeHash(MakeData(4), 2, 4);
            });
        }

        /// <summary>
        /// Verifies that calling any ComputeHash overload after Dispose throws
        /// <see cref="ObjectDisposedException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInstanceIsDisposed_ShouldThrowExactly()
        {
            var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            hasher.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                hasher.ComputeHash(MakeData(4));
            });
        }

        // -----------------------------------------------------------------------------------------
        // Empty / no-data path
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that hashing an empty span throws <see cref="InvalidOperationException"/>
        /// because no leaves were produced.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputIsEmpty_ShouldThrowInvalidOperationException()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                hasher.ComputeHash(ReadOnlySpan<byte>.Empty);
            });
        }

        // -----------------------------------------------------------------------------------------
        // Return value
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that the hash returned by the span overload is not null and has the expected length.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSpanProvided_ShouldReturnHashOfExpectedLength()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            byte[] result = hasher.ComputeHash(MakeData(8).AsSpan());
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length); // MonitoringHashAlgorithm: sizeof(uint)
        }

        // -----------------------------------------------------------------------------------------
        // Overload equivalence
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that all three sync ComputeHash overloads produce identical output for the
        /// same input.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSameInputUsedAcrossOverloads_ShouldReturnIdenticalHashes()
        {
            byte[] data = MakeData(13);

            using var h1 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h3 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] fromSpan = h1.ComputeHash(data.AsSpan());
            byte[] fromArray = h2.ComputeHash(data);
            byte[] fromSlice = h3.ComputeHash(data, 0, data.Length);

            CollectionAssert.AreEqual(fromSpan, fromArray, "Span vs array mismatch.");
            CollectionAssert.AreEqual(fromSpan, fromSlice, "Span vs slice mismatch.");
        }

        // -----------------------------------------------------------------------------------------
        // Determinism
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that two separate instances hashing the same input produce identical results.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSameInputHashedOnTwoInstances_ShouldReturnIdenticalResults()
        {
            byte[] data = MakeData(17);
            using var h1 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            CollectionAssert.AreEqual(h1.ComputeHash(data), h2.ComputeHash(data));
        }

        /// <summary>
        /// Verifies that two inputs differing by a single byte produce different hashes.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputsDifferByOneByte_ShouldReturnDifferentHashes()
        {
            byte[] a = MakeData(8);
            byte[] b = (byte[])a.Clone();
            b[3] ^= 0xFF;

            using var h1 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            CollectionAssert.AreNotEqual(h1.ComputeHash(a), h2.ComputeHash(b));
        }

        // -----------------------------------------------------------------------------------------
        // Offset / count
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that ComputeHash(byte[], offset, count) hashes only the specified region and
        /// produces the same result as the span overload over that region.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenOffsetAndCountSpecified_ShouldHashOnlyThatRegion()
        {
            byte[] data = MakeData(12);
            const int offset = 2;
            const int count = 7;

            using var h1 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            CollectionAssert.AreEqual(
                h1.ComputeHash(data, offset, count),
                h2.ComputeHash(data.AsSpan(offset, count)));
        }

        // -----------------------------------------------------------------------------------------
        // Various block/fanOut combinations
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that the computed hash matches the hand-computed additive root across a range
        /// of block sizes and fan-out values.
        /// </summary>
        [TestMethod]
        [DataRow(1, 2, 7)]
        [DataRow(4, 2, 8)]
        [DataRow(4, 2, 9)]
        [DataRow(4, 3, 12)]
        [DataRow(4, 4, 16)]
        [DataRow(8, 2, 25)]
        [DataRow(16, 3, 50)]
        public void ComputeHash_WhenVariousConfigurationsUsed_ShouldMatchHandComputedRoot(
            int blockSize, int fanOut, int dataLength)
        {
            byte[] data = MakeData(dataLength);
            byte[] expected = ComputeAdditiveRoot(data, blockSize, fanOut);

            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data),
                $"Mismatch: blockSize={blockSize}, fanOut={fanOut}, length={dataLength}");
        }

        // -----------------------------------------------------------------------------------------
        // Multi-use — same instance, multiple sequential calls
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that calling ComputeHash a second time on the same instance resets all pipeline
        /// state and produces the same result as a freshly constructed instance on identical input.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalledTwiceOnSameInstance_ShouldResetAndProduceCorrectResult()
        {
            byte[] data = MakeData(9);
            byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

            using var reused = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] first = reused.ComputeHash(data);
            byte[] second = reused.ComputeHash(data);

            CollectionAssert.AreEqual(expected, first, "First call produced wrong result.");
            CollectionAssert.AreEqual(expected, second, "Second call produced wrong result.");
        }

        /// <summary>
        /// Verifies that the instance correctly handles switching between different input sizes
        /// across multiple calls without state contamination from the previous computation.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalledWithAlternatingInputSizes_ShouldNotContaminateState()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] small = MakeData(3);
            byte[] large = MakeData(20);

            byte[] expectedSmall = ComputeAdditiveRoot(small, DefaultBlockSize, DefaultFanOut);
            byte[] expectedLarge = ComputeAdditiveRoot(large, DefaultBlockSize, DefaultFanOut);

            byte[] hashSmall1 = hasher.ComputeHash(small);
            byte[] hashLarge = hasher.ComputeHash(large);
            byte[] hashSmall2 = hasher.ComputeHash(small);

            CollectionAssert.AreEqual(expectedSmall, hashSmall1, "First small call wrong.");
            CollectionAssert.AreEqual(expectedLarge, hashLarge, "Large call wrong.");
            CollectionAssert.AreEqual(expectedSmall, hashSmall2, "Second small call wrong (state contaminated).");
        }

        /// <summary>
        /// Verifies that the instance produces consistent results across many sequential calls on
        /// the same input, confirming that Reset restores a fully clean state every time.
        /// </summary>
        [TestMethod]
        [DataRow(5)]
        [DataRow(10)]
        [DataRow(25)]
        public void ComputeHash_WhenCalledRepeatedly_ShouldProduceConsistentResults(int callCount)
        {
            byte[] data = MakeData(13);
            byte[] expected = ComputeAdditiveRoot(data, DefaultBlockSize, DefaultFanOut);

            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            for (int i = 0; i < callCount; i++)
            {
                byte[] result = hasher.ComputeHash(data);
                CollectionAssert.AreEqual(expected, result,
                    $"Result mismatch on call {i + 1} of {callCount}.");
            }
        }

        /// <summary>
        /// Verifies that a partial-block tail from one call does not bleed into the buffer seen by
        /// the next call, confirming that buffer state is fully reset between computations.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenPartialBlockFollowedByDifferentInput_ShouldNotReuseOldTailBytes()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

            // First call leaves a partial tail of {5} in the buffer.
            byte[] first = hasher.ComputeHash(new byte[] { 1, 2, 3, 4, 5 });

            // Second call — same first four bytes but no tail. Result must equal a single full block.
            byte[] second = hasher.ComputeHash(new byte[] { 1, 2, 3, 4 });

            byte[] expectedFirst = ComputeAdditiveRoot(new byte[] { 1, 2, 3, 4, 5 }, 4, 2);
            byte[] expectedSecond = ComputeAdditiveRoot(new byte[] { 1, 2, 3, 4 }, 4, 2);

            CollectionAssert.AreEqual(expectedFirst, first, "First call result wrong.");
            CollectionAssert.AreEqual(expectedSecond, second, "Second call result wrong (old tail bytes leaked).");
            CollectionAssert.AreNotEqual(first, second, "Different inputs must produce different roots.");
        }

        /// <summary>
        /// Verifies that multi-use with per-call diagnostics records only the nodes from the
        /// corresponding call and does not accumulate nodes across calls.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenDiagnosticsPassedPerCall_ShouldRecordOnlyThatCallsNodes()
        {
            byte[] data = MakeData(8); // 2 full blocks → 2 leaves + 1 internal

            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

            var diag1 = new MerkleTreeDiagnostics();
            var diag2 = new MerkleTreeDiagnostics();

            hasher.ComputeHash(data, diag1);
            hasher.ComputeHash(data, diag2);

            // Each diagnostics instance must contain exactly the nodes from its own call.
            Assert.AreEqual(3, diag1.GetAllNodes().Count,
                "diag1 should contain 2 leaves + 1 internal = 3 nodes.");
            Assert.AreEqual(3, diag2.GetAllNodes().Count,
                "diag2 should contain 2 leaves + 1 internal = 3 nodes, not 6.");
        }

        /// <summary>
        /// Verifies that omitting diagnostics on one call and supplying them on the next produces
        /// a clean, isolated trace containing only the second call's nodes.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenDiagnosticsOmittedThenSupplied_ShouldOnlyRecordSecondCall()
        {
            byte[] data = MakeData(8);

            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

            // First call — no diagnostics.
            hasher.ComputeHash(data);

            // Second call — diagnostics attached.
            var diag = new MerkleTreeDiagnostics();
            hasher.ComputeHash(data, diag);

            Assert.AreEqual(3, diag.GetAllNodes().Count,
                "Diagnostics should contain only the second call's 3 nodes.");
        }
    }
}