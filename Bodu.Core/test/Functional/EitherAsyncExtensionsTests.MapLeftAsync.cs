// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensionsTests.MapLeftAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that MapLeftAsync over a task source with a synchronous selector projects the left value.
    /// </summary>
    [TestMethod]
    public async Task MapLeftAsync_WhenLeft_ForTaskSourceWithSyncSelector_ShouldProjectLeft()
    {
        var source = Task.FromResult(Either<int, string>.Left(21));

        var mapped = await source.MapLeftAsync(v => v * 2);

        Assert.IsTrue(mapped.TryGetLeft(out var left));
        Assert.AreEqual(42, left);
    }

    /// <summary>
    /// Verifies that MapLeftAsync passes a right value through untouched without invoking the selector.
    /// </summary>
    [TestMethod]
    public async Task MapLeftAsync_WhenRight_ForTaskSourceWithSyncSelector_ShouldPassRightThrough()
    {
        var selectorInvoked = false;
        var source = Task.FromResult(Either<int, string>.Right("text"));

        var mapped = await source.MapLeftAsync(v =>
        {
            selectorInvoked = true;
            return v * 2;
        });

        Assert.IsFalse(selectorInvoked);
        Assert.IsTrue(mapped.TryGetRight(out var right));
        Assert.AreEqual("text", right);
    }

    /// <summary>
    /// Verifies that MapLeftAsync over a value source with an asynchronous selector projects the left value.
    /// </summary>
    [TestMethod]
    public async Task MapLeftAsync_WhenLeft_ForValueSourceWithAsyncSelector_ShouldProjectLeft()
    {
        var mapped = await Either<int, string>.Left(21).MapLeftAsync(v => Task.FromResult(v * 2));

        Assert.IsTrue(mapped.TryGetLeft(out var left));
        Assert.AreEqual(42, left);
    }

    /// <summary>
    /// Verifies that MapLeftAsync over a value source passes a right value through untouched.
    /// </summary>
    [TestMethod]
    public async Task MapLeftAsync_WhenRight_ForValueSourceWithAsyncSelector_ShouldPassRightThrough()
    {
        var mapped = await Either<int, string>.Right("text").MapLeftAsync(v => Task.FromResult(v * 2));

        Assert.IsTrue(mapped.TryGetRight(out var right));
        Assert.AreEqual("text", right);
    }

    /// <summary>
    /// Verifies that MapLeftAsync over a task source throws <see cref="InvalidOperationException" /> when the awaited
    /// either is uninitialized.
    /// </summary>
    [TestMethod]
    public async Task MapLeftAsync_WhenUninitialized_ForTaskSource_ShouldThrowInvalidOperationException()
    {
        var source = Task.FromResult(default(Either<int, string>));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await source.MapLeftAsync(v => Task.FromResult(v * 2));
        });
    }

    /// <summary>
    /// Verifies that MapLeftAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapLeftAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Either<int, string>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapLeftAsync(v => v * 2);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapLeftAsync rejects a <see langword="null" /> selector synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapLeftAsync_WhenSelectorIsNull_ForValueSource_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<int, string>.Left(1).MapLeftAsync((Func<int, Task<int>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
