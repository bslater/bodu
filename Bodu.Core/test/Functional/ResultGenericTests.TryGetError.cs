// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.TryGetError.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that TryGetError yields the carried error and returns <see langword="true" /> for a failure.
    /// </summary>
    [TestMethod]
    public void TryGetError_WhenFailure_ShouldReturnTrueAndError()
    {
        var error = ResultError.FromMessage("boom");

        var found = Result.Failure<int>(error).TryGetError(out var observed);

        Assert.IsTrue(found);
        Assert.AreEqual(error, observed);
    }

    /// <summary>
    /// Verifies that TryGetError yields the default error and returns <see langword="false" /> for a success.
    /// </summary>
    [TestMethod]
    public void TryGetError_WhenSuccess_ShouldReturnFalseAndDefault()
    {
        var found = Result.Success(1).TryGetError(out var observed);

        Assert.IsFalse(found);
        Assert.AreEqual(default(ResultError), observed);
    }
}
