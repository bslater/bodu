// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3CoreTests.CompressSubtrees.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Blake3CoreTests
{
    /// <summary>
    /// Gets the subtree plans the parallel tests divide among threads: a single large subtree, the descending run a
    /// one-shot write makes, the ascending run that completes open subtrees, and plans at and below the size worth
    /// dividing.
    /// </summary>
    private static IEnumerable<int[]> SubtreePlans =>
    [
        [512],
        [256, 128, 64, 32, 16, 8, 4, 2, 1],
        [1, 2, 4, 8, 16, 32, 64, 128, 256],
        [64, 64, 64, 64],
        [128, 64, 32, 16, 8, 4, 2, 1],
        [2, 1],
    ];

    /// <summary>
    /// Verifies that dividing consecutive subtrees among threads yields, for each, the chaining value computing it on
    /// its own does, for every thread bound.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The thread bound.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(-1)]
    public void CompressSubtrees_WhenDividedAmongThreads_ForEachThreadBound_ShouldMatchSubtreeBySubtree(int maxDegreeOfParallelism)
    {
        var random = new Random(0x3F3F);

        foreach (int[] plan in SubtreePlans)
        {
            byte[] input = new byte[plan.Sum() * Blake3Core.ChunkBytes];
            random.NextBytes(input);
            uint[] key = RandomKey(random);
            ulong counter = (ulong)plan[0] * 7;

            byte[] expected = new byte[plan.Length * Blake3Core.ChainingValueBytes];
            ulong subtreeCounter = counter;
            int offset = 0;
            for (int subtree = 0; subtree < plan.Length; subtree++)
            {
                uint[] chainingValue = new uint[Blake3Core.ChainingValueWords];
                Blake3Core.CompressSubtree(input.AsSpan(offset, plan[subtree] * Blake3Core.ChunkBytes), key, subtreeCounter, Blake3Core.KeyedHash, chainingValue);
                Encode(chainingValue).CopyTo(expected, subtree * Blake3Core.ChainingValueBytes);

                subtreeCounter += (ulong)plan[subtree];
                offset += plan[subtree] * Blake3Core.ChunkBytes;
            }

            byte[] actual = new byte[expected.Length];
            Blake3Core.CompressSubtrees(input, plan, key, counter, Blake3Core.KeyedHash, maxDegreeOfParallelism, actual);

            CollectionAssert.AreEqual(expected, actual, string.Join(", ", plan));
        }
    }

    /// <summary>
    /// Verifies that each kernel divided among threads yields the chaining values the scalar kernel computes on the
    /// calling thread.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void CompressSubtrees_WhenDividedAmongThreads_ForEachKernel_ShouldMatchScalarKernelOnOneThread(string kernel)
    {
        Blake3Core.KernelKind kind = ParseSupportedKernel(kernel);
        int[] plan = [256, 128, 64, 1];
        byte[] input = new byte[plan.Sum() * Blake3Core.ChunkBytes];
        new Random(0x3F40).NextBytes(input);

        byte[] expected = new byte[plan.Length * Blake3Core.ChainingValueBytes];
        byte[] actual = new byte[expected.Length];
        Blake3Core.CompressSubtrees(Blake3Core.KernelKind.Scalar, input, plan, Blake3Core.InitializationVector, 1024, 0, 1, expected);
        Blake3Core.CompressSubtrees(kind, input, plan, Blake3Core.InitializationVector, 1024, 0, 4, actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a subtree size that is not a power of two is rejected with <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="chunks">The subtree size.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(-4)]
    public void CompressSubtrees_WhenSubtreeChunksIsNotAPowerOfTwo_ShouldThrowArgumentException(int chunks)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Blake3Core.CompressSubtrees(new byte[Math.Max(0, chunks) * Blake3Core.ChunkBytes], [chunks], Blake3Core.InitializationVector.ToArray(), 0, 0, 1, new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("subtreeChunks", ex.ParamName);
    }

    /// <summary>
    /// Verifies that subtree sizes that do not add up to the input are rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CompressSubtrees_WhenSubtreesDoNotCoverInput_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Blake3Core.CompressSubtrees(new byte[3 * Blake3Core.ChunkBytes], [2], Blake3Core.InitializationVector.ToArray(), 0, 0, 1, new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("input", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a key shorter than eight words is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void CompressSubtrees_WhenKeyIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressSubtrees(new byte[Blake3Core.ChunkBytes], [1], new uint[7], 0, 0, 1, new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that room for fewer chaining values than subtrees is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> rather than written past.
    /// </summary>
    [TestMethod]
    public void CompressSubtrees_WhenChainingValuesIsShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressSubtrees(new byte[3 * Blake3Core.ChunkBytes], [2, 1], Blake3Core.InitializationVector.ToArray(), 0, 0, 1, new byte[(2 * Blake3Core.ChainingValueBytes) - 1]);
        });

        Assert.AreEqual("chainingValues", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a thread bound of zero or below <c>-1</c> is rejected with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The thread bound.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void CompressSubtrees_WhenMaxDegreeOfParallelismIsInvalid_ShouldThrowArgumentOutOfRangeException(int maxDegreeOfParallelism)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Blake3Core.CompressSubtrees(new byte[Blake3Core.ChunkBytes], [1], Blake3Core.InitializationVector.ToArray(), 0, 0, maxDegreeOfParallelism, new byte[Blake3Core.ChainingValueBytes]);
        });

        Assert.AreEqual("maxDegreeOfParallelism", ex.ParamName);
    }
}
