// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Filter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that Filter with a fixed error retains a success when the carried value satisfies the predicate.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSuccessAndPredicateMatches_ForFixedError_ShouldReturnSameResult()
    {
        var source = Result.Success(10);

        var filtered = source.Filter(v => v > 5, ResultError.FromMessage("too small"));

        Assert.AreEqual(source, filtered);
    }

    /// <summary>
    /// Verifies that Filter with a fixed error demotes a success to a failure carrying that error when the carried
    /// value does not satisfy the predicate.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSuccessAndPredicateFails_ForFixedError_ShouldReturnFailureWithError()
    {
        var filtered = Result.Success(3).Filter(v => v > 5, ResultError.FromMessage("too small"));

        Assert.IsTrue(filtered.IsFailure);
        Assert.AreEqual("too small", filtered.Error.Message);
    }

    /// <summary>
    /// Verifies that Filter propagates an existing failure unchanged and does not invoke the predicate.
    /// </summary>
    [TestMethod]
    public void Filter_WhenFailure_ForFixedError_ShouldReturnSameFailureWithoutInvokingPredicate()
    {
        var invoked = false;
        var source = Result.Failure<int>("boom");

        var filtered = source.Filter(
            _ =>
            {
                invoked = true;
                return true;
            },
            ResultError.FromMessage("replacement"));

        Assert.IsFalse(invoked);
        Assert.AreEqual(source, filtered);
    }

    /// <summary>
    /// Verifies that Filter with an error factory builds the failure error from the rejected value.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSuccessAndPredicateFails_ForErrorFactory_ShouldReturnFailureFromValue()
    {
        var filtered = Result.Success(3).Filter(v => v > 5, v => ResultError.FromMessage($"rejected {v}"));

        Assert.IsTrue(filtered.IsFailure);
        Assert.AreEqual("rejected 3", filtered.Error.Message);
    }

    /// <summary>
    /// Verifies that Filter with an error factory does not invoke the factory when the predicate is satisfied.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSuccessAndPredicateMatches_ForErrorFactory_ShouldNotInvokeFactory()
    {
        var factoryInvoked = false;
        var source = Result.Success(10);

        var filtered = source.Filter(
            v => v > 5,
            _ =>
            {
                factoryInvoked = true;
                return ResultError.FromMessage("unused");
            });

        Assert.IsFalse(factoryInvoked);
        Assert.AreEqual(source, filtered);
    }

    /// <summary>
    /// Verifies that Filter rejects a <see langword="null" /> predicate.
    /// </summary>
    [TestMethod]
    public void Filter_WhenPredicateIsNull_ForFixedError_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).Filter(null!, ResultError.FromMessage("x"));
        });

        Assert.AreEqual("predicate", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Filter rejects a <see langword="null" /> error factory.
    /// </summary>
    [TestMethod]
    public void Filter_WhenErrorFactoryIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success(1).Filter(v => v > 0, (Func<int, ResultError>)null!);
        });

        Assert.AreEqual("errorFactory", ex.ParamName);
    }
}
