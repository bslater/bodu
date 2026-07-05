// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Bind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that Bind returns the selector's successful result when the source represents success.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSuccessAndSelectorSucceeds_ShouldReturnSelectorResult()
    {
        var result = Result.Success(21).Bind(v => Result.Success(v * 2));

        Assert.AreEqual(Result.Success(42), result);
    }

    /// <summary>
    /// Verifies that Bind returns the selector's failed result when the selector fails.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSuccessAndSelectorFails_ShouldReturnSelectorFailure()
    {
        var error = ResultError.FromMessage("boom");

        var result = Result.Success(21).Bind(_ => Result.Failure<int>(error));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }

    /// <summary>
    /// Verifies that Bind propagates the original error without invoking the selector when the result is a failure.
    /// </summary>
    [TestMethod]
    public void Bind_WhenFailure_ShouldPropagateErrorWithoutInvokingSelector()
    {
        var error = ResultError.FromMessage("boom");
        var invoked = false;

        var result = Result.Failure<int>(error).Bind(v =>
        {
            invoked = true;
            return Result.Success(v * 2);
        });

        Assert.IsFalse(invoked);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }

    /// <summary>
    /// Verifies that Bind rejects a <see langword="null" /> selector.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSelectorIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).Bind<string>(null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
