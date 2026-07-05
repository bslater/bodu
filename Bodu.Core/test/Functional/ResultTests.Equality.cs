// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultTests
{
    /// <summary>
    /// Verifies that two successes compare equal and share a hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothSuccess_ShouldBeEqual()
    {
        Assert.IsTrue(Result.Success().Equals(Result.Success()));
        Assert.AreEqual(Result.Success().GetHashCode(), Result.Success().GetHashCode());
    }

    /// <summary>
    /// Verifies that two failures are equal exactly when their errors are equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothFailure_ShouldBeEqualOnlyWhenErrorsMatch()
    {
        Assert.IsTrue(Result.Failure("boom").Equals(Result.Failure("boom")));
        Assert.IsFalse(Result.Failure("boom").Equals(Result.Failure("bang")));
        Assert.AreEqual(Result.Failure("boom").GetHashCode(), Result.Failure("boom").GetHashCode());
    }

    /// <summary>
    /// Verifies that a success and a failure are never equal, in either direction.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSuccessComparedToFailure_ShouldNotBeEqual()
    {
        Assert.IsFalse(Result.Success().Equals(Result.Failure("boom")));
        Assert.IsFalse(Result.Failure("boom").Equals(Result.Success()));
    }

    /// <summary>
    /// Verifies the boxed Equals overload accepts only other <see cref="Result" /> values.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToBoxedObject_ShouldMatchOnlyResult()
    {
        var result = Result.Success();

        Assert.IsTrue(result.Equals((object)Result.Success()));
        Assert.IsFalse(result.Equals((object)Result.Failure("boom")));
        Assert.IsFalse(result.Equals((object)true));
        Assert.IsFalse(result.Equals(null));
    }

    /// <summary>
    /// Verifies that the equality operators agree with <see cref="Result.Equals(Result)" />.
    /// </summary>
    [TestMethod]
    public void EqualityOperators_WhenComparingResults_ShouldMatchEqualsSemantics()
    {
        Assert.IsTrue(Result.Success() == Result.Success());
        Assert.IsTrue(Result.Failure("boom") == Result.Failure("boom"));
        Assert.IsTrue(Result.Success() != Result.Failure("boom"));
        Assert.IsTrue(Result.Failure("boom") != Result.Failure("bang"));
    }
}
