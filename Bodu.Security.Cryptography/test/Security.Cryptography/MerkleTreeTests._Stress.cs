// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests._Stress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concurrency stress: many independent parallel instances started together must all produce correct results without
/// deadlock, with and without recorders attached.
/// </summary>
public partial class MerkleTreeTests
{
    private const int StressDeadlockTimeoutMs = 30_000;

    private static async Task<bool> CompletedInTime(Task[] tasks) =>
        await Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromMilliseconds(StressDeadlockTimeoutMs))
            .ContinueWith(t => !t.IsFaulted && !t.IsCanceled);

    /// <summary>
    /// Verifies that many parallel instances released together all reproduce the additive oracle, across shapes.
    /// </summary>
    /// <param name="parallelism">The number of concurrent instances.</param>
    /// <param name="fanOut">The fan-out.</param>
    /// <param name="dataLength">The input length in bytes.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(20, 2, 8)]
    [DataRow(20, 2, 12)]
    [DataRow(20, 3, 12)]
    [DataRow(8, 4, 10_007)]
    public async Task StressTest_WhenManyIndependentInstancesRunConcurrently_ShouldAllProduceCorrectResults(int parallelism, int fanOut, int dataLength)
    {
        int blockSize = dataLength > 1_000 ? 256 : 4;
        byte[] data = MakeData(dataLength);
        byte[] expected = ComputeAdditiveRoot(data, blockSize, fanOut);
        using var startGate = new ManualResetEventSlim(false);
        var errors = new ConcurrentBag<string>();

        Task[] tasks = Enumerable.Range(0, parallelism).Select(index => Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                byte[] result = CreateAdditiveTree(fanOut, -1).ComputeRootOfBlocks((byte[])data.Clone(), blockSize);
                if (!result.SequenceEqual(expected))
                    errors.Add($"[{index}] hash mismatch: {Convert.ToHexString(result)}");
            }
            catch (Exception ex)
            {
                errors.Add($"[{index}] {ex.GetType().Name}: {ex.Message}");
            }
        })).ToArray();
        startGate.Set();

        Assert.IsTrue(await CompletedInTime(tasks), $"not all tasks completed within {StressDeadlockTimeoutMs} ms — possible deadlock");
        Assert.IsEmpty(errors, string.Join("\n", errors));
    }

    /// <summary>
    /// Verifies that concurrent instances over distinct inputs produce independent, correct results.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task StressTest_WhenConcurrentInstancesUseDifferentInputs_ShouldProduceIndependentResults()
    {
        const int parallelism = 20;
        using var startGate = new ManualResetEventSlim(false);
        var results = new ConcurrentDictionary<int, byte[]>();

        Task[] tasks = Enumerable.Range(0, parallelism).Select(index => Task.Run(() =>
        {
            startGate.Wait();
            results[index] = CreateAdditiveTree(2, -1).ComputeRootOfBlocks(MakeData(8 + (index % 8), seed: index), 4);
        })).ToArray();
        startGate.Set();

        Assert.IsTrue(await CompletedInTime(tasks), "tasks did not complete — possible deadlock");
        Assert.HasCount(parallelism, results);
        for (int index = 0; index < parallelism; index++)
            CollectionAssert.AreEqual(ComputeAdditiveRoot(MakeData(8 + (index % 8), seed: index), 4, 2), results[index], $"instance {index}");
    }

    /// <summary>
    /// Verifies that several times the processor count of parallel instances neither deadlocks nor miscomputes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task StressTest_WhenHighConcurrency_ShouldNotDeadlockOrLivelock()
    {
        int parallelism = Math.Max(8, Environment.ProcessorCount * 4);
        byte[] data = MakeData(20);
        byte[] expected = ComputeAdditiveRoot(data, 4, 2);
        using var startGate = new ManualResetEventSlim(false);
        int faults = 0;

        Task[] tasks = Enumerable.Range(0, parallelism).Select(_ => Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                if (!CreateAdditiveTree(2, -1).ComputeRootOfBlocks((byte[])data.Clone(), 4).SequenceEqual(expected))
                    Interlocked.Increment(ref faults);
            }
            catch
            {
                Interlocked.Increment(ref faults);
            }
        })).ToArray();
        startGate.Set();

        Assert.IsTrue(await CompletedInTime(tasks), $"not all {parallelism} tasks completed within {StressDeadlockTimeoutMs} ms");
        Assert.AreEqual(0, faults);
    }

    /// <summary>
    /// Verifies that repeated waves of concurrent instances stay consistent.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task StressTest_WhenRepeatedWavesOfConcurrentInstances_ShouldProduceConsistentResults()
    {
        byte[] data = MakeData(12);
        byte[] expected = ComputeAdditiveRoot(data, 4, 2);

        for (int wave = 0; wave < 5; wave++)
        {
            byte[][] results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => CreateAdditiveTree(2, -1).ComputeRootOfBlocks((byte[])data.Clone(), 4))));

            for (int index = 0; index < results.Length; index++)
                CollectionAssert.AreEqual(expected, results[index], $"wave {wave}, instance {index}");
        }
    }

    /// <summary>
    /// Verifies that recorders attached to concurrent parallel instances each capture a validating trace.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task StressTest_WhenDiagnosticsAttachedToConcurrentInstances_ShouldAllPassValidation()
    {
        const int parallelism = 10;
        byte[] data = MakeData(12);
        using var startGate = new ManualResetEventSlim(false);
        var traces = new ConcurrentBag<MerkleTreeDiagnostics>();

        Task[] tasks = Enumerable.Range(0, parallelism).Select(_ => Task.Run(() =>
        {
            startGate.Wait();
            var diagnostics = new MerkleTreeDiagnostics();
            CreateAdditiveTree(2, -1).ComputeRootOfBlocks((byte[])data.Clone(), 4, diagnostics);
            traces.Add(diagnostics);
        })).ToArray();
        startGate.Set();

        Assert.IsTrue(await CompletedInTime(tasks), "tasks did not complete — possible deadlock");
        Assert.HasCount(parallelism, traces);
        foreach (MerkleTreeDiagnostics diagnostics in traces)
            Assert.IsTrue(diagnostics.Validate(Factory, out IReadOnlyList<string> errors), string.Join("; ", errors));
    }
}
