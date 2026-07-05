// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultTests.Match.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultTests
{
    /// <summary>
    /// Verifies that the value-returning Match invokes the success branch for a success.
    /// </summary>
    [TestMethod]
    public void Match_WhenSuccess_ShouldInvokeOnSuccessBranch()
    {
        var result = Result.Success().Match(() => 1, _ => -1);

        Assert.AreEqual(1, result);
    }

    /// <summary>
    /// Verifies that the value-returning Match invokes the failure branch with the carried error for a failure.
    /// </summary>
    [TestMethod]
    public void Match_WhenFailure_ShouldInvokeOnFailureBranchWithError()
    {
        var result = Result.Failure("boom").Match(() => "ok", error => error.Message);

        Assert.AreEqual("boom", result);
    }

    /// <summary>
    /// Verifies that the void Match invokes exactly the success action for a success.
    /// </summary>
    [TestMethod]
    public void Match_WhenSuccess_ForActionOverload_ShouldInvokeOnSuccessOnly()
    {
        var successInvoked = false;
        var failureInvoked = false;

        Result.Success().Match(() => successInvoked = true, _ => failureInvoked = true);

        Assert.IsTrue(successInvoked);
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

        Result.Failure("boom").Match(() => successInvoked = true, error => observedMessage = error.Message);

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
            _ = Result.Success().Match(null!, _ => 0);
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
            _ = Result.Success().Match(() => 0, null!);
        });

        Assert.AreEqual("onFailure", ex.ParamName);
    }
}
