// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that a value survives a Success → Map → Bind → Match round-trip, exercising the primary railway
    /// path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Result_WhenChainedThroughMapBindAndMatch_ShouldProduceProjectedValue()
    {
        var result = Result.Success(20)
            .Map(v => v + 1)
            .Bind(v => Result.Success(v * 2))
            .Match(v => v.ToString(), error => $"failed: {error}");

        Assert.AreEqual("42", result);
    }
}
