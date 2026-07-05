// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensionsTests.BindAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that BindAsync over a task source with a synchronous selector produces the selector's option.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSome_ForTaskSourceWithSyncSelector_ShouldReturnSelectorOption()
    {
        var source = Task.FromResult(Option<string>.Some("railway"));

        var bound = await source.BindAsync(s => Option<int>.Some(s.Length));

        Assert.AreEqual(Option<int>.Some(7), bound);
    }

    /// <summary>
    /// Verifies that BindAsync over a task source with an asynchronous selector produces the awaited selector's
    /// option.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSome_ForTaskSourceWithAsyncSelector_ShouldReturnSelectorOption()
    {
        var source = Task.FromResult(Option<string>.Some("railway"));

        var bound = await source.BindAsync(s => Task.FromResult(Option<int>.Some(s.Length)));

        Assert.AreEqual(Option<int>.Some(7), bound);
    }

    /// <summary>
    /// Verifies that BindAsync over an option source with an asynchronous selector produces the awaited selector's
    /// option.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSome_ForOptionSourceWithAsyncSelector_ShouldReturnSelectorOption()
    {
        var source = Option<string>.Some("railway");

        var bound = await source.BindAsync(s => Task.FromResult(Option<int>.Some(s.Length)));

        Assert.AreEqual(Option<int>.Some(7), bound);
    }

    /// <summary>
    /// Verifies that BindAsync over a task source propagates None without invoking the asynchronous selector.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenNone_ForTaskSourceWithAsyncSelector_ShouldReturnNoneWithoutInvokingSelector()
    {
        var invoked = false;
        var source = Task.FromResult(Option<string>.None);

        var bound = await source.BindAsync(s =>
        {
            invoked = true;
            return Task.FromResult(Option<int>.Some(s.Length));
        });

        Assert.IsTrue(bound.IsNone);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that BindAsync over an option source propagates None without invoking the asynchronous selector.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenNone_ForOptionSourceWithAsyncSelector_ShouldReturnNoneWithoutInvokingSelector()
    {
        var invoked = false;

        var bound = await Option<string>.None.BindAsync(s =>
        {
            invoked = true;
            return Task.FromResult(Option<int>.Some(s.Length));
        });

        Assert.IsTrue(bound.IsNone);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that BindAsync propagates a None produced by the selector itself.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSelectorReturnsNone_ShouldReturnNone()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var bound = await source.BindAsync(_ => Task.FromResult(Option<string>.None));

        Assert.IsTrue(bound.IsNone);
    }

    /// <summary>
    /// Verifies that BindAsync rejects a <see langword="null" /> synchronous selector synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void BindAsync_WhenSelectorIsNull_ForTaskSourceWithSyncSelector_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.BindAsync((Func<int, Option<string>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that BindAsync rejects a <see langword="null" /> asynchronous selector synchronously, before any
    /// await.
    /// </summary>
    [TestMethod]
    public void BindAsync_WhenSelectorIsNull_ForTaskSourceWithAsyncSelector_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.BindAsync((Func<int, Task<Option<string>>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that BindAsync over an option source rejects a <see langword="null" /> asynchronous selector
    /// synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void BindAsync_WhenSelectorIsNull_ForOptionSourceWithAsyncSelector_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).BindAsync((Func<int, Task<Option<string>>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that BindAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void BindAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Option<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.BindAsync(v => Option<string>.Some(v.ToString()));
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
