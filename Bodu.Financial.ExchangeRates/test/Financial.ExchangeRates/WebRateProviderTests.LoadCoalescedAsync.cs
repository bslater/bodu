// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebRateProviderTests.LoadCoalescedAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class WebRateProviderTests
{
    /// <summary>
    /// Verifies that a load whose unit is already loaded completes at once without running the load.
    /// </summary>
    [TestMethod]
    public async Task LoadCoalescedAsync_WhenAlreadyLoaded_ShouldNotRunTheLoad()
    {
        using CoalescingTestWebRateProvider provider = new();
        int loads = 0;

        Task task = provider.RunAsync("unit", () => true, _ =>
        {
            loads++;
            return Task.CompletedTask;
        });
        await task;

        Assert.IsTrue(task.IsCompletedSuccessfully);
        Assert.AreEqual(0, loads);
    }

    /// <summary>
    /// Verifies that a load whose unit is not yet loaded runs the load once.
    /// </summary>
    [TestMethod]
    public async Task LoadCoalescedAsync_WhenNotLoaded_ShouldRunTheLoadOnce()
    {
        using CoalescingTestWebRateProvider provider = new();
        int loads = 0;

        await provider.RunAsync("unit", () => false, _ =>
        {
            loads++;
            return Task.CompletedTask;
        });

        Assert.AreEqual(1, loads);
    }

    /// <summary>
    /// Verifies that the coverage check runs while the provider holds its synchronization object, so it reads the
    /// provider's coverage consistently with a concurrent publish.
    /// </summary>
    [TestMethod]
    public async Task LoadCoalescedAsync_WhenCheckingCoverage_ShouldHoldTheSyncRoot()
    {
        using CoalescingTestWebRateProvider provider = new();
        bool held = false;

        await provider.RunAsync("unit", () => held = Monitor.IsEntered(provider.Gate), _ => Task.CompletedTask);

        Assert.IsTrue(held);
    }

    /// <summary>
    /// Verifies that concurrent loads of one unit share a single run of the load.
    /// </summary>
    [TestMethod]
    public async Task LoadCoalescedAsync_WhenCalledConcurrentlyForSameKey_ShouldShareOneLoad()
    {
        using CoalescingTestWebRateProvider provider = new();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int loads = 0;

        Task Load(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref loads);
            return gate.Task;
        }

        Task first = provider.RunAsync("unit", () => false, Load);
        Task second = provider.RunAsync("unit", () => false, Load);
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.AreEqual(1, loads);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void LoadCoalescedAsync_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using CoalescingTestWebRateProvider provider = new();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = provider.RunAsync(null!, () => false, _ => Task.CompletedTask);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> coverage check throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void LoadCoalescedAsync_WhenIsLoadedIsNull_ShouldThrowArgumentNullException()
    {
        using CoalescingTestWebRateProvider provider = new();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = provider.RunAsync("unit", null!, _ => Task.CompletedTask);
        });

        Assert.AreEqual("isLoaded", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> load throws <see cref="ArgumentNullException" /> naming it.
    /// </summary>
    [TestMethod]
    public void LoadCoalescedAsync_WhenLoadIsNull_ShouldThrowArgumentNullException()
    {
        using CoalescingTestWebRateProvider provider = new();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = provider.RunAsync("unit", () => false, null!);
        });

        Assert.AreEqual("load", ex.ParamName);
    }
}
