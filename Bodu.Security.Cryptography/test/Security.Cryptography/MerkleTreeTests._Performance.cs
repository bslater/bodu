// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests._Performance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Correctness under load with an elapsed-time ceiling: large inputs, many instances, every fan-out.
/// </summary>
public partial class MerkleTreeTests
{
    private const int SingleOperationTimeoutMs = 15_000;

    /// <summary>Gets or sets the test context the performance rows report timings through.</summary>
    public TestContext TestContext { get; set; } = null!;

    /// <summary>
    /// Verifies that hashing a large input, in memory and from a stream, sequentially and in parallel, stays correct
    /// and completes within the ceiling.
    /// </summary>
    /// <param name="kilobytes">The input size in KiB.</param>
    /// <param name="blockSize">The block size.</param>
    /// <param name="fanOut">The fan-out.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(1, 256, 2)]
    [DataRow(10, 256, 2)]
    [DataRow(100, 256, 4)]
    [DataRow(1_000, 256, 4)]
    public async Task Performance_WhenHashingLargeInput_ShouldCompleteWithinTimeout(int kilobytes, int blockSize, int fanOut)
    {
        byte[] data = MakeData(kilobytes * 1024);
        byte[] expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        foreach (int degree in (int[])[1, -1])
        {
            MerkleTree tree = CreateAdditiveTree(fanOut, degree);
            var stopwatch = Stopwatch.StartNew();
            byte[] inMemory = tree.ComputeRootOfBlocks(data, blockSize);
            byte[] fromStream = await tree.ComputeRootOfBlocksAsync(new MemoryStream(data), blockSize);
            stopwatch.Stop();

            TestContext.WriteLine($"{kilobytes} KB, blockSize={blockSize}, fanOut={fanOut}, degree={degree}: {stopwatch.ElapsedMilliseconds} ms");

            Assert.IsLessThan(SingleOperationTimeoutMs, stopwatch.ElapsedMilliseconds);
            CollectionAssert.AreEqual(expected, inMemory, $"in memory, degree {degree}");
            CollectionAssert.AreEqual(expected, fromStream, $"stream, degree {degree}");
        }
    }

    /// <summary>
    /// Verifies that many instances created in sequence stay correct and finish in time.
    /// </summary>
    /// <param name="iterations">The number of instances.</param>
    /// <param name="dataLength">The input length in bytes.</param>
    [TestMethod]
    [DataRow(100, 8)]
    [DataRow(100, 64)]
    [DataRow(1000, 8)]
    public void Performance_WhenManySequentialInstances_ShouldRemainCorrectAndFinishInTime(int iterations, int dataLength)
    {
        byte[] data = MakeData(dataLength);
        byte[] expected = ComputeAdditiveRoot(data, 4, 2);

        var stopwatch = Stopwatch.StartNew();
        for (int index = 0; index < iterations; index++)
            CollectionAssert.AreEqual(expected, CreateAdditiveTree().ComputeRootOfBlocks(data, 4), $"iteration {index}");

        stopwatch.Stop();
        TestContext.WriteLine($"{iterations} instances over {dataLength} bytes in {stopwatch.ElapsedMilliseconds} ms");

        Assert.IsLessThan(SingleOperationTimeoutMs * 2, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Verifies that many parallel instances racing on the thread pool all stay correct and finish in time.
    /// </summary>
    /// <param name="parallelism">The number of concurrent instances.</param>
    /// <param name="dataLength">The input length in bytes.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(50, 8)]
    [DataRow(50, 64)]
    public async Task Performance_WhenManyParallelInstances_ShouldRemainCorrectAndFinishInTime(int parallelism, int dataLength)
    {
        byte[] data = MakeData(dataLength);
        byte[] expected = ComputeAdditiveRoot(data, 4, 2);

        var stopwatch = Stopwatch.StartNew();
        byte[][] results = await Task.WhenAll(Enumerable.Range(0, parallelism).Select(_ => Task.Run(() => CreateAdditiveTree(2, -1).ComputeRootOfBlocks(data, 4))));
        stopwatch.Stop();

        TestContext.WriteLine($"{parallelism} parallel instances in {stopwatch.ElapsedMilliseconds} ms");

        Assert.IsLessThan(SingleOperationTimeoutMs, stopwatch.ElapsedMilliseconds);
        for (int index = 0; index < results.Length; index++)
            CollectionAssert.AreEqual(expected, results[index], $"instance {index}");
    }

    /// <summary>
    /// Verifies that every fan-out from two to eight reproduces the additive oracle.
    /// </summary>
    /// <param name="fanOut">The fan-out.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(8)]
    public void Performance_WhenDifferentFanOutValues_ShouldAllProduceCorrectResults(int fanOut)
    {
        byte[] data = MakeData(64);

        CollectionAssert.AreEqual(ComputeAdditiveRoot(data, 4, fanOut), CreateAdditiveTree(fanOut).ComputeRootOfBlocks(data, 4));
        CollectionAssert.AreEqual(ComputeAdditiveRoot(data, 4, fanOut), CreateAdditiveTree(fanOut, -1).ComputeRootOfBlocks(data, 4));
    }
}
