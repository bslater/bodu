// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleFlightCoordinatorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

/// <summary>
/// Verifies the behavior of <see cref="SingleFlightCoordinator{TKey}" />, which coalesces concurrent operations that
/// share a key.
/// </summary>
[TestClass]
public sealed class SingleFlightCoordinatorTests
{
    /// <summary>
    /// Verifies that a single call runs its operation exactly once and completes.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task RunAsync_WhenSingleCaller_ShouldRunOperationOnce()
    {
        SingleFlightCoordinator<string> coordinator = new();
        var runCount = 0;

        await coordinator.RunAsync("key", () =>
        {
            Interlocked.Increment(ref runCount);
            return Task.CompletedTask;
        });

        Assert.AreEqual(1, runCount);
    }

    /// <summary>
    /// Verifies that several callers requesting the same key while an operation is in flight share that single
    /// operation rather than each starting their own.
    /// </summary>
    [TestMethod]
    public async Task RunAsync_WhenConcurrentCallersForSameKey_ShouldRunOperationOnce()
    {
        SingleFlightCoordinator<string> coordinator = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var runCount = 0;

        Task Operation()
        {
            Interlocked.Increment(ref runCount);
            return gate.Task;
        }

        Task first = coordinator.RunAsync("key", Operation);
        Task second = coordinator.RunAsync("key", Operation);
        Task third = coordinator.RunAsync("key", Operation);

        gate.SetResult();
        await Task.WhenAll(first, second, third);

        Assert.AreEqual(1, runCount);
    }

    /// <summary>
    /// Verifies that operations under distinct keys run independently rather than being coalesced.
    /// </summary>
    [TestMethod]
    public async Task RunAsync_WhenDifferentKeys_ShouldRunEachOperation()
    {
        SingleFlightCoordinator<string> coordinator = new();
        var firstRuns = 0;
        var secondRuns = 0;

        await Task.WhenAll(
            coordinator.RunAsync("a", () =>
            {
                Interlocked.Increment(ref firstRuns);
                return Task.CompletedTask;
            }),
            coordinator.RunAsync("b", () =>
            {
                Interlocked.Increment(ref secondRuns);
                return Task.CompletedTask;
            }));

        Assert.AreEqual(1, firstRuns);
        Assert.AreEqual(1, secondRuns);
    }

    /// <summary>
    /// Verifies that once an operation completes the key is released, so a later call for the same key runs a fresh
    /// operation.
    /// </summary>
    [TestMethod]
    public async Task RunAsync_WhenKeyReusedAfterCompletion_ShouldRunOperationAgain()
    {
        SingleFlightCoordinator<string> coordinator = new();
        var runCount = 0;

        Task Operation()
        {
            Interlocked.Increment(ref runCount);
            return Task.CompletedTask;
        }

        await coordinator.RunAsync("key", Operation);
        await coordinator.RunAsync("key", Operation);

        Assert.AreEqual(2, runCount);
    }

    /// <summary>
    /// Verifies that a faulting operation propagates its exception to every joined caller and that the key is released
    /// afterwards so a subsequent call can run again.
    /// </summary>
    [TestMethod]
    public async Task RunAsync_WhenOperationFaults_ShouldPropagateToJoinersAndReleaseKey()
    {
        SingleFlightCoordinator<string> coordinator = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var runCount = 0;

        async Task Operation()
        {
            Interlocked.Increment(ref runCount);
            await gate.Task;
            throw new InvalidOperationException("boom");
        }

        Task first = coordinator.RunAsync("key", Operation);
        Task second = coordinator.RunAsync("key", Operation);

        gate.SetResult();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await first);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await second);
        Assert.AreEqual(1, runCount);

        var reran = false;
        await coordinator.RunAsync("key", () =>
        {
            reran = true;
            return Task.CompletedTask;
        });

        Assert.IsTrue(reran);
    }

    /// <summary>
    /// Verifies that the result-producing overload runs the operation once for concurrent same-key callers and yields
    /// the shared result to each of them.
    /// </summary>
    [TestMethod]
    public async Task RunAsync_OfTResult_WhenConcurrentCallersForSameKey_ShouldRunOnceAndReturnSharedResult()
    {
        SingleFlightCoordinator<string> coordinator = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var runCount = 0;

        async Task<int> Operation()
        {
            Interlocked.Increment(ref runCount);
            await gate.Task;
            return 42;
        }

        Task<int> first = coordinator.RunAsync("key", Operation);
        Task<int> second = coordinator.RunAsync("key", Operation);

        gate.SetResult();
        int[] results = await Task.WhenAll(first, second);

        Assert.AreEqual(1, runCount);
        Assert.AreEqual(42, results[0]);
        Assert.AreEqual(42, results[1]);
    }

    /// <summary>
    /// Verifies that the void overload rejects a <see langword="null" /> operation.
    /// </summary>
    [TestMethod]
    public void RunAsync_WhenOperationIsNull_ShouldThrowArgumentNullException()
    {
        SingleFlightCoordinator<string> coordinator = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () => { _ = coordinator.RunAsync("key", (Func<Task>)null!); },
            "operation");
    }

    /// <summary>
    /// Verifies that the result-producing overload rejects a <see langword="null" /> operation.
    /// </summary>
    [TestMethod]
    public void RunAsync_OfTResult_WhenOperationIsNull_ShouldThrowArgumentNullException()
    {
        SingleFlightCoordinator<string> coordinator = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () => { _ = coordinator.RunAsync("key", (Func<Task<int>>)null!); },
            "operation");
    }
}
