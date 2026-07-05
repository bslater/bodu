// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that two successes with equal values compare equal and share a hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothSuccessWithEqualValues_ShouldBeEqual()
    {
        var left = Result.Success("value");
        var right = Result.Success("value");

        Assert.IsTrue(left.Equals(right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that successes with different values are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothSuccessWithDifferentValues_ShouldNotBeEqual()
    {
        Assert.IsFalse(Result.Success(1).Equals(Result.Success(2)));
    }

    /// <summary>
    /// Verifies that two failures are equal exactly when their errors are equal, and share a hash code when equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothFailure_ShouldBeEqualOnlyWhenErrorsMatch()
    {
        var left = Result.Failure<int>("boom");
        var right = Result.Failure<int>("boom");

        Assert.IsTrue(left.Equals(right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        Assert.IsFalse(left.Equals(Result.Failure<int>("bang")));
    }

    /// <summary>
    /// Verifies that a success and a failure are never equal, in either direction.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSuccessComparedToFailure_ShouldNotBeEqual()
    {
        Assert.IsFalse(Result.Success(0).Equals(Result.Failure<int>("boom")));
        Assert.IsFalse(Result.Failure<int>("boom").Equals(Result.Success(0)));
    }

    /// <summary>
    /// Verifies the boxed Equals overload accepts only results of the same type.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToBoxedObject_ShouldMatchOnlySameTypedResult()
    {
        var result = Result.Success(1);

        Assert.IsTrue(result.Equals((object)Result.Success(1)));
        Assert.IsFalse(result.Equals((object)1));
        Assert.IsFalse(result.Equals((object)Result.Success(1L)));
        Assert.IsFalse(result.Equals(null));
    }

    /// <summary>
    /// Verifies that the equality operators agree with <see cref="Result{T}.Equals(Result{T})" />.
    /// </summary>
    [TestMethod]
    public void EqualityOperators_WhenComparingResults_ShouldMatchEqualsSemantics()
    {
        Assert.IsTrue(Result.Success(1) == Result.Success(1));
        Assert.IsTrue(Result.Success(1) != Result.Success(2));
        Assert.IsTrue(Result.Failure<int>("boom") == Result.Failure<int>("boom"));
        Assert.IsTrue(Result.Success(1) != Result.Failure<int>("boom"));
        Assert.IsTrue(default(Result<int>) == Result.Failure<int>(default));
    }
}
