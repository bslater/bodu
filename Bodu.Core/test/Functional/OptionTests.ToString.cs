// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that a present value renders as <c>Some(value)</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSome_ShouldRenderSomeWithValue()
    {
        Assert.AreEqual("Some(42)", Option<int>.Some(42).ToString());
    }

    /// <summary>
    /// Verifies that an absent value renders as <c>None</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNone_ShouldRenderNone()
    {
        Assert.AreEqual("None", Option<int>.None.ToString());
        Assert.AreEqual("None", default(Option<string>).ToString());
    }
}
