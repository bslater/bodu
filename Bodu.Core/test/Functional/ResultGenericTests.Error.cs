// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Error.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that Error returns the carried error when the result represents failure.
    /// </summary>
    [TestMethod]
    public void Error_WhenFailure_ShouldReturnCarriedError()
    {
        var error = ResultError.FromMessage("boom", "Code");

        Assert.AreEqual(error, Result.Failure<int>(error).Error);
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
            _ = Result.Success(1).Error;
        });

        Assert.IsTrue(ex.Message.Contains("does not represent a failure", StringComparison.Ordinal));
    }
}
