// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensionsTests.MatchAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that MatchAsync over a task source with synchronous branches invokes the left branch for a left value.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenLeft_ForTaskSourceWithSyncBranches_ShouldInvokeOnLeft()
    {
        var source = Task.FromResult(Either<int, string>.Left(7));

        var matched = await source.MatchAsync(l => $"left {l}", r => $"right {r}");

        Assert.AreEqual("left 7", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with synchronous branches invokes the right branch for a right value.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenRight_ForTaskSourceWithSyncBranches_ShouldInvokeOnRight()
    {
        var source = Task.FromResult(Either<int, string>.Right("text"));

        var matched = await source.MatchAsync(l => $"left {l}", r => $"right {r}");

        Assert.AreEqual("right text", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with asynchronous branches awaits the left branch for a left value,
    /// without invoking the right branch.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenLeft_ForTaskSourceWithAsyncBranches_ShouldInvokeOnLeftOnly()
    {
        var rightInvoked = false;
        var source = Task.FromResult(Either<int, string>.Left(7));

        var matched = await source.MatchAsync(
            l => Task.FromResult($"left {l}"),
            r =>
            {
                rightInvoked = true;
                return Task.FromResult($"right {r}");
            });

        Assert.AreEqual("left 7", matched);
        Assert.IsFalse(rightInvoked);
    }

    /// <summary>
    /// Verifies that MatchAsync over a value source with asynchronous branches awaits the right branch for a right value.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenRight_ForValueSourceWithAsyncBranches_ShouldInvokeOnRight()
    {
        var matched = await Either<int, string>.Right("text").MatchAsync(
            l => Task.FromResult($"left {l}"),
            r => Task.FromResult($"right {r}"));

        Assert.AreEqual("right text", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source throws <see cref="InvalidOperationException" /> when the awaited
    /// either is uninitialized.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenUninitialized_ForTaskSource_ShouldThrowInvalidOperationException()
    {
        var source = Task.FromResult(default(Either<int, string>));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await source.MatchAsync(l => Task.FromResult($"left {l}"), r => Task.FromResult($"right {r}"));
        });
    }

    /// <summary>
    /// Verifies that MatchAsync over a value source throws <see cref="InvalidOperationException" /> synchronously when
    /// the either is uninitialized.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenUninitialized_ForValueSource_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = default(Either<int, string>).MatchAsync(l => Task.FromResult($"left {l}"), r => Task.FromResult($"right {r}"));
        });
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Either<int, string>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MatchAsync(l => l.ToString(), r => r);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> onRight branch synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenOnRightIsNull_ForValueSourceWithAsyncBranches_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<int, string>.Left(1).MatchAsync(l => Task.FromResult(l.ToString()), (Func<string, Task<string>>)null!);
        });

        Assert.AreEqual("onRight", ex.ParamName);
    }
}
