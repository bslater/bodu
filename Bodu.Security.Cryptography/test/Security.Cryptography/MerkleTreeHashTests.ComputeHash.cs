using System;
using System.IO;
using Bodu.Infrastructure;

namespace Bodu.Security.Cryptography
{
    public partial class MerkleTreeHashTests
    {
        // -----------------------------------------------------------------------------------------
        // ComputeHash(Stream) — argument validation
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a null stream throws <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenStreamIsNull_ShouldThrowExactly()
        {
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                hasher.ComputeHash((Stream)null!);
            });
        }

        // -----------------------------------------------------------------------------------------
        // ComputeHash(byte[], int, int) — argument validation
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a null array with a non-zero count throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenArrayIsNullWithNonZeroCount_ShouldThrowExactly()
        {
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
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
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
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
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
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
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                hasher.ComputeHash(MakeData(4), 2, 4);
            });
        }

        // -----------------------------------------------------------------------------------------
        // Return value — non-null and correct length
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that the hash returned by the span overload is not null and has the expected length.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSpanProvided_ShouldReturnHashOfExpectedLength()
        {
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            byte[] result = hasher.ComputeHash(MakeData(8).AsSpan());
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length); // MonitoringHashAlgorithm: sizeof(uint)
        }

        /// <summary>
        /// Verifies that the hash returned by the stream overload is not null and has the expected length.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenStreamProvided_ShouldReturnHashOfExpectedLength()
        {
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var stream = new IncrementingByteStream(8);
            byte[] result = hasher.ComputeHash(stream);
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length);
        }

        // -----------------------------------------------------------------------------------------
        // Overload equivalence
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that all four ComputeHash overloads produce identical output for the same input.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSameInputUsedAcrossOverloads_ShouldReturnIdenticalHashes()
        {
            byte[] data = MakeData(13);

            using var h1 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h3 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h4 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] fromSpan = h1.ComputeHash(data.AsSpan());
            byte[] fromArray = h2.ComputeHash(data);
            byte[] fromSlice = h3.ComputeHash(data, 0, data.Length);
            using var ms = new MemoryStream(data);
            byte[] fromStream = h4.ComputeHash(ms);

            CollectionAssert.AreEqual(fromSpan, fromArray, "Span vs array mismatch.");
            CollectionAssert.AreEqual(fromSpan, fromSlice, "Span vs slice mismatch.");
            CollectionAssert.AreEqual(fromSpan, fromStream, "Span vs stream mismatch.");
        }

        // -----------------------------------------------------------------------------------------
        // Stream reads — partial delivery
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a stream that delivers data in partial chunks produces the same hash as
        /// providing the same bytes via the span overload.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenStreamDeliversPartialReads_ShouldProduceSameResultAsSpan()
        {
            // IncrementingByteStream purposely delivers at most half its remaining bytes per Read
            // call, exercising the loop in ComputeHash(Stream).
            const int length = 37;

            using var hasher1 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var hasher2 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            using var stream = new IncrementingByteStream(length);
            byte[] fromStream = hasher1.ComputeHash(stream);
            byte[] fromSpan = hasher2.ComputeHash(new IncrementingByteStream(length).ToArray().AsSpan());

            CollectionAssert.AreEqual(fromStream, fromSpan);
        }

        // -----------------------------------------------------------------------------------------
        // Reuse — same instance, multiple calls
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that calling ComputeHash a second time on the same instance resets state
        /// and produces the same result as a freshly constructed instance.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalledTwiceOnSameInstance_ShouldResetStateAndProduceSameResult()
        {
            byte[] data = MakeData(9);

            using var reused = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var fresh = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] first = reused.ComputeHash(data);
            byte[] second = reused.ComputeHash(data);
            byte[] reference = fresh.ComputeHash(data);

            CollectionAssert.AreEqual(reference, first, "First call result does not match reference.");
            CollectionAssert.AreEqual(reference, second, "Second call result does not match reference.");
        }

        /// <summary>
        /// Verifies that the instance correctly handles switching between different input sizes
        /// across multiple calls without state contamination.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalledWithDifferentInputSizes_ShouldNotContaminateState()
        {
            using var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] small = MakeData(3);
            byte[] large = MakeData(20);

            byte[] hashSmall1 = hasher.ComputeHash(small);
            byte[] hashLarge = hasher.ComputeHash(large);
            byte[] hashSmall2 = hasher.ComputeHash(small);

            CollectionAssert.AreEqual(hashSmall1, hashSmall2, "Small hash not reproducible after large input.");
            CollectionAssert.AreNotEqual(hashSmall1, hashLarge, "Small and large hash should differ.");
        }

        // -----------------------------------------------------------------------------------------
        // Slice offset
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
            const int count = 6;

            using var h1 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] fromSlice = h1.ComputeHash(data, offset, count);
            byte[] fromSpan = h2.ComputeHash(data.AsSpan(offset, count));

            CollectionAssert.AreEqual(fromSpan, fromSlice);
        }

        // -----------------------------------------------------------------------------------------
        // Determinism
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that hashing the same input twice across separate instances yields the same hash.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSameInputHashedTwice_ShouldReturnIdenticalResults()
        {
            byte[] data = MakeData(17);

            using var h1 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            CollectionAssert.AreEqual(h1.ComputeHash(data), h2.ComputeHash(data));
        }

        /// <summary>
        /// Verifies that two inputs that differ by a single byte produce different hashes.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputsDifferByOneByte_ShouldReturnDifferentHashes()
        {
            byte[] a = MakeData(8);
            byte[] b = (byte[])a.Clone();
            b[3] ^= 0xFF;

            using var h1 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var h2 = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            CollectionAssert.AreNotEqual(h1.ComputeHash(a), h2.ComputeHash(b));
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

            using var hasher = new MerkleTreeHash(Factory, blockSize, fanOut);
            byte[] actual = hasher.ComputeHash(data);

            CollectionAssert.AreEqual(expected, actual,
                $"Mismatch: blockSize={blockSize}, fanOut={fanOut}, length={dataLength}");
        }

        // -----------------------------------------------------------------------------------------
        // Vector A — single full block, fanOut=2
        //   Data:   { 1, 2, 3, 4 }
        //   Leaf 0: { 1,2,3,4 } padded → sum = 10 → { 10,0,0,0 }
        //   1 leaf → root = { 10,0,0,0 }
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a single full block is hashed directly into the root without reduction.
        /// Data: {1,2,3,4} → leaf sum = 10 → root = { 0x0A, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSingleFullBlock_ShouldReturnLeafHashAsRoot()
        {
            byte[] data = { 1, 2, 3, 4 };
            byte[] expected = BitConverter.GetBytes((uint)10);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Vector B — two full blocks, fanOut=2
        //   Data:   { 1,2,3,4,  5,6,7,8 }
        //   Leaf 0: sum=10 → { 10,0,0,0 }
        //   Leaf 1: sum=26 → { 26,0,0,0 }
        //   Level1: sum({ 10,0,0,0, 26,0,0,0 }) = 36 → { 36,0,0,0 }
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that two full blocks are combined into a single root.
        /// Data: {1..8} → leaf sums 10,26 → root = 36 → { 0x24, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenTwoFullBlocks_ShouldCombineIntoRoot()
        {
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8 };
            byte[] expected = BitConverter.GetBytes((uint)36);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Vector C — one full block + one partial block, fanOut=2
        //   Data:   { 1,2,3,4,  5 }
        //   Leaf 0: sum=10 → { 10,0,0,0 }
        //   Leaf 1: { 5,0,0,0 } (zero-padded) → sum=5 → { 5,0,0,0 }
        //   Level1: sum({ 10,0,0,0, 5,0,0,0 }) = 15 → { 15,0,0,0 }
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a partial tail block is zero-padded before hashing.
        /// Data: {1,2,3,4,5} → leaf sums 10,5 → root = 15 → { 0x0F, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenPartialTailBlock_ShouldZeroPadAndProduceCorrectRoot()
        {
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] expected = BitConverter.GetBytes((uint)15);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Vector D — three full blocks, fanOut=2  (uneven reduction: 3 leaves → 2 level-1 nodes)
        //   Data:   { 1..4, 5..8, 9..12 }
        //   Leaf 0: sum=10, Leaf 1: sum=26, Leaf 2: sum=42
        //   Level1, group0: sum({ 10,0,0,0, 26,0,0,0 }) = 36 → { 36,0,0,0 }
        //   Level1, remainder: CombineAndHash([{ 42,0,0,0 }]) = 42 → { 42,0,0,0 }
        //   Level2: sum({ 36,0,0,0, 42,0,0,0 }) = 78 → { 78,0,0,0 }
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that three leaves with fanOut=2 produce a two-level tree with a partial
        /// remainder promoted to a dedicated node.
        /// Data: {1..12} → leaf sums 10,26,42 → root = 78 → { 0x4E, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenThreeLeavesWithFanOutTwo_ShouldProduceCorrectTwoLevelRoot()
        {
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            byte[] expected = BitConverter.GetBytes((uint)78);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Vector E — three full blocks, fanOut=3  (all three fit in one group)
        //   Data:   { 1..4, 5..8, 9..12 }
        //   Leaf 0: sum=10, Leaf 1: sum=26, Leaf 2: sum=42
        //   Level1: sum({ 10,0,0,0, 26,0,0,0, 42,0,0,0 }) = 78 → { 78,0,0,0 }
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that three leaves with fanOut=3 are reduced in a single step.
        /// Data: {1..12} → leaf sums 10,26,42 → root = 78 → { 0x4E, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenThreeLeavesWithFanOutThree_ShouldReduceInOneStep()
        {
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            byte[] expected = BitConverter.GetBytes((uint)78);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 3);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Vector F — blockSize=1 (every byte is its own leaf), fanOut=2, 4 bytes
        //   Leaf 0: { 10,0,0,0 }, Leaf 1: { 20,0,0,0 }
        //   Leaf 2: { 30,0,0,0 }, Leaf 3: { 40,0,0,0 }
        //   Level1, group0: 10+20=30 → { 30,0,0,0 }
        //   Level1, group1: 30+40=70 → { 70,0,0,0 }
        //   Level2: 30+70=100 → { 100,0,0,0 }
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a blockSize of 1 treats every byte as an independent leaf.
        /// Data: {10,20,30,40} → leaf sums 10,20,30,40 → root = 100 → { 0x64, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenBlockSizeIsOne_ShouldTreatEachByteAsLeaf()
        {
            byte[] data = { 10, 20, 30, 40 };
            byte[] expected = BitConverter.GetBytes((uint)100);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 1, fanOut: 2);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Vector G — single byte, partial block only
        //   Data:   { 7 }
        //   Leaf 0: { 7,0,0,0 } (zero-padded to blockSize=4) → sum=7 → { 7,0,0,0 }
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a single-byte input is zero-padded and hashed as the sole leaf, which
        /// becomes the root directly.
        /// Data: {7} → padded { 7,0,0,0 } → root = 7 → { 0x07, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSingleByteInput_ShouldZeroPadAndReturnLeafAsRoot()
        {
            byte[] data = { 7 };
            byte[] expected = BitConverter.GetBytes((uint)7);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Vector H — two partial blocks with fanOut=3, 9 bytes
        //   Data:   { 1..4, 5..8, 9 }
        //   Leaf 0: sum=10, Leaf 1: sum=26, Leaf 2: { 9,0,0,0 } → sum=9
        //   Level1 (fanOut=3 ⇒ all three fit): sum({ 10,0,0,0, 26,0,0,0, 9,0,0,0 }) = 45
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a partial tail block and fanOut=3 reduce all three leaves in a single step.
        /// Data: {1..8,9} → leaf sums 10,26,9 → root = 45 → { 0x2D, 0x00, 0x00, 0x00 }.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenNineBytesWithFanOutThree_ShouldReduceAllThreeLeavesInOneStep()
        {
            byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] expected = BitConverter.GetBytes((uint)45);

            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 3);
            CollectionAssert.AreEqual(expected, hasher.ComputeHash(data));
        }

        // -----------------------------------------------------------------------------------------
        // Padding isolation — partial block value must not influence next computation
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that two computations on the same instance that differ only in tail-block
        /// padding produce different hashes, confirming that padding is not carried over.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenPartialBlockFollowedByLargerInput_ShouldNotReuseOldPaddingBytes()
        {
            using var hasher = new MerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

            // First call — 5 bytes (tail = {5, 0, 0, 0} padded).
            byte[] first = hasher.ComputeHash(new byte[] { 1, 2, 3, 4, 5 });

            // Second call — 4 bytes exact (no padding). Root is just the leaf sum.
            byte[] second = hasher.ComputeHash(new byte[] { 1, 2, 3, 4 });

            CollectionAssert.AreNotEqual(first, second,
                "A 5-byte and 4-byte input that share a prefix must produce different roots.");
        }
    }
}