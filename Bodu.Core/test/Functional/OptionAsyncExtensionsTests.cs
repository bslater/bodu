// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionAsyncExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class OptionAsyncExtensionsTests
{
    /// <summary>
    /// Verifies that a value survives a Task-lifted Some → MapAsync → MatchAsync round-trip, exercising the primary
    /// asynchronous railway path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public async Task OptionAsync_WhenChainedThroughMapAsyncAndMatchAsync_ShouldProduceProjectedValue()
    {
        var source = Task.FromResult(Option<int>.Some(21));

        var result = await source
            .MapAsync(v => v * 2)
            .MatchAsync(v => v.ToString(), () => "none");

        Assert.AreEqual("42", result);
    }
}
