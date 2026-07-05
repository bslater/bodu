// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensionsTests.MatchAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that MatchAsync over a task source with synchronous branches invokes the onSuccess branch for a
    /// successful result.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenSuccess_ForTaskSourceWithSyncBranches_ShouldInvokeOnSuccess()
    {
        var source = Task.FromResult(Result.Success(7));

        var matched = await source.MatchAsync(v => $"got {v}", error => $"failed: {error.Message}");

        Assert.AreEqual("got 7", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with synchronous branches invokes the onFailure branch for a
    /// failed result.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenFailure_ForTaskSourceWithSyncBranches_ShouldInvokeOnFailure()
    {
        var source = Task.FromResult(Result.Failure<int>(ResultError.FromMessage("boom")));

        var matched = await source.MatchAsync(v => $"got {v}", error => $"failed: {error.Message}");

        Assert.AreEqual("failed: boom", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with asynchronous branches awaits the onSuccess branch for a
    /// successful result, without invoking onFailure.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenSuccess_ForTaskSourceWithAsyncBranches_ShouldInvokeOnSuccessOnly()
    {
        var failureInvoked = false;
        var source = Task.FromResult(Result.Success(7));

        var matched = await source.MatchAsync(
            v => Task.FromResult($"got {v}"),
            error =>
            {
                failureInvoked = true;
                return Task.FromResult($"failed: {error.Message}");
            });

        Assert.AreEqual("got 7", matched);
        Assert.IsFalse(failureInvoked);
    }

    /// <summary>
    /// Verifies that MatchAsync over a task source with asynchronous branches awaits the onFailure branch for a
    /// failed result, without invoking onSuccess.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenFailure_ForTaskSourceWithAsyncBranches_ShouldInvokeOnFailureOnly()
    {
        var successInvoked = false;
        var source = Task.FromResult(Result.Failure<int>(ResultError.FromMessage("boom")));

        var matched = await source.MatchAsync(
            v =>
            {
                successInvoked = true;
                return Task.FromResult($"got {v}");
            },
            error => Task.FromResult($"failed: {error.Message}"));

        Assert.AreEqual("failed: boom", matched);
        Assert.IsFalse(successInvoked);
    }

    /// <summary>
    /// Verifies that MatchAsync over a result source with asynchronous branches awaits the onSuccess branch for a
    /// successful result.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenSuccess_ForResultSourceWithAsyncBranches_ShouldInvokeOnSuccess()
    {
        var matched = await Result.Success(7).MatchAsync(
            v => Task.FromResult($"got {v}"),
            error => Task.FromResult($"failed: {error.Message}"));

        Assert.AreEqual("got 7", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync over a result source with asynchronous branches awaits the onFailure branch for a
    /// failed result.
    /// </summary>
    [TestMethod]
    public async Task MatchAsync_WhenFailure_ForResultSourceWithAsyncBranches_ShouldInvokeOnFailure()
    {
        var matched = await Result.Failure<int>(ResultError.FromMessage("boom")).MatchAsync(
            v => Task.FromResult($"got {v}"),
            error => Task.FromResult($"failed: {error.Message}"));

        Assert.AreEqual("failed: boom", matched);
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> onSuccess branch synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenOnSuccessIsNull_ForTaskSourceWithSyncBranches_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MatchAsync((Func<int, string>)null!, error => error.Message);
        });

        Assert.AreEqual("onSuccess", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> onFailure branch synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenOnFailureIsNull_ForTaskSourceWithAsyncBranches_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MatchAsync(v => Task.FromResult(v.ToString()), (Func<ResultError, Task<string>>)null!);
        });

        Assert.AreEqual("onFailure", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MatchAsync over a result source rejects a <see langword="null" /> onSuccess branch
    /// synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenOnSuccessIsNull_ForResultSourceWithAsyncBranches_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).MatchAsync((Func<int, Task<string>>)null!, error => Task.FromResult(error.Message));
        });

        Assert.AreEqual("onSuccess", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MatchAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MatchAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Result<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MatchAsync(v => v.ToString(), error => error.Message);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
