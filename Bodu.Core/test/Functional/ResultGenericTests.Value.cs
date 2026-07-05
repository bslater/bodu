// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Value.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that Value returns the carried value when the result represents success.
    /// </summary>
    [TestMethod]
    public void Value_WhenSuccess_ShouldReturnCarriedValue()
    {
        var result = Result.Success(7);

        Assert.AreEqual(7, result.Value);
    }

    /// <summary>
    /// Verifies that accessing Value on a failure throws <see cref="InvalidOperationException" /> with the
    /// documented message.
    /// </summary>
    [TestMethod]
    public void Value_WhenFailure_ShouldThrowInvalidOperationException()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Result.Failure<int>("boom").Value;
        });

        Assert.IsTrue(ex.Message.Contains("does not represent a success", StringComparison.Ordinal));
    }
}
