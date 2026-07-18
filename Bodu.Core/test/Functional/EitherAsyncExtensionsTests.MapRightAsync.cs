// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensionsTests.MapRightAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that MapRightAsync over a task source with a synchronous selector projects the right value.
    /// </summary>
    [TestMethod]
    public async Task MapRightAsync_WhenRight_ForTaskSourceWithSyncSelector_ShouldProjectRight()
    {
        var source = Task.FromResult(Either<int, string>.Right("ab"));

        var mapped = await source.MapRightAsync(v => v.Length);

        Assert.IsTrue(mapped.TryGetRight(out var right));
        Assert.AreEqual(2, right);
    }

    /// <summary>
    /// Verifies that MapRightAsync passes a left value through untouched without invoking the selector.
    /// </summary>
    [TestMethod]
    public async Task MapRightAsync_WhenLeft_ForTaskSourceWithSyncSelector_ShouldPassLeftThrough()
    {
        var selectorInvoked = false;
        var source = Task.FromResult(Either<int, string>.Left(9));

        var mapped = await source.MapRightAsync(v =>
        {
            selectorInvoked = true;
            return v.Length;
        });

        Assert.IsFalse(selectorInvoked);
        Assert.IsTrue(mapped.TryGetLeft(out var left));
        Assert.AreEqual(9, left);
    }

    /// <summary>
    /// Verifies that MapRightAsync over a value source with an asynchronous selector projects the right value.
    /// </summary>
    [TestMethod]
    public async Task MapRightAsync_WhenRight_ForValueSourceWithAsyncSelector_ShouldProjectRight()
    {
        var mapped = await Either<int, string>.Right("ab").MapRightAsync(v => Task.FromResult(v.Length));

        Assert.IsTrue(mapped.TryGetRight(out var right));
        Assert.AreEqual(2, right);
    }

    /// <summary>
    /// Verifies that MapRightAsync over a value source passes a left value through untouched.
    /// </summary>
    [TestMethod]
    public async Task MapRightAsync_WhenLeft_ForValueSourceWithAsyncSelector_ShouldPassLeftThrough()
    {
        var mapped = await Either<int, string>.Left(9).MapRightAsync(v => Task.FromResult(v.Length));

        Assert.IsTrue(mapped.TryGetLeft(out var left));
        Assert.AreEqual(9, left);
    }

    /// <summary>
    /// Verifies that MapRightAsync over a task source throws <see cref="InvalidOperationException" /> when the awaited
    /// either is uninitialized.
    /// </summary>
    [TestMethod]
    public async Task MapRightAsync_WhenUninitialized_ForTaskSource_ShouldThrowInvalidOperationException()
    {
        var source = Task.FromResult(default(Either<int, string>));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            _ = await source.MapRightAsync(v => Task.FromResult(v.Length));
        });
    }

    /// <summary>
    /// Verifies that MapRightAsync rejects a <see langword="null" /> selector synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapRightAsync_WhenSelectorIsNull_ForTaskSource_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Either<int, string>.Right("x"));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapRightAsync((Func<string, int>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
