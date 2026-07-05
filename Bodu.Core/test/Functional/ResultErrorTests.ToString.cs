// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultErrorTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultErrorTests
{
    /// <summary>
    /// Verifies that an error without a code renders as the bare message.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCodeIsNull_ShouldRenderMessageOnly()
    {
        Assert.AreEqual("boom", ResultError.FromMessage("boom").ToString());
    }

    /// <summary>
    /// Verifies that an error with a code renders as <c>Code: Message</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCodePresent_ShouldRenderCodeColonMessage()
    {
        Assert.AreEqual("IO.NotFound: boom", ResultError.FromMessage("boom", "IO.NotFound").ToString());
    }

    /// <summary>
    /// Verifies that <c>default(ResultError)</c> renders as the empty string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldRenderEmptyString()
    {
        Assert.AreEqual(string.Empty, default(ResultError).ToString());
    }
}
