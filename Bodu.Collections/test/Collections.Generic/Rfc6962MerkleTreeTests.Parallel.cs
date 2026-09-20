// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.Parallel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Kat;

namespace Bodu.Collections.Generic;

/// <summary>
/// Tests for <see cref="Rfc6962MerkleTree.ComputeBlockedParallel(Stream, int, int, CancellationToken)" /> and
/// <see cref="Rfc6962MerkleTree.ComputeRootParallel(IReadOnlyList{ReadOnlyMemory{byte}}, int, CancellationToken)" />.
/// </summary>
/// <remarks>
/// The requirement these satisfy is that the parallel and sequential paths produce identical roots for every
/// published vector. That equality is a design property rather than a coincidence — only leaf hashing is
/// parallelized, and both paths fold the resulting leaf hashes through the same reduction — so these tests confirm
/// the property rather than being the only thing that establishes it.
/// </remarks>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>The degrees of parallelism every equivalence test is repeated across.</summary>
    private static readonly int[] ParallelDegrees = [-1, 1, 2, 3, 8, 64];

    /// <summary>
    /// Verifies that parallel entry-mode hashing reproduces every published entry-mode root (appendix A).
    /// </summary>
    /// <param name="kat">The entry count and its expected root.</param>
    [TestMethod]
    [DynamicData(nameof(EntryModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeRootParallel_WhenGivenReferenceEntries_ShouldReproduceRfc6962MerkleTreeHash(
        ValidKat<int, string> kat)
    {
        Rfc6962MerkleTree tree = CreateTree();

        foreach (int degree in ParallelDegrees)
        {
            Assert.AreEqual(
                kat.Expected,
                Hex(tree.ComputeRootParallel(TakeEntries(kat.Input), degree)),
                $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that parallel block-mode hashing reproduces every published block-mode root (appendix B), including
    /// the short final block and the zero-length input.
    /// </summary>
    /// <param name="kat">The input length and its expected root.</param>
    [TestMethod]
    [DynamicData(nameof(BlockModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlockedParallel_WhenReadingAStream_ShouldReproduceRfc6962MerkleTreeHash(
        ValidKat<int, string> kat)
    {
        Rfc6962MerkleTree tree = CreateTree();

        foreach (int degree in ParallelDegrees)
        {
            using var stream = new MemoryStream(BlockModeInput(kat.Input));
            MerkleComputation computation = tree.ComputeBlockedParallel(stream, VectorBlockSize, degree);

            Assert.AreEqual(kat.Expected, Hex(computation.Root), $"degree {degree}");
            Assert.AreEqual(kat.Input, computation.InputLength, $"degree {degree}");
            Assert.AreEqual(
                MerkleBlocks.BlockCount(kat.Input, VectorBlockSize),
                computation.LeafHashes.Count,
                $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that binding the parallel computation's length reproduces every published bound root (appendix C).
    /// </summary>
    /// <param name="kat">The input length and its expected bound root.</param>
    [TestMethod]
    [DynamicData(nameof(BoundRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlockedParallel_WhenBound_ShouldReproducePublishedBoundRoots(ValidKat<int, string> kat)
    {
        Rfc6962MerkleTree tree = CreateTree();
        using var stream = new MemoryStream(BlockModeInput(kat.Input));

        MerkleComputation computation = tree.ComputeBlockedParallel(stream, VectorBlockSize, maxDegreeOfParallelism: 4);

        Assert.AreEqual(kat.Expected, Hex(tree.BindRoot(computation.Root, computation.InputLength)));
    }

    /// <summary>
    /// Verifies that the parallel and sequential block-mode paths agree on the root, the input length and every
    /// retained leaf hash, for every leaf count from zero to sixty-four with both tail shapes.
    /// </summary>
    /// <remarks>
    /// Leaf hashes are compared individually rather than only through the root, so a batch boundary that reordered or
    /// dropped a leaf would be caught at the leaf rather than masked by a coincidence in the reduction.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    public void ComputeBlockedParallel_WhenComparedWithTheSequentialPath_ShouldAgreeOnRootAndEveryLeafHash()
    {
        Rfc6962MerkleTree tree = CreateTree();

        for (int leafCount = 0; leafCount <= 64; leafCount++)
        {
            foreach (int tail in (int[])[0, 1])
            {
                int length = leafCount == 0 && tail == 0 ? 0 : (leafCount * VectorBlockSize) + tail;
                byte[] input = BlockModeInput(length);

                using var sequentialStream = new MemoryStream(input);
                MerkleComputation sequential = tree.ComputeBlocked(sequentialStream, VectorBlockSize);

                foreach (int degree in ParallelDegrees)
                {
                    using var parallelStream = new MemoryStream(input);
                    MerkleComputation parallel = tree.ComputeBlockedParallel(parallelStream, VectorBlockSize, degree);

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
    /// Verifies that the parallel path agrees with the sequential one when the input spans many batches, so the batch
    /// boundary itself is exercised rather than only inputs that fit in one.
    /// </summary>
    /// <param name="leafCount">The number of one-byte blocks, and so of leaves.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1023)]
    [DataRow(1024)]
    [DataRow(1025)]
    [DataRow(4097)]
    public void ComputeBlockedParallel_WhenInputSpansManyBatches_ShouldAgreeWithTheSequentialPath(int leafCount)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(leafCount);

        using var sequentialStream = new MemoryStream(input);
        MerkleComputation sequential = tree.ComputeBlocked(sequentialStream, blockSize: 1);

        foreach (int degree in (int[])[-1, 2, 8])
        {
            using var parallelStream = new MemoryStream(input);
            MerkleComputation parallel = tree.ComputeBlockedParallel(parallelStream, blockSize: 1, degree);

            Assert.AreEqual(leafCount, parallel.LeafHashes.Count, $"degree {degree}");
            Assert.AreEqual(Hex(sequential.Root), Hex(parallel.Root), $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that the in-memory parallel overload reproduces every published block-mode root (appendix B).
    /// </summary>
    /// <param name="kat">The input length and its expected root.</param>
    [TestMethod]
    [DynamicData(nameof(BlockModeRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlockedParallel_WhenGivenAMemoryBuffer_ShouldReproduceRfc6962MerkleTreeHash(
        ValidKat<int, string> kat)
    {
        Rfc6962MerkleTree tree = CreateTree();

        foreach (int degree in ParallelDegrees)
        {
            MerkleComputation computation =
                tree.ComputeBlockedParallel(BlockModeInput(kat.Input).AsMemory(), VectorBlockSize, degree);

            Assert.AreEqual(kat.Expected, Hex(computation.Root), $"degree {degree}");
            Assert.AreEqual(kat.Input, computation.InputLength, $"degree {degree}");
            Assert.AreEqual(
                MerkleBlocks.BlockCount(kat.Input, VectorBlockSize),
                computation.LeafHashes.Count,
                $"degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that the in-memory parallel overload, the stream parallel overload and the sequential span overload
    /// all agree on the root and on every retained leaf hash.
    /// </summary>
    /// <param name="length">The number of input bytes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(9)]
    [DataRow(17)]
    [DataRow(28)]
    [DataRow(33)]
    [DataRow(257)]
    public void ComputeBlockedParallel_WhenGivenAMemoryBuffer_ShouldAgreeWithEveryOtherPath(int length)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(length);

        MerkleComputation sequentialSpan = tree.ComputeBlocked(input.AsSpan(), VectorBlockSize);
        MerkleComputation parallelMemory = tree.ComputeBlockedParallel(input.AsMemory(), VectorBlockSize, 4);

        using var stream = new MemoryStream(input);
        MerkleComputation parallelStream = tree.ComputeBlockedParallel(stream, VectorBlockSize, 4);

        Assert.AreEqual(Hex(sequentialSpan.Root), Hex(parallelMemory.Root), "memory parallel vs sequential span");
        Assert.AreEqual(Hex(sequentialSpan.Root), Hex(parallelStream.Root), "stream parallel vs sequential span");
        Assert.AreEqual(sequentialSpan.InputLength, parallelMemory.InputLength);

        CollectionAssert.AreEqual(
            sequentialSpan.LeafHashes.Select(step => Hex(step)).ToArray(),
            parallelMemory.LeafHashes.Select(step => Hex(step)).ToArray(),
            "leaf hashes must match the sequential span path exactly");
    }

    /// <summary>
    /// Verifies that the in-memory overload rejects an invalid degree of parallelism and a non-positive block size.
    /// </summary>
    [TestMethod]
    public void ComputeBlockedParallel_WhenMemoryOverloadArgumentsAreInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        Rfc6962MerkleTree tree = CreateTree();
        ReadOnlyMemory<byte> input = BlockModeInput(16).AsMemory();

        var degreeEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.ComputeBlockedParallel(input, VectorBlockSize, 0);
        });
        Assert.AreEqual("maxDegreeOfParallelism", degreeEx.ParamName);

        var blockEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.ComputeBlockedParallel(input, 0);
        });
        Assert.AreEqual("blockSize", blockEx.ParamName);
    }

    /// <summary>
    /// Verifies that the parallel path tolerates a stream that returns short reads, so a batch is filled from however
    /// many reads it takes rather than assuming one read per block.
    /// </summary>
    [TestMethod]
    public void ComputeBlockedParallel_WhenStreamReturnsShortReads_ShouldProduceTheSameRoot()
    {
        Rfc6962MerkleTree tree = CreateTree();

        using var throttled = new ThrottledStream(BlockModeInput(33), maxBytesPerRead: 3);
        MerkleComputation computation = tree.ComputeBlockedParallel(throttled, VectorBlockSize, maxDegreeOfParallelism: 4);

        Assert.AreEqual("31dce6ff9c5ac1336d203f99ea214a31eaba3429a3316fdc7eb7dfa1e787e9ec", Hex(computation.Root));
        Assert.AreEqual(33, computation.InputLength);
        Assert.AreEqual(9, computation.LeafHashes.Count);
    }

    /// <summary>
    /// Verifies that the parallel path reproduces the published one-mebibyte bound roots, the leaf size at which
    /// parallel hashing is actually worth using.
    /// </summary>
    /// <param name="kat">The preimage length and its expected bound root.</param>
    [TestMethod]
    [DynamicData(nameof(WholePreimageBoundRoots), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void ComputeBlockedParallel_WhenBlockSizeIsOneMebibyte_ShouldReproducePublishedBoundRoots(
        ValidKat<int, string> kat)
    {
        Rfc6962MerkleTree tree = CreateTree();
        using var stream = new MemoryStream(CounterStream(kat.Input));

        MerkleComputation computation = tree.ComputeBlockedParallel(stream, OneMebibyteBlock);

        Assert.AreEqual(kat.Input, computation.InputLength);
        Assert.AreEqual(kat.Expected, Hex(tree.BindRoot(computation.Root, computation.InputLength)));
    }

    /// <summary>
    /// Verifies that a path built from the parallel computation's leaf hashes verifies against its root, so the
    /// parallel path is usable for the possession challenge it exists to speed up.
    /// </summary>
    [TestMethod]
    public void ComputeBlockedParallel_WhenLeafHashesAreUsedForAPath_ShouldVerifyAgainstItsRoot()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(17);
        using var stream = new MemoryStream(input);

        MerkleComputation computation = tree.ComputeBlockedParallel(stream, VectorBlockSize, maxDegreeOfParallelism: 3);
        byte[] boundRoot = tree.BindRoot(computation.Root, computation.InputLength);

        for (long blockIndex = 0; blockIndex < computation.LeafHashes.Count; blockIndex++)
        {
            byte[][] path = tree.AuthenticationPath(computation.LeafHashes, blockIndex);
            int offset = (int)MerkleBlocks.BlockOffset(blockIndex, VectorBlockSize);
            int length = MerkleBlocks.BlockLength(input.Length, blockIndex, VectorBlockSize);

            Assert.IsTrue(
                tree.VerifyBlockInclusion(
                    boundRoot, computation.InputLength, VectorBlockSize, blockIndex,
                    input.AsSpan(offset, length), ToPath(path)),
                $"block {blockIndex}");
        }
    }

    /// <summary>
    /// Verifies that a SHA-512 tree parallelizes correctly, confirming the parallel path makes no assumption about
    /// digest width either.
    /// </summary>
    [TestMethod]
    public void ComputeBlockedParallel_WhenAlgorithmIsSha512_ShouldAgreeWithTheSequentialPath()
    {
        var tree = new Rfc6962MerkleTree(SHA512.Create);
        byte[] input = BlockModeInput(37);

        using var sequentialStream = new MemoryStream(input);
        using var parallelStream = new MemoryStream(input);

        MerkleComputation sequential = tree.ComputeBlocked(sequentialStream, VectorBlockSize);
        MerkleComputation parallel = tree.ComputeBlockedParallel(parallelStream, VectorBlockSize, 4);

        Assert.AreEqual(64, parallel.Root.Length);
        Assert.AreEqual(Hex(sequential.Root), Hex(parallel.Root));
    }

    /// <summary>
    /// Verifies that concurrent parallel computations on one shared instance agree with each other, since the type is
    /// documented as safe to share and the parallel path creates algorithms per worker.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ComputeBlockedParallel_WhenInstanceIsSharedAcrossThreads_ShouldProduceConsistentRoots()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] input = BlockModeInput(33);

        using var reference = new MemoryStream(input);
        string expected = Hex(tree.ComputeBlocked(reference, VectorBlockSize).Root);

        string[] results = new string[32];
        Parallel.For(0, results.Length, index =>
        {
            using var stream = new MemoryStream(input);
            results[index] = Hex(tree.ComputeBlockedParallel(stream, VectorBlockSize, maxDegreeOfParallelism: 2).Root);
        });

        CollectionAssert.AreEqual(Enumerable.Repeat(expected, results.Length).ToArray(), results);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> stream is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ComputeBlockedParallel_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        Rfc6962MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.ComputeBlockedParallel((Stream)null!, 4);
        });
    }

    /// <summary>
    /// Verifies that an invalid degree of parallelism is rejected with
    /// <see cref="ArgumentOutOfRangeException" />, while <c>-1</c> is accepted as "use the processor count".
    /// </summary>
    /// <param name="degree">The invalid degree.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    [DataRow(int.MinValue)]
    public void ComputeBlockedParallel_WhenDegreeOfParallelismIsInvalid_ShouldThrowArgumentOutOfRangeException(int degree)
    {
        Rfc6962MerkleTree tree = CreateTree();
        using var stream = new MemoryStream(BlockModeInput(8));

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.ComputeBlockedParallel(stream, VectorBlockSize, degree);
        });

        Assert.AreEqual("maxDegreeOfParallelism", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid degree of parallelism is rejected by the entry-mode overload too.
    /// </summary>
    [TestMethod]
    public void ComputeRootParallel_WhenDegreeOfParallelismIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        Rfc6962MerkleTree tree = CreateTree();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.ComputeRootParallel(TakeEntries(4), 0);
        });

        Assert.AreEqual("maxDegreeOfParallelism", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a non-positive block size is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ComputeBlockedParallel_WhenBlockSizeIsNotPositive_ShouldThrowArgumentOutOfRangeException()
    {
        Rfc6962MerkleTree tree = CreateTree();
        using var stream = new MemoryStream(BlockModeInput(8));

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = tree.ComputeBlockedParallel(stream, 0);
        });

        Assert.AreEqual("blockSize", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a cancelled token stops the computation rather than completing it.
    /// </summary>
    [TestMethod]
    public void ComputeBlockedParallel_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        Rfc6962MerkleTree tree = CreateTree();
        using var stream = new MemoryStream(BlockModeInput(256));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = Assert.ThrowsExactly<OperationCanceledException>(() =>
        {
            _ = tree.ComputeBlockedParallel(stream, VectorBlockSize, 4, cts.Token);
        });
    }
}
