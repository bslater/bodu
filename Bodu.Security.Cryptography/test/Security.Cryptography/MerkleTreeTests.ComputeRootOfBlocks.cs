// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.ComputeRootOfBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.IO;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for the root-only block computations over streams, spans, memories and arrays: shape coverage against the
/// additive oracle, overload agreement, and stream behaviours.
/// </summary>
public partial class MerkleTreeTests
{
    private static MerkleTree CreateAdditiveTree(int fanOut = 2, int maxDegreeOfParallelism = 1) =>
        new(Factory, fanOut, maxDegreeOfParallelism);

    /// <summary>
    /// Verifies that several block-size, fan-out and length configurations reproduce the hand-computed additive
    /// root, on the sequential and the parallel instance.
    /// </summary>
    /// <param name="blockSize">The block size.</param>
    /// <param name="fanOut">The fan-out.</param>
    /// <param name="length">The input length in bytes.</param>
    [TestMethod]
    [DataRow(1, 2, 7)]
    [DataRow(4, 2, 8)]
    [DataRow(4, 2, 9)]
    [DataRow(4, 3, 12)]
    [DataRow(4, 4, 16)]
    [DataRow(8, 2, 25)]
    [DataRow(16, 3, 50)]
    [DataRow(3, 10, 9)]
    [DataRow(4, 100, 4)]
    [DataRow(4, 2, 4 * 64)]
    public void ComputeRootOfBlocks_WhenVariousConfigurationsUsed_ShouldMatchHandComputedRoot(int blockSize, int fanOut, int length)
    {
        byte[] data = MakeData(length);
        byte[] expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        CollectionAssert.AreEqual(expected, CreateAdditiveTree(fanOut).ComputeRootOfBlocks(data, blockSize), "sequential");
        CollectionAssert.AreEqual(expected, CreateAdditiveTree(fanOut, -1).ComputeRootOfBlocks(data, blockSize), "parallel");
    }

    /// <summary>
    /// Verifies the boundary shapes around one block and one fan-out group: a lone partial block, exactly one block,
    /// one full plus one partial, two full, and three with a promoted third.
    /// </summary>
    /// <param name="length">The input length in bytes.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(12)]
    public void ComputeRootOfBlocks_AtBlockAndGroupBoundaries_ShouldMatchHandComputedRoot(int length)
    {
        byte[] data = MakeData(length);

        CollectionAssert.AreEqual(ComputeAdditiveRoot(data, 4, 2), CreateAdditiveTree().ComputeRootOfBlocks(data, 4));
    }

    /// <summary>
    /// Verifies prime input lengths, which never align with a block or a group.
    /// </summary>
    /// <param name="length">The input length in bytes.</param>
    [TestMethod]
    [DataRow(7)]
    [DataRow(11)]
    [DataRow(13)]
    [DataRow(17)]
    [DataRow(23)]
    [DataRow(97)]
    public void ComputeRootOfBlocks_WhenInputLengthIsPrime_ShouldMatchHandComputedRoot(int length)
    {
        byte[] data = MakeData(length);

        CollectionAssert.AreEqual(ComputeAdditiveRoot(data, 4, 3), CreateAdditiveTree(3).ComputeRootOfBlocks(data, 4));
    }

    /// <summary>
    /// Verifies that a single full block's root is its leaf hash, and that all-zero and all-0xFF inputs are handled
    /// like any other.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenSingleBlockOrUniformInput_ShouldMatchHandComputedRoot()
    {
        MerkleTree tree = CreateAdditiveTree();
        byte[] single = MakeData(4);
        byte[] zeros = new byte[40];
        byte[] ones = Enumerable.Repeat((byte)0xFF, 40).ToArray();

        CollectionAssert.AreEqual(AdditiveHash(single), tree.ComputeRootOfBlocks(single, 4), "a single block's root is its leaf");
        CollectionAssert.AreEqual(ComputeAdditiveRoot(zeros, 4, 2), tree.ComputeRootOfBlocks(zeros, 4), "zeros");
        CollectionAssert.AreEqual(ComputeAdditiveRoot(ones, 4, 2), tree.ComputeRootOfBlocks(ones, 4), "0xFF");
    }

    /// <summary>
    /// Verifies that every input overload — stream, memory, span, array, the leaf-retaining computations and the
    /// accumulator — produces the same root, on the sequential and the parallel instance.
    /// </summary>
    /// <param name="length">The input length in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    [DataRow(33)]
    [DataRow(1025)]
    public void ComputeRootOfBlocks_WhenSameInputUsedAcrossOverloads_ShouldReturnIdenticalRoots(int length)
    {
        byte[] data = BlockModeInput(length);
        string expected = Hex(CreateTree().ComputeRootOfBlocks(new MemoryStream(data), VectorBlockSize));

        foreach (int degree in (int[])[1, -1])
        {
            MerkleTree tree = CreateParallelTree(degree);

            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(new MemoryStream(data), VectorBlockSize)), $"stream, degree {degree}");
            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(data.AsMemory(), VectorBlockSize)), $"memory, degree {degree}");
            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(data.AsSpan(), VectorBlockSize)), $"span, degree {degree}");
            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(data, VectorBlockSize)), $"array, degree {degree}");
            Assert.AreEqual(expected, Hex(tree.ComputeBlocked(data, VectorBlockSize).Root), $"blocked array, degree {degree}");
            Assert.AreEqual(expected, Hex(tree.ComputeBlocked(new MemoryStream(data), VectorBlockSize).Root), $"blocked stream, degree {degree}");

            using MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(VectorBlockSize);
            accumulator.Append(data);
            Assert.AreEqual(expected, Hex(accumulator.Finish()), $"accumulator, degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that mutating the input after a computation does not alter the returned root.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenInputMutatedAfterReturn_ShouldNotAffectReturnedRoot()
    {
        byte[] data = MakeData(16);
        byte[] root = CreateTree().ComputeRootOfBlocks(data, 4);
        string before = Hex(root);

        Array.Fill(data, (byte)0xAB);

        Assert.AreEqual(before, Hex(root));
        Assert.AreNotEqual(before, Hex(CreateTree().ComputeRootOfBlocks(data, 4)));
    }

    /// <summary>
    /// Verifies that a non-seekable stream and a stream delivering one byte per read produce the same root as the
    /// in-memory overload, on both instances.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenStreamIsNonSeekableOrDripsBytes_ShouldProduceTheSameRoot()
    {
        byte[] data = MakeData(13);
        string expected = Hex(CreateTree().ComputeRootOfBlocks(data, VectorBlockSize));

        foreach (int degree in (int[])[1, -1])
        {
            MerkleTree tree = CreateParallelTree(degree);

            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(new NonSeekableStream(data), VectorBlockSize)), $"non-seekable, degree {degree}");
            Assert.AreEqual(expected, Hex(tree.ComputeRootOfBlocks(new FixedChunkStream(data, chunkSize: 1), VectorBlockSize)), $"one byte per read, degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that a stream faulting mid-read propagates its <see cref="IOException" /> from the synchronous stream
    /// overloads, on both instances.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenStreamFaults_ShouldPropagateTheIOException()
    {
        foreach (int degree in (int[])[1, -1])
        {
            MerkleTree tree = CreateParallelTree(degree);

            _ = Assert.ThrowsExactly<IOException>(() => { _ = tree.ComputeRootOfBlocks(new FaultingStream(MakeData(64), throwAfterBytes: 20), VectorBlockSize); });
            _ = Assert.ThrowsExactly<IOException>(() => { _ = tree.ComputeBlocked(new FaultingStream(MakeData(64), throwAfterBytes: 20), VectorBlockSize); });
        }
    }

    /// <summary>
    /// Verifies that a null array or stream and a non-positive block size are rejected with the parameter named on
    /// the root-only overloads.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenArgumentsAreInvalid_ShouldThrowWithTheParameterNamed()
    {
        MerkleTree tree = CreateTree();

        var nullArray = Assert.ThrowsExactly<ArgumentNullException>(() => { _ = tree.ComputeRootOfBlocks((byte[])null!, 4); });
        var nullStream = Assert.ThrowsExactly<ArgumentNullException>(() => { _ = tree.ComputeRootOfBlocks((Stream)null!, 4); });
        var memoryEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = tree.ComputeRootOfBlocks(MakeData(8).AsMemory(), 0); });
        var spanEx = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { _ = tree.ComputeRootOfBlocks(MakeData(8).AsSpan(), -1); });

        Assert.AreEqual("source", nullArray.ParamName);
        Assert.AreEqual("source", nullStream.ParamName);
        Assert.AreEqual("blockSize", memoryEx.ParamName);
        Assert.AreEqual("blockSize", spanEx.ParamName);
    }

    /// <summary>
    /// Verifies that a factory that throws propagates its exception from the computation rather than from
    /// construction, since the constructor's probe is the first call.
    /// </summary>
    [TestMethod]
    public void ComputeRootOfBlocks_WhenFactoryThrowsAfterTheProbe_ShouldPropagateTheFactoryException()
    {
        int calls = 0;
        var tree = new MerkleTree(() => ++calls == 1 ? SHA256.Create() : throw new InvalidOperationException("factory failed"));

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => { _ = tree.ComputeRootOfBlocks(MakeData(8), 4); });

        Assert.AreEqual("factory failed", ex.Message);
    }
}
