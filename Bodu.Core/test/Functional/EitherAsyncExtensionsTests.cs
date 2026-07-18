// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherAsyncExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class EitherAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that a left value survives a Task-lifted MapLeftAsync then MatchAsync round-trip, exercising the
    /// primary asynchronous path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task EitherAsync_WhenChainedThroughMapLeftAsyncAndMatchAsync_ShouldProduceProjectedValue()
    {
        var source = Task.FromResult(Either<int, string>.Left(21));

        var result = await source
            .MapLeftAsync(v => v * 2)
            .MatchAsync(l => $"left {l}", r => $"right {r}");

        Assert.AreEqual("left 42", result);
    }
}
