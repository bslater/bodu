// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensionsTests.MapAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that MapAsync over a task source with a synchronous selector projects the contained value.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenSome_ForTaskSourceWithSyncSelector_ShouldProjectValue()
    {
        var source = Task.FromResult(Option<string>.Some("railway"));

        var mapped = await source.MapAsync(s => s.Length);

        Assert.AreEqual(Option<int>.Some(7), mapped);
    }

    /// <summary>
    /// Verifies that MapAsync over a task source with an asynchronous selector projects the contained value.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenSome_ForTaskSourceWithAsyncSelector_ShouldProjectValue()
    {
        var source = Task.FromResult(Option<string>.Some("railway"));

        var mapped = await source.MapAsync(s => Task.FromResult(s.Length));

        Assert.AreEqual(Option<int>.Some(7), mapped);
    }

    /// <summary>
    /// Verifies that MapAsync over an option source with an asynchronous selector projects the contained value.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenSome_ForOptionSourceWithAsyncSelector_ShouldProjectValue()
    {
        var source = Option<string>.Some("railway");

        var mapped = await source.MapAsync(s => Task.FromResult(s.Length));

        Assert.AreEqual(Option<int>.Some(7), mapped);
    }

    /// <summary>
    /// Verifies that MapAsync over a task source propagates None without invoking the asynchronous selector.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenNone_ForTaskSourceWithAsyncSelector_ShouldReturnNoneWithoutInvokingSelector()
    {
        var invoked = false;
        var source = Task.FromResult(Option<string>.None);

        var mapped = await source.MapAsync(s =>
        {
            invoked = true;
            return Task.FromResult(s.Length);
        });

        Assert.IsTrue(mapped.IsNone);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that MapAsync over an option source propagates None without invoking the asynchronous selector.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenNone_ForOptionSourceWithAsyncSelector_ShouldReturnNoneWithoutInvokingSelector()
    {
        var invoked = false;

        var mapped = await Option<string>.None.MapAsync(s =>
        {
            invoked = true;
            return Task.FromResult(s.Length);
        });

        Assert.IsTrue(mapped.IsNone);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> awaited projection result maps to None (the lenient lift).
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenAsyncSelectorReturnsNull_ShouldReturnNone()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var mapped = await source.MapAsync(_ => Task.FromResult<string>(null!));

        Assert.IsTrue(mapped.IsNone);
    }

    /// <summary>
    /// Verifies that MapAsync rejects a <see langword="null" /> synchronous selector synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSelectorIsNull_ForTaskSourceWithSyncSelector_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapAsync((Func<int, string>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapAsync rejects a <see langword="null" /> asynchronous selector synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSelectorIsNull_ForTaskSourceWithAsyncSelector_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Option<int>.Some(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapAsync((Func<int, Task<string>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapAsync over an option source rejects a <see langword="null" /> asynchronous selector
    /// synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSelectorIsNull_ForOptionSourceWithAsyncSelector_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).MapAsync((Func<int, Task<string>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Option<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapAsync(v => v.ToString());
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
