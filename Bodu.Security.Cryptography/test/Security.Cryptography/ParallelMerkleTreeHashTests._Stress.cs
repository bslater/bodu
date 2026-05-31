// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests._Stress.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Stress tests for <see cref="ParallelMerkleTreeHash" />. These tests run many independent
/// instances concurrently to verify that instances are genuinely isolated — sharing a factory
/// but not algorithm state, channels, or buffers — and that results are deterministic and
/// correct under concurrent load.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ParallelMerkleTreeHash" /> is a single-instance, single-use class. Concurrent
/// stress is achieved by running many independent instances simultaneously, not by calling a
/// single instance from multiple threads. The factory function is the only shared resource; it
/// must be safe to invoke from multiple threads simultaneously.
/// </para>
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    /// <summary>
    /// Generous upper bound for the entire stress run. A correct implementation finishes well
    /// within this on any reasonable hardware; exceeding it indicates a deadlock, livelock, or
    /// a catastrophic regression.
    /// </summary>
    private const int StressDeadlockTimeoutMs = 30_000;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Concurrent independent instances — correctness invariant
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that many independent instances running concurrently each produce the correct
    /// root hash, confirming that instances do not share mutable state.
    /// </summary>
    [TestMethod]
    [DataRow(20, 4, 2, 8)]
    [DataRow(20, 4, 2, 12)]
    [DataRow(20, 4, 3, 12)]
    public async Task StressTest_WhenManyIndependentInstancesRunConcurrently_ShouldAllProduceCorrectResults(
        int parallelism, int blockSize, int fanOut, int dataLength)
    {
        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using var startGate = new ManualResetEventSlim(false);
        var errors = new ConcurrentBag<string>();

        Task[] tasks = Enumerable.Range(0, parallelism).Select(_ => Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
                var result = hasher.ComputeHash((byte[])data.Clone());
                if (!result.SequenceEqual(expected))
                    errors.Add($"Hash mismatch: {Convert.ToHexString(result)}");
            }
            catch (Exception ex)
            {
                errors.Add($"Exception: {ex.GetType().Name}: {ex.Message}");
            }
        })).ToArray();

        startGate.Set();

        var completed = await Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromMilliseconds(StressDeadlockTimeoutMs))
            .ContinueWith(t => !t.IsFaulted && !t.IsCanceled);

        TestContext.WriteLine(
            $"Parallelism={parallelism}, blockSize={blockSize}, fanOut={fanOut}, " +
            $"dataLength={dataLength}, Errors={errors.Count}");

        Assert.IsTrue(completed,
            $"Not all tasks completed within {StressDeadlockTimeoutMs} ms — possible deadlock.");
        Assert.AreEqual(0, errors.Count,
            $"One or more concurrent instances produced incorrect results:\n" +
            string.Join("\n", errors));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Concurrent instances — varying data per instance
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that concurrent instances with different inputs each produce independent
    /// correct results, confirming that instances do not interfere with one another.
    /// </summary>
    [TestMethod]
    public async Task StressTest_WhenConcurrentInstancesUseDifferentInputs_ShouldProduceIndependentResults()
    {
        const int parallelism = 20;
        const int blockSize = 4;
        const int fanOut = 2;

        using var startGate = new ManualResetEventSlim(false);
        var results = new ConcurrentDictionary<int, byte[]>();
        var errors = new ConcurrentBag<string>();

        Task[] tasks = Enumerable.Range(0, parallelism).Select(i => Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                var data = MakeData(8 + i % 8, seed: i); // each instance sees distinct data
                using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
                results[i] = hasher.ComputeHash(data);
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] {ex.GetType().Name}: {ex.Message}");
            }
        })).ToArray();

        startGate.Set();

        var completed = await Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromMilliseconds(StressDeadlockTimeoutMs))
            .ContinueWith(t => !t.IsFaulted && !t.IsCanceled);

        TestContext.WriteLine($"Completed={completed}, Results={results.Count}, Errors={errors.Count}");

        Assert.IsTrue(completed, "Tasks did not complete — possible deadlock.");
        Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
        Assert.AreEqual(parallelism, results.Count, "Not all instances returned a result.");

        // Cross-validate each instance's result against the hand-computed reference.
        for (var i = 0; i < parallelism; i++)
        {
            var data = MakeData(8 + i % 8, seed: i);
            var expected = ComputeAdditiveRoot(data, blockSize, fanOut);
            CollectionAssert.AreEqual(expected, results[i], $"Instance {i} produced a wrong hash.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // High parallelism — deadlock detection
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a high number of concurrent instances — scaled to
    /// <see cref="Environment.ProcessorCount" /> — all complete within the timeout, guarding
    /// against deadlock or livelock at scale.
    /// </summary>
    [TestMethod]
    public async Task StressTest_WhenHighConcurrency_ShouldNotDeadlockOrLivelock()
    {
        var parallelism = Math.Max(8, Environment.ProcessorCount * 4);
        const int blockSize = 4;
        const int fanOut = 2;
        const int dataLength = 20;

        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using var startGate = new ManualResetEventSlim(false);
        var faults = 0;

        Task[] tasks = Enumerable.Range(0, parallelism).Select(_ => Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
                var result = hasher.ComputeHash((byte[])data.Clone());
                if (!result.SequenceEqual(expected))
                    Interlocked.Increment(ref faults);
            }
            catch
            {
                Interlocked.Increment(ref faults);
            }
        })).ToArray();

        startGate.Set();

        var completed = await Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromMilliseconds(StressDeadlockTimeoutMs))
            .ContinueWith(t => !t.IsFaulted && !t.IsCanceled);

        TestContext.WriteLine(
            $"ProcessorCount={Environment.ProcessorCount}, Parallelism={parallelism}, Faults={faults}");

        Assert.IsTrue(completed,
            $"Not all {parallelism} tasks completed within {StressDeadlockTimeoutMs} ms — " +
            "possible deadlock or livelock.");
        Assert.AreEqual(0, faults,
            $"{faults} instance(s) produced incorrect results or threw exceptions.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Repeated computation wave — factory shared across waves
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that running multiple waves of concurrent instances, each wave fully completing
    /// before the next begins, produces consistent results across all waves. This tests that
    /// the shared factory has no hidden state that accumulates across invocations.
    /// </summary>
    [TestMethod]
    public async Task StressTest_WhenRepeatedWavesOfConcurrentInstances_ShouldProduceConsistentResults()
    {
        const int waves = 5;
        const int perWave = 10;
        const int blockSize = 4;
        const int fanOut = 2;
        const int dataLength = 12;

        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        for (var wave = 0; wave < waves; wave++)
        {
            Task<byte[]>[] tasks = Enumerable.Range(0, perWave).Select(_ => Task.Run(() =>
            {
                using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
                return hasher.ComputeHash((byte[])data.Clone());
            })).ToArray();

            var results = await Task.WhenAll(tasks);

            for (var i = 0; i < results.Length; i++)
                CollectionAssert.AreEqual(expected, results[i],
                    $"Wave {wave}, instance {i}: incorrect result.");

            TestContext.WriteLine($"Wave {wave} complete ({perWave} instances).");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Large-input concurrent instances
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that concurrent instances hashing large inputs — each spanning multiple tree
    /// levels — complete without error and produce correct results.
    /// </summary>
    [TestMethod]
    public async Task StressTest_WhenConcurrentInstancesHashLargeInputs_ShouldAllSucceed()
    {
        const int parallelism = 8;
        const int blockSize = 256;
        const int fanOut = 4;
        const int dataLength = 10_007; // intentionally uneven: 39 full + 1 partial block

        var data = MakeData(dataLength);
        var expected = ComputeAdditiveRoot(data, blockSize, fanOut);

        using var startGate = new ManualResetEventSlim(false);
        var errors = new ConcurrentBag<string>();

        Task[] tasks = Enumerable.Range(0, parallelism).Select(i => Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
                var result = hasher.ComputeHash((byte[])data.Clone());
                if (!result.SequenceEqual(expected))
                    errors.Add($"[{i}] hash mismatch");
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] {ex.GetType().Name}: {ex.Message}");
            }
        })).ToArray();

        startGate.Set();

        var completed = await Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromMilliseconds(StressDeadlockTimeoutMs))
            .ContinueWith(t => !t.IsFaulted && !t.IsCanceled);

        TestContext.WriteLine(
            $"Large-input stress: parallelism={parallelism}, dataLength={dataLength}, Errors={errors.Count}");

        Assert.IsTrue(completed, "Large-input stress did not complete — possible deadlock.");
        Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Diagnostics under concurrent load
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that attaching independent diagnostics instances to each concurrent hasher
    /// records the correct node counts and passes validation for all instances.
    /// </summary>
    [TestMethod]
    public async Task StressTest_WhenDiagnosticsAttachedToConcurrentInstances_ShouldAllPassValidation()
    {
        const int parallelism = 10;
        const int blockSize = 4;
        const int fanOut = 2;
        const int dataLength = 12;

        var data = MakeData(dataLength);

        using var startGate = new ManualResetEventSlim(false);
        var diagnosticsList = new ConcurrentBag<MerkleTreeDiagnostics>();
        var errors = new ConcurrentBag<string>();

        Task[] tasks = Enumerable.Range(0, parallelism).Select(i => Task.Run(() =>
        {
            startGate.Wait();
            try
            {
                var diag = new MerkleTreeDiagnostics();
                using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
                hasher.ComputeHash((byte[])data.Clone(), diag);
                diagnosticsList.Add(diag);
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] {ex.GetType().Name}: {ex.Message}");
            }
        })).ToArray();

        startGate.Set();

        var completed = await Task.WhenAll(tasks)
            .WaitAsync(TimeSpan.FromMilliseconds(StressDeadlockTimeoutMs))
            .ContinueWith(t => !t.IsFaulted && !t.IsCanceled);

        Assert.IsTrue(completed, "Tasks did not complete — possible deadlock.");
        Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
        Assert.AreEqual(parallelism, diagnosticsList.Count, "Not all diagnostics instances were recorded.");

        foreach (MerkleTreeDiagnostics diag in diagnosticsList)
        {
            var valid = diag.Validate(Factory, out IReadOnlyList<string>? validationErrors);
            Assert.IsTrue(valid, string.Join("; ", validationErrors));
        }
    }
}
