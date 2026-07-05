// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensionsTests.MatchAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that MatchAsync over a task source with synchronous branches invokes the onSome branch when a value
    /// is present.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenSome_ForTaskSourceWithSyncBranches_ShouldInvokeOnSome()
    {
        var source = Task.FromResult(Option<int>.Some(7));

        var matched = await source.MatchAsync(v => $"got {v}", () => "nothing");

        Assert.AreEqual("got 7", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with synchronous branches invokes the onNone branch when no value
    /// is present.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenNone_ForTaskSourceWithSyncBranches_ShouldInvokeOnNone()
    {
        var source = Task.FromResult(Option<int>.None);

        var matched = await source.MatchAsync(v => $"got {v}", () => "nothing");

        Assert.AreEqual("nothing", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with asynchronous branches awaits the onSome branch when a value
    /// is present, without invoking onNone.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenSome_ForTaskSourceWithAsyncBranches_ShouldInvokeOnSomeOnly()
    {
        var noneInvoked = false;
        var source = Task.FromResult(Option<int>.Some(7));

        var matched = await source.MatchAsync(
            v => Task.FromResult($"got {v}"),
            () =>
            {
                noneInvoked = true;
                return Task.FromResult("nothing");
            });

        Assert.AreEqual("got 7", matched);
        Assert.IsFalse(noneInvoked);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with asynchronous branches awaits the onNone branch when no value
    /// is present, without invoking onSome.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenNone_ForTaskSourceWithAsyncBranches_ShouldInvokeOnNoneOnly()
    {
        var someInvoked = false;
        var source = Task.FromResult(Option<int>.None);

        var matched = await source.MatchAsync(
            v =>
            {
                someInvoked = true;
                return Task.FromResult($"got {v}");
            },
            () => Task.FromResult("nothing"));

        Assert.AreEqual("nothing", matched);
        Assert.IsFalse(someInvoked);
    }

    /// <summary>
    /// Verifies that MatchAsync over an option source with asynchronous branches awaits the matching branch.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenSome_ForOptionSourceWithAsyncBranches_ShouldInvokeOnSome()
    {
        var matched = await Option<int>.Some(7).MatchAsync(
            v => Task.FromResult($"got {v}"),
            () => Task.FromResult("nothing"));

        Assert.AreEqual("got 7", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over an option source with asynchronous branches awaits the onNone branch when no
    /// value is present.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenNone_ForOptionSourceWithAsyncBranches_ShouldInvokeOnNone()
    {
        var matched = await Option<int>.None.MatchAsync(
            v => Task.FromResult($"got {v}"),
            () => Task.FromResult("nothing"));

        Assert.AreEqual("nothing", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> onSome branch synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenOnSomeIsNull_ForTaskSourceWithSyncBranches_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MatchAsync((Func<int, string>)null!, () => "nothing");
        });

        Assert.AreEqual("onSome", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> onNone branch synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenOnNoneIsNull_ForTaskSourceWithAsyncBranches_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MatchAsync(v => Task.FromResult(v.ToString()), (Func<Task<string>>)null!);
        });

        Assert.AreEqual("onNone", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MatchAsync over an option source rejects a <see langword="null" /> onSome branch synchronously,
    /// before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenOnSomeIsNull_ForOptionSourceWithAsyncBranches_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).MatchAsync((Func<int, Task<string>>)null!, () => Task.FromResult("nothing"));
        });

        Assert.AreEqual("onSome", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Option<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MatchAsync(v => v.ToString(), () => "nothing");
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
