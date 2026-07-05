// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensionsTests.TapAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that TapAsync over a task source with a synchronous action invokes the action with the carried value
    /// and returns the result unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapAsync_WhenSuccess_ForTaskSourceWithSyncAction_ShouldInvokeActionAndReturnResultUnchanged()
    {
        var observed = 0;
        var source = Task.FromResult(Result.Success(7));

        var tapped = await source.TapAsync(v => observed = v);

        Assert.AreEqual(7, observed);
        Assert.AreEqual(Result.Success(7), tapped);
    }

    /// <summary>
    /// Verifies that TapAsync over a task source with an asynchronous callback awaits the callback with the carried
    /// value and returns the result unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapAsync_WhenSuccess_ForTaskSourceWithAsyncCallback_ShouldAwaitCallbackAndReturnResultUnchanged()
    {
        var observed = 0;
        var source = Task.FromResult(Result.Success(7));

        var tapped = await source.TapAsync(v =>
        {
            observed = v;
            return Task.CompletedTask;
        });

        Assert.AreEqual(7, observed);
        Assert.AreEqual(Result.Success(7), tapped);
    }

    /// <summary>
    /// Verifies that TapAsync over a result source with an asynchronous callback awaits the callback with the
    /// carried value and returns the result unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapAsync_WhenSuccess_ForResultSourceWithAsyncCallback_ShouldAwaitCallbackAndReturnResultUnchanged()
    {
        var observed = 0;
        var source = Result.Success(7);

        var tapped = await source.TapAsync(v =>
        {
            observed = v;
            return Task.CompletedTask;
        });

        Assert.AreEqual(7, observed);
        Assert.AreEqual(Result.Success(7), tapped);
    }

    /// <summary>
    /// Verifies that TapAsync does not invoke the asynchronous callback for a failed result and returns the failure
    /// unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapAsync_WhenFailure_ForTaskSourceWithAsyncCallback_ShouldNotInvokeCallbackAndReturnResultUnchanged()
    {
        var invoked = false;
        var source = Task.FromResult(Result.Failure<int>(ResultError.FromMessage("boom")));

        var tapped = await source.TapAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        Assert.IsFalse(invoked);
        Assert.IsTrue(tapped.IsFailure);
        Assert.AreEqual("boom", tapped.Error.Message);
    }

    /// <summary>
    /// Verifies that TapAsync over a result source does not invoke the asynchronous callback for a failed result.
    /// </summary>
    [TestMethod]
    public async Task TapAsync_WhenFailure_ForResultSourceWithAsyncCallback_ShouldNotInvokeCallback()
    {
        var invoked = false;
        var source = Result.Failure<int>(ResultError.FromMessage("boom"));

        var tapped = await source.TapAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        Assert.IsFalse(invoked);
        Assert.IsTrue(tapped.IsFailure);
    }

    /// <summary>
    /// Verifies that TapAsync rejects a <see langword="null" /> synchronous action synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void TapAsync_WhenActionIsNull_ForTaskSourceWithSyncAction_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.TapAsync((Action<int>)null!);
        });

        Assert.AreEqual("onSuccess", ex.ParamName);
    }

    /// <summary>
    /// Verifies that TapAsync rejects a <see langword="null" /> asynchronous callback synchronously, before any
    /// await.
    /// </summary>
    [TestMethod]
    public void TapAsync_WhenCallbackIsNull_ForResultSourceWithAsyncCallback_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).TapAsync((Func<int, Task>)null!);
        });

        Assert.AreEqual("onSuccess", ex.ParamName);
    }

    /// <summary>
    /// Verifies that TapAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void TapAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Result<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.TapAsync(v => { });
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that TapErrorAsync over a task source with a synchronous action invokes the action with the carried
    /// error and returns the result unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapErrorAsync_WhenFailure_ForTaskSourceWithSyncAction_ShouldInvokeActionAndReturnResultUnchanged()
    {
        var observed = string.Empty;
        var source = Task.FromResult(Result.Failure<int>(ResultError.FromMessage("boom")));

        var tapped = await source.TapErrorAsync(error => observed = error.Message);

        Assert.AreEqual("boom", observed);
        Assert.IsTrue(tapped.IsFailure);
        Assert.AreEqual("boom", tapped.Error.Message);
    }

    /// <summary>
    /// Verifies that TapErrorAsync over a task source with an asynchronous callback awaits the callback with the
    /// carried error and returns the result unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapErrorAsync_WhenFailure_ForTaskSourceWithAsyncCallback_ShouldAwaitCallbackAndReturnResultUnchanged()
    {
        var observed = string.Empty;
        var source = Task.FromResult(Result.Failure<int>(ResultError.FromMessage("boom")));

        var tapped = await source.TapErrorAsync(error =>
        {
            observed = error.Message;
            return Task.CompletedTask;
        });

        Assert.AreEqual("boom", observed);
        Assert.IsTrue(tapped.IsFailure);
    }

    /// <summary>
    /// Verifies that TapErrorAsync over a result source with an asynchronous callback awaits the callback with the
    /// carried error and returns the result unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapErrorAsync_WhenFailure_ForResultSourceWithAsyncCallback_ShouldAwaitCallbackAndReturnResultUnchanged()
    {
        var observed = string.Empty;
        var source = Result.Failure<int>(ResultError.FromMessage("boom"));

        var tapped = await source.TapErrorAsync(error =>
        {
            observed = error.Message;
            return Task.CompletedTask;
        });

        Assert.AreEqual("boom", observed);
        Assert.IsTrue(tapped.IsFailure);
    }

    /// <summary>
    /// Verifies that TapErrorAsync does not invoke the asynchronous callback for a successful result and returns the
    /// success unchanged.
    /// </summary>
    [TestMethod]
    public async Task TapErrorAsync_WhenSuccess_ForTaskSourceWithAsyncCallback_ShouldNotInvokeCallbackAndReturnResultUnchanged()
    {
        var invoked = false;
        var source = Task.FromResult(Result.Success(7));

        var tapped = await source.TapErrorAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        Assert.IsFalse(invoked);
        Assert.AreEqual(Result.Success(7), tapped);
    }

    /// <summary>
    /// Verifies that TapErrorAsync over a result source does not invoke the asynchronous callback for a successful
    /// result.
    /// </summary>
    [TestMethod]
    public async Task TapErrorAsync_WhenSuccess_ForResultSourceWithAsyncCallback_ShouldNotInvokeCallback()
    {
        var invoked = false;

        var tapped = await Result.Success(7).TapErrorAsync(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        Assert.IsFalse(invoked);
        Assert.AreEqual(Result.Success(7), tapped);
    }

    /// <summary>
    /// Verifies that TapErrorAsync rejects a <see langword="null" /> synchronous action synchronously, before any
    /// await.
    /// </summary>
    [TestMethod]
    public void TapErrorAsync_WhenActionIsNull_ForTaskSourceWithSyncAction_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.TapErrorAsync((Action<ResultError>)null!);
        });

        Assert.AreEqual("onFailure", ex.ParamName);
    }

    /// <summary>
    /// Verifies that TapErrorAsync rejects a <see langword="null" /> asynchronous callback synchronously, before any
    /// await.
    /// </summary>
    [TestMethod]
    public void TapErrorAsync_WhenCallbackIsNull_ForResultSourceWithAsyncCallback_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).TapErrorAsync((Func<ResultError, Task>)null!);
        });

        Assert.AreEqual("onFailure", ex.ParamName);
    }

    /// <summary>
    /// Verifies that TapErrorAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void TapErrorAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Result<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.TapErrorAsync(error => { });
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
