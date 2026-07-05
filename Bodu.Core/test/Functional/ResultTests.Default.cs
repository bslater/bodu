// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultTests
{
    /// <summary>
    /// Verifies the default-value contract: <c>default(Result)</c> is a failure carrying the empty error, and
    /// <see cref="Result.Error" /> on it is valid.
    /// </summary>
    [TestMethod]
    public void Default_WhenUninitialized_ShouldBeFailureWithEmptyError()
    {
        var result = default(Result);

        Assert.IsTrue(result.IsFailure);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(string.Empty, result.Error.Message);
        Assert.IsNull(result.Error.Code);
        Assert.IsNull(result.Error.Exception);
    }

    /// <summary>
    /// Verifies that <c>default(Result)</c> equals an explicit failure carrying the default error.
    /// </summary>
    [TestMethod]
    public void Default_WhenComparedToExplicitEmptyFailure_ShouldBeEqual()
    {
        Assert.AreEqual(Result.Failure(default), default(Result));
    }

    /// <summary>
    /// Verifies that Match on <c>default(Result)</c> is total and hits the failure branch.
    /// </summary>
    [TestMethod]
    public void Default_WhenMatched_ShouldInvokeFailureBranch()
    {
        var result = default(Result).Match(() => "ok", error => $"failed: '{error.Message}'");

        Assert.AreEqual("failed: ''", result);
    }
}
