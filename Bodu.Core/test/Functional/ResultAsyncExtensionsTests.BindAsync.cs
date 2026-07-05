// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensionsTests.BindAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that BindAsync over a task source with a synchronous selector produces the selector's result.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSuccess_ForTaskSourceWithSyncSelector_ShouldReturnSelectorResult()
    {
        var source = Task.FromResult(Result.Success("railway"));

        var bound = await source.BindAsync(s => Result.Success(s.Length));

        Assert.AreEqual(Result.Success(7), bound);
    }

    /// <summary>
    /// Verifies that BindAsync over a task source with an asynchronous selector produces the awaited selector's
    /// result.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSuccess_ForTaskSourceWithAsyncSelector_ShouldReturnSelectorResult()
    {
        var source = Task.FromResult(Result.Success("railway"));

        var bound = await source.BindAsync(s => Task.FromResult(Result.Success(s.Length)));

        Assert.AreEqual(Result.Success(7), bound);
    }

    /// <summary>
    /// Verifies that BindAsync over a result source with an asynchronous selector produces the awaited selector's
    /// result.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSuccess_ForResultSourceWithAsyncSelector_ShouldReturnSelectorResult()
    {
        var source = Result.Success("railway");

        var bound = await source.BindAsync(s => Task.FromResult(Result.Success(s.Length)));

        Assert.AreEqual(Result.Success(7), bound);
    }

    /// <summary>
    /// Verifies that BindAsync over a task source propagates the original error without invoking the asynchronous
    /// selector.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenFailure_ForTaskSourceWithAsyncSelector_ShouldPropagateErrorWithoutInvokingSelector()
    {
        var invoked = false;
        var source = Task.FromResult(Result.Failure<string>(ResultError.FromMessage("boom")));

        var bound = await source.BindAsync(s =>
        {
            invoked = true;
            return Task.FromResult(Result.Success(s.Length));
        });

        Assert.IsTrue(bound.IsFailure);
        Assert.AreEqual("boom", bound.Error.Message);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that BindAsync over a result source propagates the original error without invoking the asynchronous
    /// selector.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenFailure_ForResultSourceWithAsyncSelector_ShouldPropagateErrorWithoutInvokingSelector()
    {
        var invoked = false;
        var source = Result.Failure<string>(ResultError.FromMessage("boom"));

        var bound = await source.BindAsync(s =>
        {
            invoked = true;
            return Task.FromResult(Result.Success(s.Length));
        });

        Assert.IsTrue(bound.IsFailure);
        Assert.AreEqual("boom", bound.Error.Message);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that BindAsync propagates a failure produced by the selector itself.
    /// </summary>
    [TestMethod]
    public async Task BindAsync_WhenSelectorReturnsFailure_ShouldReturnSelectorFailure()
    {
        var source = Task.FromResult(Result.Success(1));

        var bound = await source.BindAsync(_ => Task.FromResult(Result.Failure<string>(ResultError.FromMessage("rejected"))));

        Assert.IsTrue(bound.IsFailure);
        Assert.AreEqual("rejected", bound.Error.Message);
    }

    /// <summary>
    /// Verifies that BindAsync rejects a <see langword="null" /> synchronous selector synchronously, before any
    /// await.
    /// </summary>
    [TestMethod]
    public void BindAsync_WhenSelectorIsNull_ForTaskSourceWithSyncSelector_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.BindAsync((Func<int, Result<string>>)null!);
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
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.BindAsync((Func<int, Task<Result<string>>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that BindAsync over a result source rejects a <see langword="null" /> asynchronous selector
    /// synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void BindAsync_WhenSelectorIsNull_ForResultSourceWithAsyncSelector_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).BindAsync((Func<int, Task<Result<string>>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that BindAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void BindAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Result<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.BindAsync(v => Result.Success(v.ToString()));
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
