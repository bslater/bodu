// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

[TestClass]
public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that a value survives a Some → Map → Match round-trip, exercising the primary railway path.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Option_WhenChainedThroughMapAndMatch_ShouldProduceProjectedValue()
    {
        var option = Option<int>.Some(21);

        var result = option
            .Map(v => v * 2)
            .Match(v => v.ToString(), () => "none");

        Assert.AreEqual("42", result);
    }
}
