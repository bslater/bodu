// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultErrorTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultErrorTests
{
    /// <summary>
    /// Verifies that two errors with equal code and message and no exception compare equal and share a hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenCodeAndMessageMatch_ShouldBeEqual()
    {
        var left = ResultError.FromMessage("boom", "Code");
        var right = ResultError.FromMessage("boom", "Code");

        Assert.IsTrue(left.Equals(right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that errors differing by message or by code are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenMessageOrCodeDiffers_ShouldNotBeEqual()
    {
        Assert.IsFalse(ResultError.FromMessage("boom").Equals(ResultError.FromMessage("bang")));
        Assert.IsFalse(ResultError.FromMessage("boom", "A").Equals(ResultError.FromMessage("boom", "B")));
        Assert.IsFalse(ResultError.FromMessage("boom", "A").Equals(ResultError.FromMessage("boom")));
    }

    /// <summary>
    /// Verifies that two errors created from the same exception instance compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenExceptionIsSameReference_ShouldBeEqual()
    {
        var source = new InvalidOperationException("kaboom");
        var left = ResultError.FromException(source);
        var right = ResultError.FromException(source);

        Assert.IsTrue(left.Equals(right));
    }

    /// <summary>
    /// Verifies the exception-by-reference pin: equivalent but distinct exception instances make errors unequal,
    /// while the hash code — which excludes the exception — stays identical.
    /// </summary>
    [TestMethod]
    public void Equals_WhenExceptionsAreDistinctButEquivalent_ShouldNotBeEqualYetHashIdentically()
    {
        var left = ResultError.FromException(new InvalidOperationException("kaboom"));
        var right = ResultError.FromException(new InvalidOperationException("kaboom"));

        Assert.IsFalse(left.Equals(right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that <c>default(ResultError)</c> equals an explicitly created error with an empty message and no
    /// code, because the default's message normalizes to <see cref="string.Empty" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDefaultComparedToEmptyMessageError_ShouldBeEqual()
    {
        Assert.IsTrue(default(ResultError).Equals(ResultError.FromMessage(string.Empty)));
        Assert.AreEqual(default(ResultError).GetHashCode(), ResultError.FromMessage(string.Empty).GetHashCode());
    }

    /// <summary>
    /// Verifies the boxed Equals overload accepts only other <see cref="ResultError" /> values.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToBoxedObject_ShouldMatchOnlyResultError()
    {
        var error = ResultError.FromMessage("boom");

        Assert.IsTrue(error.Equals((object)ResultError.FromMessage("boom")));
        Assert.IsFalse(error.Equals((object)"boom"));
        Assert.IsFalse(error.Equals((object?)null));
    }
}
