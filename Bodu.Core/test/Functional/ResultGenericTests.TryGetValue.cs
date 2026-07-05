// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that TryGetValue yields the carried value and returns <see langword="true" /> for a success.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenSuccess_ShouldReturnTrueAndValue()
    {
        var found = Result.Success("value").TryGetValue(out var value);

        Assert.IsTrue(found);
        Assert.AreEqual("value", value);
    }

    /// <summary>
    /// Verifies that TryGetValue yields the default value and returns <see langword="false" /> for a failure.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenFailure_ShouldReturnFalseAndDefault()
    {
        var found = Result.Failure<string>("boom").TryGetValue(out var value);

        Assert.IsFalse(found);
        Assert.IsNull(value);
    }
}
