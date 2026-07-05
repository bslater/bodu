// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensionsTests.MapAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that MapAsync over a task source with a synchronous selector projects the carried value.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenSuccess_ForTaskSourceWithSyncSelector_ShouldProjectValue()
    {
        var source = Task.FromResult(Result.Success("railway"));

        var mapped = await source.MapAsync(s => s.Length);

        Assert.AreEqual(Result.Success(7), mapped);
    }

    /// <summary>
    /// Verifies that MapAsync over a task source with an asynchronous selector projects the carried value.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenSuccess_ForTaskSourceWithAsyncSelector_ShouldProjectValue()
    {
        var source = Task.FromResult(Result.Success("railway"));

        var mapped = await source.MapAsync(s => Task.FromResult(s.Length));

        Assert.AreEqual(Result.Success(7), mapped);
    }

    /// <summary>
    /// Verifies that MapAsync over a result source with an asynchronous selector projects the carried value.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenSuccess_ForResultSourceWithAsyncSelector_ShouldProjectValue()
    {
        var source = Result.Success("railway");

        var mapped = await source.MapAsync(s => Task.FromResult(s.Length));

        Assert.AreEqual(Result.Success(7), mapped);
    }

    /// <summary>
    /// Verifies that MapAsync over a task source propagates the original error without invoking the asynchronous
    /// selector.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenFailure_ForTaskSourceWithAsyncSelector_ShouldPropagateErrorWithoutInvokingSelector()
    {
        var invoked = false;
        var source = Task.FromResult(Result.Failure<string>(ResultError.FromMessage("boom")));

        var mapped = await source.MapAsync(s =>
        {
            invoked = true;
            return Task.FromResult(s.Length);
        });

        Assert.IsTrue(mapped.IsFailure);
        Assert.AreEqual("boom", mapped.Error.Message);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that MapAsync over a result source propagates the original error without invoking the asynchronous
    /// selector.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenFailure_ForResultSourceWithAsyncSelector_ShouldPropagateErrorWithoutInvokingSelector()
    {
        var invoked = false;
        var source = Result.Failure<string>(ResultError.FromMessage("boom"));

        var mapped = await source.MapAsync(s =>
        {
            invoked = true;
            return Task.FromResult(s.Length);
        });

        Assert.IsTrue(mapped.IsFailure);
        Assert.AreEqual("boom", mapped.Error.Message);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> awaited projection result throws
    /// <see cref="ArgumentNullException" /> (the strict lift), surfaced when the returned task is awaited.
    /// </summary>
    [TestMethod]
    public async Task MapAsync_WhenAsyncSelectorReturnsNull_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await source.MapAsync(_ => Task.FromResult<string>(null!));
        });
    }

    /// <summary>
    /// Verifies that MapAsync rejects a <see langword="null" /> synchronous selector synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSelectorIsNull_ForTaskSourceWithSyncSelector_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapAsync((Func<int, string>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapAsync rejects a <see langword="null" /> asynchronous selector synchronously, before any
    /// await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSelectorIsNull_ForTaskSourceWithAsyncSelector_ShouldThrowArgumentNullException()
    {
        var source = Task.FromResult(Result.Success(1));

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapAsync((Func<int, Task<string>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapAsync over a result source rejects a <see langword="null" /> asynchronous selector
    /// synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSelectorIsNull_ForResultSourceWithAsyncSelector_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).MapAsync((Func<int, Task<string>>)null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapAsync rejects a <see langword="null" /> source task synchronously, before any await.
    /// </summary>
    [TestMethod]
    public void MapAsync_WhenSourceTaskIsNull_ShouldThrowArgumentNullException()
    {
        Task<Result<int>> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.MapAsync(v => v.ToString());
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
