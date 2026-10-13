// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoundedConcurrencyTests.ForEachAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class BoundedConcurrencyTests
{
    /// <summary>
    /// Verifies that operations start at once, in order, up to the limit, and that each further operation starts as
    /// soon as one in flight completes.
    /// </summary>
    [TestMethod]
    public async Task ForEachAsync_WhenItemsExceedTheLimit_ShouldRunAtMostTheLimitAtOnce()
    {
        List<int> started = [];
        Dictionary<int, TaskCompletionSource> gates = Enumerable.Range(0, 5).ToDictionary(i => i, _ => new TaskCompletionSource());

        Task run = BoundedConcurrency.ForEachAsync(Enumerable.Range(0, 5), 2, item =>
        {
            started.Add(item);
            return gates[item].Task;
        });

        CollectionAssert.AreEqual(new[] { 0, 1 }, started);

        gates[0].SetResult();
        await WaitUntilAsync(() => started.Count == 3);
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, started);

        foreach (TaskCompletionSource gate in gates.Values)
            gate.TrySetResult();

        await run;
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, started);
    }

    /// <summary>
    /// Verifies that no operation starts after one fails, that the operations already in flight are awaited, and that
    /// the failure is then rethrown.
    /// </summary>
    [TestMethod]
    public async Task ForEachAsync_WhenAnOperationFails_ShouldStartNoMoreAndRethrowAfterTheRestFinish()
    {
        List<int> started = [];
        TaskCompletionSource first = new();

        Task run = BoundedConcurrency.ForEachAsync(Enumerable.Range(0, 5), 2, item =>
        {
            started.Add(item);
            return item switch
            {
                0 => first.Task,
                1 => Task.FromException(new InvalidOperationException("unit 1")),
                _ => Task.CompletedTask,
            };
        });

        Assert.IsFalse(run.IsCompleted);
        first.SetResult();

        InvalidOperationException ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await run;
        });

        Assert.AreEqual("unit 1", ex.Message);
        CollectionAssert.AreEqual(new[] { 0, 1 }, started);
    }

    /// <summary>
    /// Verifies that an operation that throws instead of returning a task is reported like a failed task.
    /// </summary>
    [TestMethod]
    public async Task ForEachAsync_WhenAnOperationThrows_ShouldRethrowIt()
    {
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await BoundedConcurrency.ForEachAsync<int>([1], 2, _ => throw new InvalidOperationException("thrown"));
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> sequence throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void ForEachAsync_WhenItemsIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BoundedConcurrency.ForEachAsync<int>(null!, 2, _ => Task.CompletedTask);
        });

        Assert.AreEqual("items", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> operation throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void ForEachAsync_WhenBodyIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BoundedConcurrency.ForEachAsync<int>([1], 2, null!);
        });

        Assert.AreEqual("body", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a limit below one throws <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    [TestMethod]
    public void ForEachAsync_WhenLimitIsBelowOne_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = BoundedConcurrency.ForEachAsync<int>([1], 0, _ => Task.CompletedTask);
        });

        Assert.AreEqual("limit", ex.ParamName);
    }

    /// <summary>
    /// Waits, a short step at a time, until a condition holds, failing the test when it does not within ten seconds.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <returns>A task that completes when the condition holds.</returns>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail("The condition did not hold within ten seconds.");

            await Task.Delay(10);
        }
    }
}
