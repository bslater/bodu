// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Match.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that the value-returning Match invokes the success branch with the carried value for a success.
    /// </summary>
    [TestMethod]
    public void Match_WhenSuccess_ShouldInvokeOnSuccessBranch()
    {
        var result = Result.Success(5).Match(v => v * 2, _ => -1);

        Assert.AreEqual(10, result);
    }

    /// <summary>
    /// Verifies that the value-returning Match invokes the failure branch with the carried error for a failure.
    /// </summary>
    [TestMethod]
    public void Match_WhenFailure_ShouldInvokeOnFailureBranchWithError()
    {
        var result = Result.Failure<int>("boom").Match(v => v.ToString(), error => error.Message);

        Assert.AreEqual("boom", result);
    }

    /// <summary>
    /// Verifies that the void Match invokes exactly the success action with the carried value for a success.
    /// </summary>
    [TestMethod]
    public void Match_WhenSuccess_ForActionOverload_ShouldInvokeOnSuccessOnly()
    {
        var successValue = 0;
        var failureInvoked = false;

        Result.Success(9).Match(v => successValue = v, _ => failureInvoked = true);

        Assert.AreEqual(9, successValue);
        Assert.IsFalse(failureInvoked);
    }

    /// <summary>
    /// Verifies that the void Match invokes exactly the failure action with the carried error for a failure.
    /// </summary>
    [TestMethod]
    public void Match_WhenFailure_ForActionOverload_ShouldInvokeOnFailureOnly()
    {
        var successInvoked = false;
        var observedMessage = string.Empty;

        Result.Failure<int>("boom").Match(_ => successInvoked = true, error => observedMessage = error.Message);

        Assert.IsFalse(successInvoked);
        Assert.AreEqual("boom", observedMessage);
    }

    /// <summary>
    /// Verifies that Match rejects a <see langword="null" /> success branch.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnSuccessIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).Match(null!, _ => 0);
        });

        Assert.AreEqual("onSuccess", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Match rejects a <see langword="null" /> failure branch even for a success.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnFailureIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).Match(v => v, null!);
        });

        Assert.AreEqual("onFailure", ex.ParamName);
    }
}
