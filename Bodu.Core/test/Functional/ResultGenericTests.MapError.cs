// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.MapError.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that MapError transforms the carried error when the result represents failure.
    /// </summary>
    [TestMethod]
    public void MapError_WhenFailure_ShouldTransformError()
    {
        var result = Result.Failure<int>("boom")
            .MapError(error => ResultError.FromMessage(error.Message, "Wrapped"));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("boom", result.Error.Message);
        Assert.AreEqual("Wrapped", result.Error.Code);
    }

    /// <summary>
    /// Verifies that MapError returns the result unchanged without invoking the selector when it represents
    /// success.
    /// </summary>
    [TestMethod]
    public void MapError_WhenSuccess_ShouldReturnResultUnchangedWithoutInvokingSelector()
    {
        var invoked = false;

        var result = Result.Success(42).MapError(error =>
        {
            invoked = true;
            return error;
        });

        Assert.IsFalse(invoked);
        Assert.AreEqual(Result.Success(42), result);
    }

    /// <summary>
    /// Verifies that MapError rejects a <see langword="null" /> selector.
    /// </summary>
    [TestMethod]
    public void MapError_WhenSelectorIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Failure<int>("boom").MapError(null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
