// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.ToOption.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that ToOption produces Some carrying the value for a success.
    /// </summary>
    [TestMethod]
    public void ToOption_WhenSuccess_ShouldProduceSomeCarryingValue()
    {
        Assert.AreEqual(Option<int>.Some(42), Result.Success(42).ToOption());
    }

    /// <summary>
    /// Verifies that ToOption produces None for a failure, discarding the error.
    /// </summary>
    [TestMethod]
    public void ToOption_WhenFailure_ShouldProduceNone()
    {
        Assert.IsTrue(Result.Failure<int>("boom").ToOption().IsNone);
    }
}
