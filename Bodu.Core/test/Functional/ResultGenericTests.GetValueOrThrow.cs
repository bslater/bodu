// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.GetValueOrThrow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that GetValueOrThrow returns the carried value for a success.
    /// </summary>
    [TestMethod]
    public void GetValueOrThrow_WhenSuccess_ShouldReturnCarriedValue()
    {
        Assert.AreEqual(42, Result.Success(42).GetValueOrThrow());
    }

    /// <summary>
    /// Verifies that GetValueOrThrow on a failure without a captured exception throws
    /// <see cref="InvalidOperationException" /> carrying the error message and no inner exception.
    /// </summary>
    [TestMethod]
    public void GetValueOrThrow_WhenFailureWithoutException_ShouldThrowWithErrorMessageAndNoInner()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Result.Failure<int>("boom").GetValueOrThrow();
        });

        Assert.AreEqual("boom", ex.Message);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that GetValueOrThrow on a failure carrying an exception never rethrows it directly: a fresh
    /// <see cref="InvalidOperationException" /> is thrown with the captured exception attached as the inner
    /// exception.
    /// </summary>
    [TestMethod]
    public void GetValueOrThrow_WhenFailureWithException_ShouldAttachCapturedExceptionAsInner()
    {
        var captured = new FormatException("kaboom");

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Result.Failure<int>(ResultError.FromException(captured)).GetValueOrThrow();
        });

        Assert.AreEqual("kaboom", ex.Message);
        Assert.IsNotNull(ex.InnerException);
        Assert.AreSame(captured, ex.InnerException);
    }

    /// <summary>
    /// Verifies that GetValueOrThrow on a failure with an empty error message falls back to the documented
    /// not-a-success message.
    /// </summary>
    [TestMethod]
    public void GetValueOrThrow_WhenErrorMessageIsEmpty_ShouldThrowWithFallbackMessage()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = default(Result<int>).GetValueOrThrow();
        });

        Assert.IsTrue(ex.Message.Contains("does not represent a success", StringComparison.Ordinal));
        Assert.IsNull(ex.InnerException);
    }
}
