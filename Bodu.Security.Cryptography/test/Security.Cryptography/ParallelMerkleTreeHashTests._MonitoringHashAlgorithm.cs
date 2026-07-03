// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests._MonitoringHashAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests that validate the exact invocation pattern of <see cref="MonitoringHashAlgorithm" />
/// within <see cref="ParallelMerkleTreeHash" />, verifying which <see cref="HashAlgorithm" />
/// methods are called, how many times, with how many bytes, and that the incremental byte
/// sums accumulate to the correct hash values.
/// </summary>
/// <remarks>
/// <para>
/// <b>Path discrimination:</b>
/// <list type="bullet">
///   <item>
///     <b>Leaf hashers</b> are created by <c>HashSpan</c>, which calls
///     <see cref="HashAlgorithm.TryComputeHash" />. The runtime dispatches through
///     <c>HashCore(ReadOnlySpan&lt;byte&gt;)</c> then <c>TryHashFinal</c>. The <c>HashSize</c>
///     property is accessed once to size the result buffer. <c>HashFinal</c> and
///     <c>HashCore(byte[], int, int)</c> are never called.
///   </item>
///   <item>
///     <b>Internal node hashers</b> are created by <c>CombineAndHash</c>, which calls
///     <c>TransformBlock</c> for each non-last child and <c>TransformFinalBlock</c> for the
///     last. Both dispatch through <c>HashCore(byte[], int, int)</c>.
///     <c>TransformFinalBlock</c> additionally calls <c>HashFinal</c>, and <c>hasher.Hash</c>
///     is read once after that. <c>TryHashFinal</c> and <c>HashCore(ReadOnlySpan&lt;byte&gt;)</c>
///     are never called.
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Counter safety:</b> <see cref="MonitoringHashAlgorithm.Dispose" /> zeroes all counters
/// before <c>DisposeCalled</c> fires. All counter captures therefore happen inside event
/// handlers that fire during the computation — <c>TryHashFinalCalled</c> for leaf hashers and
/// <c>HashFinalCalled</c> for internal node hashers — where the instance is still live.
/// </para>
/// <para>
/// <b>Thread safety:</b> level workers run concurrently, so all accumulation uses
/// <see cref="Interlocked" /> operations.
/// </para>
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Factory call count — total instances created equals leaves + internal nodes
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the factory is called exactly once per leaf when a single-block input
    /// produces only one leaf and no internal nodes (the leaf IS the root).
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenSingleBlock_ShouldCreateExactlyOneHasherInstance()
    {
        // 1 full block → 1 leaf hasher, no internal nodes (leaf is the root directly).
        int factoryCalls = 0;
        Func<HashAlgorithm> countingFactory = () =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new MonitoringHashAlgorithm();
        };

        using var hasher = new ParallelMerkleTreeHash(countingFactory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash([1, 2, 3, 4]);

        Assert.AreEqual(1, factoryCalls,
            "A single-block input should invoke the factory exactly once (one leaf, no internal node).");
    }

    /// <summary>
    /// Verifies that two full blocks create exactly three hasher instances: two leaf hashers
    /// and one internal node hasher that combines them.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenTwoFullBlocks_ShouldCreateThreeHasherInstances()
    {
        // 2 leaves + 1 internal node = 3 factory calls.
        int factoryCalls = 0;
        Func<HashAlgorithm> countingFactory = () =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new MonitoringHashAlgorithm();
        };

        using var hasher = new ParallelMerkleTreeHash(countingFactory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8));

        Assert.AreEqual(3, factoryCalls,
            "Two blocks require two leaf hashers and one internal node hasher (3 total).");
    }

    /// <summary>
    /// Verifies that three full blocks with fanOut=2 create exactly six hasher instances:
    /// three leaves, two level-1 internal nodes, and one level-2 internal node.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenThreeFullBlocksWithFanOutTwo_ShouldCreateSixHasherInstances()
    {
        // Tree: 3 leaves → internal0(L0,L1) + internal1(L2) → internal2(I0,I1) → root
        // Factory calls: 3 leaves + 3 internal = 6.
        int factoryCalls = 0;
        Func<HashAlgorithm> countingFactory = () =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new MonitoringHashAlgorithm();
        };

        using var hasher = new ParallelMerkleTreeHash(countingFactory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(12));

        Assert.AreEqual(6, factoryCalls,
            "Three blocks with fanOut=2 require 3 leaf + 3 internal hashers (6 total).");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Path discrimination — leaf hashers use the span path exclusively
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that leaf hashers invoke <c>HashCore(ReadOnlySpan&lt;byte&gt;)</c> and
    /// <c>TryHashFinal</c>, confirming that <c>HashSpan</c> routes through
    /// <see cref="HashAlgorithm.TryComputeHash" />.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_LeafHashers_ShouldUseSpanCoreAndTryHashFinalPath()
    {
        int leafTryHashFinalCount = 0;
        int leafHashCoreSpanCount = 0;
        int leafHashFinalCount = 0;    // must remain 0 for leaf hashers
        int leafHashCoreCount = 0;    // must remain 0 for leaf hashers

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();

            // TryHashFinalCalled fires for leaf hashers (TryComputeHash path).
            // Capture all counters here before Dispose can zero them.
            algo.TryHashFinalCalled += (sender, _) =>
            {
                var m = (MonitoringHashAlgorithm)sender!;
                Interlocked.Increment(ref leafTryHashFinalCount);
                Interlocked.Add(ref leafHashCoreSpanCount, m.HashCoreSpanCallCount);
                Interlocked.Add(ref leafHashFinalCount, m.HashFinalCallCount);
                Interlocked.Add(ref leafHashCoreCount, m.HashCoreCallCount);
            };

            return algo;
        };

        // 2 full blocks → 2 leaf hashers. Each should fire TryHashFinalCalled exactly once.
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8));

        Assert.AreEqual(2, leafTryHashFinalCount,
            "Each leaf hasher must call TryHashFinal exactly once.");
        Assert.AreEqual(2, leafHashCoreSpanCount,
            "Each leaf hasher must call HashCore(ReadOnlySpan<byte>) exactly once.");
        Assert.AreEqual(0, leafHashFinalCount,
            "Leaf hashers must never call HashFinal (array path).");
        Assert.AreEqual(0, leafHashCoreCount,
            "Leaf hashers must never call HashCore(byte[], int, int).");
    }

    /// <summary>
    /// Verifies that leaf hashers access the <c>HashSize</c> property exactly once — to
    /// allocate the result buffer inside <c>HashSpan</c> — and never access the <c>Hash</c>
    /// property (the result is written directly to the pre-allocated span).
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_LeafHashers_ShouldAccessHashSizeOnceAndNeverAccessHash()
    {
        int totalHashSizeAccesses = 0;
        int totalHashAccesses = 0;

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();

            algo.TryHashFinalCalled += (sender, _) =>
            {
                var m = (MonitoringHashAlgorithm)sender!;
                Interlocked.Add(ref totalHashSizeAccesses, m.HashSizeAccessCount);
                Interlocked.Add(ref totalHashAccesses, m.HashAccessCount);
            };

            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8)); // 2 leaf hashers

        Assert.AreEqual(2, totalHashSizeAccesses,
            "Each leaf hasher should access HashSize exactly once (to allocate the result buffer).");
        Assert.AreEqual(0, totalHashAccesses,
            "Leaf hashers must not access the Hash property; TryComputeHash writes to the span.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Path discrimination — internal node hashers use the array path exclusively
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that internal node hashers invoke <c>HashCore(byte[], int, int)</c> and
    /// <c>HashFinal</c>, confirming that <c>CombineAndHash</c> uses the
    /// <c>TransformBlock</c> / <c>TransformFinalBlock</c> pipeline.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_InternalHashers_ShouldUseArrayCoreAndHashFinalPath()
    {
        int internalHashFinalCount = 0;
        int internalHashCoreCount = 0;
        int internalTryHashFinalCount = 0; // must remain 0
        int internalHashCoreSpanCount = 0; // must remain 0

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();

            // HashFinalCalled fires for internal hashers (TransformFinalBlock path).
            algo.HashFinalCalled += (sender, _) =>
            {
                var m = (MonitoringHashAlgorithm)sender!;
                Interlocked.Increment(ref internalHashFinalCount);
                Interlocked.Add(ref internalHashCoreCount, m.HashCoreCallCount);
                Interlocked.Add(ref internalTryHashFinalCount, m.TryHashFinalCallCount);
                Interlocked.Add(ref internalHashCoreSpanCount, m.HashCoreSpanCallCount);
            };

            return algo;
        };

        // 2 full blocks → 1 internal node (combining L0 and L1).
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8));

        Assert.AreEqual(1, internalHashFinalCount,
            "The internal node hasher must call HashFinal exactly once.");
        Assert.AreEqual(3, internalHashCoreCount,
            "The internal node hasher must call HashCore(byte[]) three times: " +
            "once via TransformBlock for the 0x01 domain prefix, once for child 0, " +
            "once via TransformFinalBlock for child 1.");
        Assert.AreEqual(0, internalTryHashFinalCount,
            "Internal node hashers must never call TryHashFinal.");
        Assert.AreEqual(0, internalHashCoreSpanCount,
            "Internal node hashers must never call HashCore(ReadOnlySpan<byte>).");
    }

    /// <summary>
    /// Verifies that internal node hashers access the <c>Hash</c> property exactly once —
    /// to read <c>hasher.Hash!</c> after <c>TransformFinalBlock</c> — and never access
    /// <c>HashSize</c> (no buffer allocation is needed on the internal path).
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_InternalHashers_ShouldAccessHashOnceAndNeverAccessHashSize()
    {
        int totalHashAccesses = 0;
        int totalHashSizeAccesses = 0;

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();

            algo.HashFinalCalled += (sender, _) =>
            {
                var m = (MonitoringHashAlgorithm)sender!;
                // HashAccessCount has not yet been incremented here — Hash is read AFTER
                // HashFinal returns. Capture HashSizeAccessCount; HashAccessCount is captured
                // in the HashAccessed event instead.
                Interlocked.Add(ref totalHashSizeAccesses, m.HashSizeAccessCount);
            };

            algo.HashAccessed += (sender, _) =>
            {
                Interlocked.Increment(ref totalHashAccesses);
            };

            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8)); // 1 internal node

        Assert.AreEqual(1, totalHashAccesses,
            "The internal node hasher must access the Hash property exactly once.");
        Assert.AreEqual(0, totalHashSizeAccesses,
            "Internal node hashers must not access HashSize; they have no result buffer to allocate.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // BytesProcessed — leaf hashers process exactly blockSize bytes
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that each leaf hasher processes exactly <c>1 + actualBlockLength</c> bytes — the one-byte
    /// <c>0x00</c> leaf-domain prefix plus the block's real length — confirming the partial tail is hashed at its
    /// actual length rather than zero-padded to the full block size.
    /// </summary>
    [DataTestMethod]
    [DataRow(4, 2, 8)]   // 2 full blocks → each leaf processes 1 + 4 = 5 bytes
    [DataRow(4, 2, 5)]   // 1 full + 1 partial → leaves process 5 and 1 + 1 = 2 bytes
    [DataRow(4, 2, 1)]   // single 1-byte input → leaf processes 1 + 1 = 2 bytes (no padding)
    [DataRow(8, 2, 17)]  // 2 full + 1 partial → leaves process 9, 9, and 1 + 1 = 2 bytes
    public void MonitoringAlgorithm_LeafHashers_ShouldEachProcessPrefixPlusActualBlockLength(
        int blockSize, int fanOut, int dataLength)
    {
        // Expected per-leaf byte counts: one leaf per block, each processing 1 (prefix) + the block's real length.
        var expected = new List<long>();
        for (int offset = 0; offset < dataLength; offset += blockSize)
            expected.Add(1 + Math.Min(blockSize, dataLength - offset));
        expected.Sort();

        var leafBytesObserved = new ConcurrentBag<long>();

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();
            algo.TryHashFinalCalled += (sender, _) =>
                leafBytesObserved.Add(((MonitoringHashAlgorithm)sender!).BytesProcessed);
            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize, fanOut);
        hasher.ComputeHash(MakeData(dataLength));

        var actual = new List<long>(leafBytesObserved);
        actual.Sort();

        CollectionAssert.AreEqual(expected, actual,
            $"Leaf byte counts mismatch (blockSize={blockSize}, dataLength={dataLength}). " +
            $"Expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}].");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // BytesProcessed — internal hashers process exactly (childCount × hashOutputSize) bytes
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that each internal node hasher processes exactly <c>1 + childCount × hashOutputSize</c> bytes — the
    /// one-byte <c>0x01</c> internal-node prefix plus every child hash in full — confirming that child hashes are
    /// fed without truncation or padding.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_InternalHashers_WhenTwoFullBlocks_ShouldProcessNineBytes()
    {
        // 2 full blocks, fanOut=2 → 1 internal hasher over the 0x01 prefix + 2 child hashes × 4 bytes = 1 + 8 = 9.
        const int hashOutputSize = 4; // sizeof(uint) for MonitoringHashAlgorithm
        const int expectedBytes = 1 + (2 * hashOutputSize);
        var internalBytesObserved = new ConcurrentBag<long>();

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();
            algo.HashFinalCalled += (sender, _) =>
                internalBytesObserved.Add(((MonitoringHashAlgorithm)sender!).BytesProcessed);
            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8));

        Assert.HasCount(1, internalBytesObserved,
            "Exactly one internal hasher should have been created.");
        Assert.AreEqual(expectedBytes, internalBytesObserved.ToArray()[0],
            "The internal hasher must process 1 + 2 × 4 = 9 bytes (0x01 prefix + two child hashes).");
    }

    /// <summary>
    /// Verifies that when three leaves produce a single-child remainder node at level 1, that remainder node's
    /// hasher processes exactly <c>1 + hashOutputSize</c> bytes (the 0x01 prefix plus one child hash), not the full
    /// fan-out width.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_InternalHashers_WhenSingleChildRemainder_ShouldProcessPrefixPlusOneChild()
    {
        // 3 blocks, fanOut=2 (every internal node adds the 1-byte 0x01 domain prefix):
        //   internal0 (L0,L1): BytesProcessed = 1 + 8 = 9
        //   internal1 (L2 alone — remainder): BytesProcessed = 1 + 4 = 5  ← the assertion target
        //   internal2 (I0,I1): BytesProcessed = 1 + 8 = 9
        const int hashOutputSize = 4;
        var internalBytes = new ConcurrentBag<long>();

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();
            algo.HashFinalCalled += (sender, _) =>
                internalBytes.Add(((MonitoringHashAlgorithm)sender!).BytesProcessed);
            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(12));

        var sorted = new System.Collections.Generic.List<long>(internalBytes);
        sorted.Sort();

        // Expected: one internal with 1 + 4 = 5 bytes (single-child), two with 1 + 8 = 9 bytes (two children).
        Assert.HasCount(3, sorted, "Expected three internal node hashers.");
        Assert.AreEqual(1 + hashOutputSize, sorted[0], "Smallest internal should be 5 bytes (prefix + single child).");
        Assert.AreEqual(1 + (2 * hashOutputSize), sorted[1], "Second internal should be 9 bytes (prefix + two children).");
        Assert.AreEqual(1 + (2 * hashOutputSize), sorted[2], "Third internal should be 9 bytes (prefix + two children).");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // HashCoreCallCount per internal hasher — equals child count (fanOut or remainder size)
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that an internal hasher combining a full fan-out group calls
    /// <c>HashCore(byte[], int, int)</c> exactly <c>fanOut + 1</c> times — once for the 0x01 domain prefix and once
    /// per child via <c>TransformBlock</c> or <c>TransformFinalBlock</c>.
    /// </summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void MonitoringAlgorithm_InternalHashers_WhenFullGroup_ShouldCallHashCoreOncePerChildPlusPrefix(int fanOut)
    {
        // Produce exactly fanOut leaves so there is one internal node covering all of them.
        int dataLength = 4 * fanOut; // fanOut complete blocks
        var hashCoreCounts = new ConcurrentBag<int>();

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();
            algo.HashFinalCalled += (sender, _) =>
                hashCoreCounts.Add(((MonitoringHashAlgorithm)sender!).HashCoreCallCount);
            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: fanOut);
        hasher.ComputeHash(MakeData(dataLength));

        Assert.HasCount(1, hashCoreCounts,
            $"Expected exactly one internal node for fanOut={fanOut} exact leaves.");
        Assert.AreEqual(fanOut + 1, hashCoreCounts.ToArray()[0],
            $"Internal hasher must call HashCore exactly {fanOut + 1} times (0x01 prefix + once per child).");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // InitializeCallCount — each instance starts with exactly one Initialize call
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that each hasher instance records exactly one <c>Initialise</c> call,
    /// confirming that the constructor invokes it once and the implementation does not
    /// reset state between operations on the same instance.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_EachHasherInstance_ShouldCallInitializeExactlyOnce()
    {
        var initCounts = new ConcurrentBag<int>();

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();

            // Capture from both paths: TryHashFinalCalled (leaf) and HashFinalCalled (internal).
            algo.TryHashFinalCalled += (sender, _) =>
                initCounts.Add(((MonitoringHashAlgorithm)sender!).InitializeCallCount);
            algo.HashFinalCalled += (sender, _) =>
                initCounts.Add(((MonitoringHashAlgorithm)sender!).InitializeCallCount);

            return algo;
        };

        // 3 leaves + 3 internal nodes = 6 instances, all should have InitializeCallCount=1.
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(12));

        foreach (int count in initCounts)
            Assert.AreEqual(1, count,
                "Every hasher instance must call Initialize exactly once (from the constructor).");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Incremental sum correctness — byte accumulation matches expected root
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the hash values produced by the level workers — captured via
    /// <c>TryHashFinalCalled</c> and <c>HashFinalCalled</c> — combine to the expected root
    /// computed by the hand-verified additive formula, confirming that the byte accumulation
    /// at every stage is correct.
    /// </summary>
    [TestMethod]
    [DataRow(4, 2, 8)]   // 2 full blocks
    [DataRow(4, 2, 5)]   // 1 full + 1 partial
    [DataRow(4, 2, 12)]  // 3 full blocks (uneven reduction)
    [DataRow(4, 3, 12)]  // 3 full blocks, single group with fanOut=3
    [DataRow(4, 2, 16)]  // 4 full blocks (perfect binary tree)
    public void MonitoringAlgorithm_IncrementalByteSum_ShouldProduceExpectedRootHash(
        int blockSize, int fanOut, int dataLength)
    {
        byte[] data = MakeData(dataLength);
        byte[] expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        byte[] actual = hasher.ComputeHash(data);

        CollectionAssert.AreEqual(expected, actual,
            $"Incremental sum mismatch: blockSize={blockSize}, fanOut={fanOut}, " +
            $"dataLength={dataLength}. Expected={Convert.ToHexString(expected)}, " +
            $"Actual={Convert.ToHexString(actual)}");
    }

    /// <summary>
    /// Verifies the complete event-driven accumulation chain for a two-block tree: captures
    /// the raw hash output from each leaf hasher via <c>TryHashFinalCalled</c> and from the
    /// internal node hasher via <c>HashFinalCalled</c>, then confirms that the root hash
    /// returned by <c>ComputeHash</c> equals the additive hash of the two leaf hash byte arrays.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenTwoFullBlocks_RootShouldEqualAdditiveHashOfLeafHashes()
    {
        // Expected derivation (0x00 leaf prefix adds 0; 0x01 internal prefix adds 1):
        //   Data:   { 1,2,3,4, 5,6,7,8 }
        //   Leaf 0: 0x00 + sum=10 → { 10,0,0,0 }
        //   Leaf 1: 0x00 + sum=26 → { 26,0,0,0 }
        //   Root:   0x01 + sum({ 10,0,0,0, 26,0,0,0 }) = 1 + 36 = 37 → { 37,0,0,0 }
        byte[] data = [1, 2, 3, 4, 5, 6, 7, 8];

        var capturedLeafHashes = new ConcurrentBag<uint>();
        var capturedInternalSums = new ConcurrentBag<uint>();

        Func<HashAlgorithm> factory = () =>
        {
            var algo = new MonitoringHashAlgorithm();

            // Leaf path: the hash value is the additive sum of the block bytes.
            // BytesProcessed equals blockSize (4). The hash returned is sum as uint32-LE.
            algo.TryHashFinalCalled += (sender, _) =>
            {
                var m = (MonitoringHashAlgorithm)sender!;
                // At this point, BytesProcessed == blockSize and hashValue == sum of block bytes.
                // hashValue is private, so we capture BytesProcessed as a structure check and
                // verify the root-level sum independently below.
                capturedLeafHashes.Add((uint)m.BytesProcessed);
            };

            algo.HashFinalCalled += (sender, _) =>
            {
                var m = (MonitoringHashAlgorithm)sender!;
                capturedInternalSums.Add((uint)m.BytesProcessed);
            };

            return algo;
        };

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        byte[] root = hasher.ComputeHash(data);

        // Structural assertions.
        Assert.HasCount(2, capturedLeafHashes, "Expected two leaf hashers.");
        Assert.HasCount(1, capturedInternalSums, "Expected one internal node hasher.");

        // The root hash must equal the hand-computed expected value (1 + 36 with the internal-node prefix).
        byte[] expected = BitConverter.GetBytes((uint)37);
        CollectionAssert.AreEqual(expected, root,
            $"Root hash mismatch. Expected {Convert.ToHexString(expected)}, " +
            $"got {Convert.ToHexString(root)}.");
    }

    /// <summary>
    /// Verifies that a partial tail block is hashed at its actual length — no zero padding — so its leaf sum equals
    /// only its real data bytes, and the root equals the sum of all input bytes plus the single internal-node prefix.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenPartialTailBlock_ShouldHashActualLengthOnly()
    {
        // Data: { 1,2,3,4,  5 }
        //   Leaf 0: 0x00 + { 1,2,3,4 } → sum=10
        //   Leaf 1: 0x00 + { 5 }       → sum=5 (tail hashed at actual length, no padding)
        //   Root:   0x01 + { 10,0,0,0, 5,0,0,0 } = 1 + 15 = 16 → { 16,0,0,0 }
        byte[] data = [1, 2, 3, 4, 5];
        byte[] expected = BitConverter.GetBytes((uint)16);

        int totalInputByteSum = 1 + 2 + 3 + 4 + 5;

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        byte[] root = hasher.ComputeHash(data);

        // The root value (as uint32-LE) must equal the sum of the input bytes plus the one internal-node prefix.
        uint rootValue = BitConverter.ToUInt32(root);
        Assert.AreEqual((uint)(totalInputByteSum + MerkleTreeFormat.InternalNodePrefix), rootValue,
            $"Root value {rootValue} should equal the input-byte sum {totalInputByteSum} plus the 0x01 prefix. " +
            "The tail must be hashed at its actual length without padding.");

        CollectionAssert.AreEqual(expected, root);
    }

    /// <summary>
    /// Verifies that the <c>DisposeCalled</c> event fires for every hasher created during the
    /// computation, confirming that all factory-produced instances are disposed exactly once.
    /// </summary>
    [TestMethod]
    public void MonitoringAlgorithm_WhenComputationComplete_AllInstancesShouldBeDisposed()
    {
        int factoryCallCount = 0;
        int disposeCallCount = 0;

        Func<HashAlgorithm> factory = () =>
        {
            Interlocked.Increment(ref factoryCallCount);
            var algo = new MonitoringHashAlgorithm();
            algo.DisposeCalled += (_, _) => Interlocked.Increment(ref disposeCallCount);
            return algo;
        };

        // 3 leaves + 3 internal nodes = 6 factory calls.
        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(12));

        Assert.AreEqual(factoryCallCount, disposeCallCount,
            $"Every created hasher instance ({factoryCallCount}) must be disposed exactly once. " +
            $"Dispose was called {disposeCallCount} time(s).");
    }
}
