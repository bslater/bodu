// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class ResultTests
{
    /// <summary>
    /// Verifies that a success and a failure each collapse through Match to the matching branch, exercising the
    /// primary non-generic railway path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Result_WhenMatchedOverSuccessAndFailure_ShouldInvokeMatchingBranch()
    {
        var ok = Result.Success().Match(() => "ok", error => $"failed: {error}");
        var failed = Result.Failure("boom").Match(() => "ok", error => $"failed: {error}");

        Assert.AreEqual("ok", ok);
        Assert.AreEqual("failed: boom", failed);
    }

    /// <summary>
    /// Verifies that IsSuccess and IsFailure report complementary states for both outcomes.
    /// </summary>
    [TestMethod]
    public void IsSuccess_WhenSuccessOrFailure_ShouldComplementIsFailure()
    {
        Assert.IsTrue(Result.Success().IsSuccess);
        Assert.IsFalse(Result.Success().IsFailure);
        Assert.IsFalse(Result.Failure("boom").IsSuccess);
        Assert.IsTrue(Result.Failure("boom").IsFailure);
    }

    /// <summary>
    /// Verifies that Error returns the carried error when the result represents failure.
    /// </summary>
    [TestMethod]
    public void Error_WhenFailure_ShouldReturnCarriedError()
    {
        var error = ResultError.FromMessage("boom", "Code");

        Assert.AreEqual(error, Result.Failure(error).Error);
    }

    /// <summary>
    /// Verifies that accessing Error on a success throws <see cref="InvalidOperationException" /> with the
    /// documented message.
    /// </summary>
    [TestMethod]
    public void Error_WhenSuccess_ShouldThrowInvalidOperationException()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Result.Success().Error;
        });

        Assert.IsTrue(ex.Message.Contains("does not represent a failure", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that TryGetError yields the error for a failure and reports absence for a success.
    /// </summary>
    [TestMethod]
    public void TryGetError_WhenFailureOrSuccess_ShouldReportErrorPresence()
    {
        var error = ResultError.FromMessage("boom");

        Assert.IsTrue(Result.Failure(error).TryGetError(out var fromFailure));
        Assert.AreEqual(error, fromFailure);

        Assert.IsFalse(Result.Success().TryGetError(out var fromSuccess));
        Assert.AreEqual(default(ResultError), fromSuccess);
    }

    /// <summary>
    /// Verifies that ToString renders <c>Success</c> for a success and <c>Failure(error)</c> for a failure.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSuccessOrFailure_ShouldRenderState()
    {
        Assert.AreEqual("Success", Result.Success().ToString());
        Assert.AreEqual("Failure(boom)", Result.Failure("boom").ToString());
        Assert.AreEqual("Failure(Code: boom)", Result.Failure(ResultError.FromMessage("boom", "Code")).ToString());
    }
}
