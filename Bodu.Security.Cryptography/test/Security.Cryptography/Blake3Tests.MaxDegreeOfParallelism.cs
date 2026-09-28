// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.MaxDegreeOfParallelism.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that the parameterless constructor hashes on the calling thread alone.
    /// </summary>
    [TestMethod]
    public void MaxDegreeOfParallelism_WhenDefaultConstructed_ShouldBeOne()
    {
        using var hasher = new Blake3();

        Assert.AreEqual(1, hasher.MaxDegreeOfParallelism);
    }

    /// <summary>
    /// Verifies that the bound passed to the constructor is the bound the instance reports.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The thread bound.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(64)]
    public void MaxDegreeOfParallelism_WhenConstructedWithBound_ShouldReturnIt(int maxDegreeOfParallelism)
    {
        using var hasher = new Blake3(maxDegreeOfParallelism);

        Assert.AreEqual(maxDegreeOfParallelism, hasher.MaxDegreeOfParallelism);
    }

    /// <summary>
    /// Verifies that a thread bound of zero or below <c>-1</c> is rejected with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The thread bound.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenMaxDegreeOfParallelismIsInvalid_ShouldThrowArgumentOutOfRangeException(int maxDegreeOfParallelism)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Blake3(maxDegreeOfParallelism);
        });

        Assert.AreEqual("maxDegreeOfParallelism", ex.ParamName);
    }

    /// <summary>
    /// Verifies that every thread bound yields the digest the calling thread alone computes, for inputs either side of
    /// the size worth dividing, of the 64 KiB parts, and of the subtrees one write divides into.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The thread bound.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(-1)]
    public void ComputeHash_WhenLengthStraddlesADivisionBoundary_ForEachThreadBound_ShouldMatchCallingThreadDigest(int maxDegreeOfParallelism)
    {
        int[] lengths =
        [
            (256 * 1024) - 1,
            256 * 1024,
            (256 * 1024) + 1,
            (320 * 1024) + 1,
            (512 * 1024) + 1,
            1024 * 1024,
            (1024 * 1024) + 1,
            (1024 * 1024) + (64 * 1024) + 12345,
            (3 * 1024 * 1024) + 7,
        ];

        using var sequential = new Blake3();
        using var parallel = new Blake3(maxDegreeOfParallelism);

        foreach (int length in lengths)
        {
            byte[] input = BuildDeterministicInput(length);

            CollectionAssert.AreEqual(sequential.ComputeHash(input), parallel.ComputeHash(input), $"{length} bytes");
        }
    }

    /// <summary>
    /// Verifies that large writes on several threads that start part-way into a chunk, or that leave subtrees open for
    /// the next write to complete, yield the digest the calling thread alone computes.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void TransformBlock_WhenLargeWritesRunOnSeveralThreads_ShouldMatchCallingThreadDigest()
    {
        int[][] patterns =
        [
            [100, 1024 * 1024, 5],
            [300 * 1024, 300 * 1024, 300 * 1024],
            [1025, 512 * 1024, 64 * 1024, 1],
        ];

        foreach (int[] pattern in patterns)
        {
            byte[] input = BuildDeterministicInput(pattern.Sum());

            using var sequential = new Blake3();
            using var parallel = new Blake3(4);
            int offset = 0;
            foreach (int length in pattern)
            {
                sequential.TransformBlock(input, offset, length, null, 0);
                parallel.TransformBlock(input, offset, length, null, 0);
                offset += length;
            }

            sequential.TransformFinalBlock([], 0, 0);
            parallel.TransformFinalBlock([], 0, 0);

            CollectionAssert.AreEqual(sequential.Hash, parallel.Hash, string.Join(", ", pattern));
        }
    }
}
