// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultTests.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultTests
{
    /// <summary>
    /// Verifies that the parameterless Success factory produces a successful non-generic result.
    /// </summary>
    [TestMethod]
    public void Success_WhenCreated_ShouldRepresentSuccess()
    {
        Assert.IsTrue(Result.Success().IsSuccess);
    }

    /// <summary>
    /// Verifies that the non-generic Failure factory carries the supplied error.
    /// </summary>
    [TestMethod]
    public void Failure_WhenCreated_ShouldCarrySuppliedError()
    {
        var error = ResultError.FromMessage("boom", "Code");
        var result = Result.Failure(error);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }

    /// <summary>
    /// Verifies that the generic Success factory produces a successful <see cref="Result{T}" /> carrying the value.
    /// </summary>
    [TestMethod]
    public void Success_ForGenericOverload_ShouldProduceSuccessfulGenericResult()
    {
        var result = Result.Success(42);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(42, result.Value);
    }

    /// <summary>
    /// Verifies that the strict generic Success factory rejects <see langword="null" /> with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Success_WhenValueIsNull_ForGenericOverload_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Success<string>(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the generic Failure factory produces a failed <see cref="Result{T}" /> carrying the error.
    /// </summary>
    [TestMethod]
    public void Failure_ForGenericOverload_ShouldProduceFailedGenericResult()
    {
        var error = ResultError.FromMessage("boom");
        var result = Result.Failure<int>(error);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(error, result.Error);
    }
}
