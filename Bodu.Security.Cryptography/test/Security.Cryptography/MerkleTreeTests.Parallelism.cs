// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.Parallelism.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The parallelism contract: an instance created with <c>maxDegreeOfParallelism</c> other than one hashes leaves on
/// workers and must reproduce, for every entry point, exactly the roots and leaf hashes the sequential instance
/// produces.
/// </summary>
public partial class MerkleTreeTests
{
    private static readonly int[] ParallelDegrees = [-1, 2, 3, 8, 64];

    private static MerkleTree CreateParallelTree(int maxDegreeOfParallelism = -1) =>
        new(SHA256.Create, maxDegreeOfParallelism: maxDegreeOfParallelism);

    /// <summary>
    /// Verifies that a parallel instance reproduces the published RFC 6962 roots over the reference entries at every
    /// degree.
    /// </summary>
    /// <param name="kat">The entry count and the published root.</param>
    [TestMethod]
    [DynamicData(nameof(EntryModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeRoot_WhenParallel_ShouldReproduceRfc6962MerkleTreeHash(ValidKat<int, string> kat)
    {
        foreach (int degree in ParallelDegrees)
        {
            Assert.AreEqual(kat.Expected, Hex(CreateParallelTree(degree).ComputeRoot(TakeEntries(kat.Input))), $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that a parallel instance reading a stream reproduces the published block-mode roots, input lengths
    /// and leaf counts at every degree.
    /// </summary>
    /// <param name="kat">The input length and the published root.</param>
    [TestMethod]
    [DynamicData(nameof(BlockModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlocked_WhenParallelAndReadingAStream_ShouldReproduceRfc6962MerkleTreeHash(ValidKat<int, string> kat)
    {
        foreach (int degree in ParallelDegrees)
        {
            using var stream = new MemoryStream(BlockModeInput(kat.Input));
            MerkleBlockComputation computation = CreateParallelTree(degree).ComputeBlocked(stream, VectorBlockSize);

            Assert.AreEqual(kat.Expected, Hex(computation.Root), $"degree {degree}");
            Assert.AreEqual(kat.Input, computation.InputLength, $"degree {degree}");
            Assert.AreEqual(MerkleTree.BlockCount(kat.Input, VectorBlockSize), computation.LeafHashes.Count, $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that a parallel instance's bound roots match the published values.
    /// </summary>
    /// <param name="kat">The input length and the published bound root.</param>
    [TestMethod]
    [DynamicData(nameof(BoundRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlocked_WhenParallelAndBound_ShouldReproducePublishedBoundRoots(ValidKat<int, string> kat)
    {
        MerkleTree tree = CreateParallelTree(4);
        using var stream = new MemoryStream(BlockModeInput(kat.Input));

        MerkleBlockComputation computation = tree.ComputeBlocked(stream, VectorBlockSize);

        Assert.AreEqual(kat.Expected, Hex(tree.BindRoot(computation.Root, computation.InputLength)));
    }

    /// <summary>
    /// Verifies that for every leaf count up to sixty-four, with and without a tail, a parallel instance agrees with
    /// the sequential instance on the root, the input length and every leaf hash.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ComputeBlocked_WhenParallel_ShouldAgreeWithTheSequentialInstanceOnRootAndEveryLeafHash()
    {
        MerkleTree sequentialTree = CreateTree();

        for (int leafCount = 0; leafCount <= 64; leafCount++)
        {
            foreach (int tail in (int[])[0, 1])
            {
                int length = leafCount == 0 && tail == 0 ? 0 : (leafCount * VectorBlockSize) + tail;
                byte[] input = BlockModeInput(length);

                using var sequentialStream = new MemoryStream(input);
                MerkleBlockComputation sequential = sequentialTree.ComputeBlocked(sequentialStream, VectorBlockSize);

                foreach (int degree in ParallelDegrees)
                {
                    using var parallelStream = new MemoryStream(input);
                    MerkleBlockComputation parallel = CreateParallelTree(degree).ComputeBlocked(parallelStream, VectorBlockSize);

                    Assert.AreEqual(Hex(sequential.Root), Hex(parallel.Root), $"length {length} degree {degree}");
                    Assert.AreEqual(sequential.InputLength, parallel.InputLength, $"length {length} degree {degree}");
                    CollectionAssert.AreEqual(
                        sequential.LeafHashes.Select(step => Hex(step)).ToArray(),
                        parallel.LeafHashes.Select(step => Hex(step)).ToArray(),
                        $"leaf hashes at length {length} degree {degree}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that an input spanning many batches — around and beyond the 256-block batch cap — reproduces the
    /// sequential root at several degrees.
    /// </summary>
    /// <param name="leafCount">The number of one-byte leaves.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1023)]
    [DataRow(1024)]
    [DataRow(1025)]
    [DataRow(4097)]
    public void ComputeBlocked_WhenParallelInputSpansManyBatches_ShouldAgreeWithTheSequentialInstance(int leafCount)
    {
        byte[] input = BlockModeInput(leafCount);

        using var sequentialStream = new MemoryStream(input);
        MerkleBlockComputation sequential = CreateTree().ComputeBlocked(sequentialStream, blockSize: 1);

        foreach (int degree in (int[])[-1, 2, 8])
        {
            using var parallelStream = new MemoryStream(input);
            MerkleBlockComputation parallel = CreateParallelTree(degree).ComputeBlocked(parallelStream, blockSize: 1);

            Assert.AreEqual(leafCount, parallel.LeafHashes.Count, $"degree {degree}");
            Assert.AreEqual(Hex(sequential.Root), Hex(parallel.Root), $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that a parallel instance over a memory buffer reproduces the published block-mode roots at every
    /// degree.
    /// </summary>
    /// <param name="kat">The input length and the published root.</param>
    [TestMethod]
    [DynamicData(nameof(BlockModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlocked_WhenParallelAndGivenAMemoryBuffer_ShouldReproduceRfc6962MerkleTreeHash(ValidKat<int, string> kat)
    {
        foreach (int degree in ParallelDegrees)
        {
            MerkleBlockComputation computation = CreateParallelTree(degree).ComputeBlocked(BlockModeInput(kat.Input).AsMemory(), VectorBlockSize);

            Assert.AreEqual(kat.Expected, Hex(computation.Root), $"degree {degree}");
            Assert.AreEqual(kat.Input, computation.InputLength, $"degree {degree}");
            Assert.AreEqual(MerkleTree.BlockCount(kat.Input, VectorBlockSize), computation.LeafHashes.Count, $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that the memory, span, array and stream overloads of a parallel instance all agree with the sequential
    /// span overload, root and leaf hashes alike.
    /// </summary>
    /// <param name="length">The input length in bytes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(9)]
    [DataRow(17)]
    [DataRow(28)]
    [DataRow(33)]
    [DataRow(257)]
    public void ComputeBlocked_WhenParallel_ShouldAgreeAcrossEveryOverload(int length)
    {
        MerkleTree parallel = CreateParallelTree(4);
        byte[] input = BlockModeInput(length);

        MerkleBlockComputation sequentialSpan = CreateTree().ComputeBlocked(input.AsSpan(), VectorBlockSize);
        MerkleBlockComputation parallelMemory = parallel.ComputeBlocked(input.AsMemory(), VectorBlockSize);
        MerkleBlockComputation parallelSpan = parallel.ComputeBlocked(input.AsSpan(), VectorBlockSize);
        MerkleBlockComputation parallelArray = parallel.ComputeBlocked(input, VectorBlockSize);

        using var stream = new MemoryStream(input);
        MerkleBlockComputation parallelStream = parallel.ComputeBlocked(stream, VectorBlockSize);

        string expected = Hex(sequentialSpan.Root);
        Assert.AreEqual(expected, Hex(parallelMemory.Root), "memory");
        Assert.AreEqual(expected, Hex(parallelSpan.Root), "span");
        Assert.AreEqual(expected, Hex(parallelArray.Root), "array");
        Assert.AreEqual(expected, Hex(parallelStream.Root), "stream");
        Assert.AreEqual(expected, Hex(parallel.ComputeRootOfBlocks(input.AsMemory(), VectorBlockSize)), "root-only memory");
        Assert.AreEqual(expected, Hex(parallel.ComputeRootOfBlocks(input.AsSpan(), VectorBlockSize)), "root-only span");
        Assert.AreEqual(sequentialSpan.InputLength, parallelMemory.InputLength);

        CollectionAssert.AreEqual(
            sequentialSpan.LeafHashes.Select(step => Hex(step)).ToArray(),
            parallelMemory.LeafHashes.Select(step => Hex(step)).ToArray(),
            "leaf hashes must match the sequential span path exactly");
    }

    /// <summary>
    /// Verifies that a parallel instance topped up from a stream returning short reads produces the same root.
    /// </summary>
    [TestMethod]
    public void ComputeBlocked_WhenParallelAndStreamReturnsShortReads_ShouldProduceTheSameRoot()
    {
        using var throttled = new ThrottledStream(BlockModeInput(33), maxBytesPerRead: 3);
        MerkleBlockComputation computation = CreateParallelTree(4).ComputeBlocked(throttled, VectorBlockSize);

        Assert.AreEqual("31dce6ff9c5ac1336d203f99ea214a31eaba3429a3316fdc7eb7dfa1e787e9ec", Hex(computation.Root));
        Assert.AreEqual(33, computation.InputLength);
        Assert.AreEqual(9, computation.LeafHashes.Count);
    }

    /// <summary>
    /// Verifies that a parallel instance reproduces the published FallbackPlan bound roots at one-mebibyte blocks.
    /// </summary>
    /// <param name="kat">The input length and the published bound root.</param>
    [TestMethod]
    [DynamicData(nameof(WholePreimageBoundRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlocked_WhenParallelAndBlockSizeIsOneMebibyte_ShouldReproducePublishedBoundRoots(ValidKat<int, string> kat)
    {
        MerkleTree tree = CreateParallelTree();
        using var stream = new MemoryStream(CounterStream(kat.Input));

        MerkleBlockComputation computation = tree.ComputeBlocked(stream, OneMebibyteBlock);

        Assert.AreEqual(kat.Input, computation.InputLength);
        Assert.AreEqual(kat.Expected, Hex(tree.BindRoot(computation.Root, computation.InputLength)));
    }

    /// <summary>
    /// Verifies that leaf hashes produced by a parallel instance build paths that verify against its bound root.
    /// </summary>
    [TestMethod]
    public void ComputeBlocked_WhenParallelLeafHashesAreUsedForAPath_ShouldVerifyAgainstItsRoot()
    {
        MerkleTree tree = CreateParallelTree(3);
        byte[] input = BlockModeInput(17);
        using var stream = new MemoryStream(input);

        MerkleBlockComputation computation = tree.ComputeBlocked(stream, VectorBlockSize);
        byte[] boundRoot = tree.BindRoot(computation.Root, computation.InputLength);

        for (long blockIndex = 0; blockIndex < computation.LeafHashes.Count; blockIndex++)
        {
            byte[][] path = tree.AuthenticationPath(computation.LeafHashes, blockIndex);
            int offset = (int)MerkleTree.BlockOffset(blockIndex, VectorBlockSize);
            int length = MerkleTree.BlockLength(input.Length, blockIndex, VectorBlockSize);

            Assert.IsTrue(
                tree.VerifyBlockInclusion(boundRoot, computation.InputLength, VectorBlockSize, blockIndex, input.AsSpan(offset, length), ToPath(path)),
                $"block {blockIndex}");
        }
    }

    /// <summary>
    /// Verifies that a parallel instance over a 64-byte digest agrees with the sequential instance.
    /// </summary>
    [TestMethod]
    public void ComputeBlocked_WhenParallelAndAlgorithmIsSha512_ShouldAgreeWithTheSequentialInstance()
    {
        byte[] input = BlockModeInput(37);
        using var sequentialStream = new MemoryStream(input);
        using var parallelStream = new MemoryStream(input);

        MerkleBlockComputation sequential = new MerkleTree(SHA512.Create).ComputeBlocked(sequentialStream, VectorBlockSize);
        MerkleBlockComputation parallel = new MerkleTree(SHA512.Create, maxDegreeOfParallelism: 4).ComputeBlocked(parallelStream, VectorBlockSize);

        Assert.AreEqual(64, parallel.Root.Length);
        Assert.AreEqual(Hex(sequential.Root), Hex(parallel.Root));
    }

    /// <summary>
    /// Verifies that one parallel instance shared across threads produces consistent roots.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ComputeBlocked_WhenParallelInstanceIsSharedAcrossThreads_ShouldProduceConsistentRoots()
    {
        MerkleTree tree = CreateParallelTree(2);
        byte[] input = BlockModeInput(33);

        using var reference = new MemoryStream(input);
        string expected = Hex(CreateTree().ComputeBlocked(reference, VectorBlockSize).Root);

        string[] results = new string[32];
        Parallel.For(0, results.Length, index =>
        {
            using var stream = new MemoryStream(input);
            results[index] = Hex(tree.ComputeBlocked(stream, VectorBlockSize).Root);
        });

        CollectionAssert.AreEqual(Enumerable.Repeat(expected, results.Length).ToArray(), results);
    }

    /// <summary>
    /// Verifies that a parallel instance rejects a null stream and a non-positive block size with the parameter
    /// named, on every overload.
    /// </summary>
    [TestMethod]
    public void ComputeBlocked_WhenParallelArgumentsAreInvalid_ShouldThrowWithTheParameterNamed()
    {
        MerkleTree tree = CreateParallelTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => { _ = tree.ComputeBlocked((Stream)null!, 4); });
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => { _ = tree.ComputeBlocked((byte[])null!, 4); });

        var streamEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = tree.ComputeBlocked(new MemoryStream(BlockModeInput(8)), 0); });
        var memoryEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = tree.ComputeBlocked(BlockModeInput(8).AsMemory(), 0); });
        var spanEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = tree.ComputeBlocked(BlockModeInput(8).AsSpan(), 0); });

        Assert.AreEqual("blockSize", streamEx.ParamName);
        Assert.AreEqual("blockSize", memoryEx.ParamName);
        Assert.AreEqual("blockSize", spanEx.ParamName);
    }

    /// <summary>
    /// Verifies that a parallel instance observes cancellation, from a stream and from a buffer alike, as a plain
    /// <see cref="OperationCanceledException" />.
    /// </summary>
    [TestMethod]
    public void ComputeBlocked_WhenParallelAndCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        MerkleTree tree = CreateParallelTree(4);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = Assert.ThrowsExactly<OperationCanceledException>(() =>
        {
            _ = tree.ComputeBlocked(new MemoryStream(BlockModeInput(256)), VectorBlockSize, cancellationToken: cts.Token);
        });
        _ = Assert.ThrowsExactly<OperationCanceledException>(() =>
        {
            _ = tree.ComputeBlocked(BlockModeInput(256).AsMemory(), VectorBlockSize, cancellationToken: cts.Token);
        });
        _ = Assert.ThrowsExactly<OperationCanceledException>(() =>
        {
            _ = tree.ComputeRoot(BlockModeEntries(256), cancellationToken: cts.Token);
        });
    }

    /// <summary>
    /// Verifies that a leaf algorithm faulting inside a parallel worker surfaces its own exception rather than an
    /// <see cref="AggregateException" />.
    /// </summary>
    [TestMethod]
    public void ComputeBlocked_WhenParallelAndTheLeafAlgorithmFaults_ShouldSurfaceTheFaultItself()
    {
        var tree = new MerkleTree(() => new FaultingHashAlgorithm(faultOnPayloadLength: 1 + VectorBlockSize, static () => { }), maxDegreeOfParallelism: -1);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = tree.ComputeBlocked(BlockModeInput(64).AsMemory(), VectorBlockSize);
        });

        Assert.AreEqual("Hashing failed.", ex.Message);
    }
}
