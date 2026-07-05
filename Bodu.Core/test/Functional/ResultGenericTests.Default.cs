// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies the default-value contract: <c>default(Result&lt;T&gt;)</c> is a failure carrying the empty error,
    /// and <see cref="Result{T}.Error" /> on it is valid.
    /// </summary>
    [TestMethod]
    public void Default_WhenUninitialized_ShouldBeFailureWithEmptyError()
    {
        var result = default(Result<int>);

        Assert.IsTrue(result.IsFailure);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(string.Empty, result.Error.Message);
        Assert.IsNull(result.Error.Code);
        Assert.IsNull(result.Error.Exception);
    }

    /// <summary>
    /// Verifies that <c>default(Result&lt;T&gt;)</c> equals an explicit failure carrying the default error.
    /// </summary>
    [TestMethod]
    public void Default_WhenComparedToExplicitEmptyFailure_ShouldBeEqual()
    {
        Assert.AreEqual(Result.Failure<int>(default), default(Result<int>));
    }

    /// <summary>
    /// Verifies that the combinators are total on <c>default(Result&lt;T&gt;)</c>: Map and Bind propagate the empty
    /// failure without invoking their selectors.
    /// </summary>
    [TestMethod]
    public void Default_WhenMappedOrBound_ShouldPropagateEmptyFailureWithoutInvokingSelectors()
    {
        var invoked = false;

        var mapped = default(Result<int>).Map(v =>
        {
            invoked = true;
            return v + 1;
        });
        var bound = default(Result<int>).Bind(v =>
        {
            invoked = true;
            return Result.Success(v + 1);
        });

        Assert.IsFalse(invoked);
        Assert.IsTrue(mapped.IsFailure);
        Assert.AreEqual(default(ResultError), mapped.Error);
        Assert.IsTrue(bound.IsFailure);
        Assert.AreEqual(default(ResultError), bound.Error);
    }

    /// <summary>
    /// Verifies that Match on <c>default(Result&lt;T&gt;)</c> is total and hits the failure branch.
    /// </summary>
    [TestMethod]
    public void Default_WhenMatched_ShouldInvokeFailureBranch()
    {
        var result = default(Result<int>).Match(v => v.ToString(), error => $"failed: '{error.Message}'");

        Assert.AreEqual("failed: ''", result);
    }
}
