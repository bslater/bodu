// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvObjectTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Nodes;

/// <summary>
/// Contains the <see cref="DotEnvNode.Parse(ReadOnlySpan{byte})" /> tests that build a <see cref="DotEnvObject" />.
/// </summary>
public partial class DotEnvObjectTests
{
    /// <summary>
    /// Verifies that a key defined twice leaves one entry holding the value of its last definition.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyRepeated_ShouldKeepLastValue()
    {
        DotEnvObject root = DotEnvNode.Parse("MULTI1=foo\nMULTI1=bar\n"u8);

        Assert.AreEqual("bar", root["MULTI1"].Value);
        Assert.AreEqual(1, root.Count);
    }
}
