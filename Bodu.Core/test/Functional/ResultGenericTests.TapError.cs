// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.TapError.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that TapError invokes the action with the carried error and returns the same result for a failure.
    /// </summary>
    [TestMethod]
    public void TapError_WhenFailure_ShouldInvokeActionAndReturnSameResult()
    {
        var observed = string.Empty;
        var source = Result.Failure<int>("boom");

        var returned = source.TapError(error => observed = error.Message);

        Assert.AreEqual("boom", observed);
        Assert.AreEqual(source, returned);
    }

    /// <summary>
    /// Verifies that TapError does not invoke the action and still returns the same result for a success.
    /// </summary>
    [TestMethod]
    public void TapError_WhenSuccess_ShouldNotInvokeActionAndReturnSameResult()
    {
        var invoked = false;
        var source = Result.Success(42);

        var returned = source.TapError(_ => invoked = true);

        Assert.IsFalse(invoked);
        Assert.AreEqual(source, returned);
    }

    /// <summary>
    /// Verifies that TapError rejects a <see langword="null" /> action even for a success.
    /// </summary>
    [TestMethod]
    public void TapError_WhenActionIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).TapError(null!);
        });

        Assert.AreEqual("action", ex.ParamName);
    }
}
