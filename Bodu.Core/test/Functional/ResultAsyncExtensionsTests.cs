// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultAsyncExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class ResultAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that a value survives a Task-lifted Success → MapAsync → MatchAsync round-trip, exercising the
    /// primary asynchronous railway path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task ResultAsync_WhenChainedThroughMapAsyncAndMatchAsync_ShouldProduceProjectedValue()
    {
        var source = Task.FromResult(Result.Success(21));

        var result = await source
            .MapAsync(v => v * 2)
            .MatchAsync(v => v.ToString(), error => $"failed: {error.Message}");

        Assert.AreEqual("42", result);
    }
}
