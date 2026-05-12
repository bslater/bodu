// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests._Performance.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Performance tests for <see cref="ParallelMerkleTreeHash" />.
/// </summary>
/// <remarks>
/// <para>
/// These tests do not enforce strict timing constraints — they assert that results are correct
/// and that operations complete within a generous wall-clock bound. That catches hangs and
/// catastrophic regressions without making assertions that would flake on slow CI hardware.
/// </para>
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    /// <summary>
    /// Generous upper bound for any single-instance synchronous operation. A correct
    /// implementation should complete well within this on any reasonable hardware; failure
    /// indicates a deadlock, livelock, or catastrophic regression.
    /// </summary>
    private const int SingleOperationTimeoutMs = 15_000;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Throughput — correctness under load
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the parallel implementation produces a correct result for a large input
    /// within the allowed timeout.
    /// </summary>
    [TestMethod]
    [DataRow(1, 256, 2)]
    [DataRow(10, 256, 2)]
    [DataRow(100, 256, 4)]
    [DataRow(1_000, 256, 4)]
    public void Performance_WhenHashingLargeInput_ShouldCompleteWithinTimeout(
        int kilobytes, int blockSize, int fanOut)
    {
        var data = MakeData(kilobytes * 1024);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        var sw = Stopwatch.StartNew();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        var actual = hasher.ComputeHash(data);
        sw.Stop();

        TestContext.WriteLine(
            $"Hashed {kilobytes} KB in {sw.ElapsedMilliseconds} ms " +
            $"(blockSize={blockSize}, fanOut={fanOut})");

        Assert.IsTrue(sw.ElapsedMilliseconds < SingleOperationTimeoutMs,
            $"ComputeHash exceeded timeout: {sw.ElapsedMilliseconds} ms");
        CollectionAssert.AreEqual(expected, actual, "Result incorrect under performance load.");
    }

    /// <summary>
    /// Verifies that <c>ComputeHashAsync</c> completes within the allowed timeout for a large
    /// stream and produces a correct result.
    /// </summary>
    [TestMethod]
    [DataRow(1, 256, 2)]
    [DataRow(100, 256, 4)]
    [DataRow(1_000, 256, 4)]
    public async Task Performance_WhenHashingLargeStreamAsync_ShouldCompleteWithinTimeout(
        int kilobytes, int blockSize, int fanOut)
    {
        var data = MakeData(kilobytes * 1024);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        var sw = Stopwatch.StartNew();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        var actual = await hasher.ComputeHashAsync(new MemoryStream(data));
        sw.Stop();

        TestContext.WriteLine(
            $"ComputeHashAsync: {kilobytes} KB in {sw.ElapsedMilliseconds} ms " +
            $"(blockSize={blockSize}, fanOut={fanOut})");

        Assert.IsTrue(sw.ElapsedMilliseconds < SingleOperationTimeoutMs,
            $"ComputeHashAsync exceeded timeout: {sw.ElapsedMilliseconds} ms");
        CollectionAssert.AreEqual(expected, actual, "Result incorrect under async performance load.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Throughput — many sequential instances
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that constructing and computing hashes on many independent instances in
    /// sequence remains correct and completes within the timeout.
    /// </summary>
    [TestMethod]
    [DataRow(100, 4, 2, 8)]
    [DataRow(100, 4, 2, 64)]
    [DataRow(1000, 4, 2, 8)]
    public void Performance_WhenManySequentialInstances_ShouldRemainCorrectAndFinishInTime(
        int iterations, int blockSize, int fanOut, int dataLength)
    {
        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
            var result = hasher.ComputeHash(data);
            CollectionAssert.AreEqual(expected, result,
                $"Result mismatch on iteration {i}.");
        }

        sw.Stop();

        TestContext.WriteLine(
            $"{iterations} sequential instances in {sw.ElapsedMilliseconds} ms " +
            $"(blockSize={blockSize}, fanOut={fanOut}, dataLength={dataLength})");

        Assert.IsTrue(sw.ElapsedMilliseconds < SingleOperationTimeoutMs * 2,
            $"Sequential instance loop exceeded timeout: {sw.ElapsedMilliseconds} ms");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Throughput — many parallel instances
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that hashing on many independent instances concurrently produces correct
    /// results for all of them and completes within the timeout.
    /// </summary>
    [TestMethod]
    [DataRow(50, 4, 2, 8)]
    [DataRow(50, 4, 2, 64)]
    public async Task Performance_WhenManyParallelInstances_ShouldRemainCorrectAndFinishInTime(
        int parallelism, int blockSize, int fanOut, int dataLength)
    {
        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        var sw = Stopwatch.StartNew();

        var tasks = new List<Task<byte[]>>(parallelism);
        for (var i = 0; i < parallelism; i++)
        {
            // Capture loop variable for closure safety.
            var capturedData = data;
            tasks.Add(Task.Run(() =>
            {
                using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
                return hasher.ComputeHash(capturedData);
            }));
        }

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        TestContext.WriteLine(
            $"{parallelism} parallel instances in {sw.ElapsedMilliseconds} ms " +
            $"(blockSize={blockSize}, fanOut={fanOut}, dataLength={dataLength})");

        Assert.IsTrue(sw.ElapsedMilliseconds < SingleOperationTimeoutMs,
            $"Parallel instance race exceeded timeout: {sw.ElapsedMilliseconds} ms");

        for (var i = 0; i < results.Length; i++)
            CollectionAssert.AreEqual(expected, results[i],
                $"Result mismatch on parallel instance {i}.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Fan-out comparison
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that all fan-out values from 2 to 8 produce correct results on the same input,
    /// confirming that wider trees do not introduce correctness regressions.
    /// </summary>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(8)]
    public void Performance_WhenDifferentFanOutValues_ShouldAllProduceCorrectResults(int fanOut)
    {
        const int blockSize = 4;
        const int dataLength = 64;

        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        var sw = Stopwatch.StartNew();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
        var actual = hasher.ComputeHash(data);
        sw.Stop();

        TestContext.WriteLine($"fanOut={fanOut}: {sw.ElapsedMilliseconds} ms");

        CollectionAssert.AreEqual(expected, actual, $"fanOut={fanOut} produced incorrect result.");
        Assert.IsTrue(sw.ElapsedMilliseconds < SingleOperationTimeoutMs,
            $"fanOut={fanOut} exceeded timeout.");
    }
}
