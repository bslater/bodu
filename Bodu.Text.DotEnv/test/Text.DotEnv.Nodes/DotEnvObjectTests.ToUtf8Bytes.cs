// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvObjectTests.ToUtf8Bytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv.Nodes;

/// <summary>
/// Contains the <see cref="DotEnvNode.ToUtf8Bytes" /> tests for a <see cref="DotEnvObject" /> edited after it was
/// parsed.
/// </summary>
public partial class DotEnvObjectTests
{
    /// <summary>
    /// Verifies that a key added to an object parsed from text without a final newline is written on a line of its
    /// own, every line ending with a line feed, so that the output reads back as both entries.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenParsedWithoutFinalNewlineAndKeyAdded_ShouldWriteEachEntryOnItsOwnLine()
    {
        DotEnvObject root = DotEnvNode.Parse("a=b"u8);
        root["c"] = new DotEnvValue("d");

        byte[] written = root.ToUtf8Bytes();
        DotEnvObject readBack = DotEnvNode.Parse(written);

        Assert.AreEqual("a=b\nc=d\n", Encoding.UTF8.GetString(written));
        CollectionAssert.AreEqual(new List<string> { "a", "c" }, readBack.Keys.ToList());
        Assert.AreEqual("b", readBack["a"].Value);
        Assert.AreEqual("d", readBack["c"].Value);
    }

    /// <summary>
    /// Verifies that updating the value of an existing key writes it on its own line, leaving the entry that follows
    /// it on the next line, so that the output reads back as both entries.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenExistingKeyUpdated_ShouldKeepFollowingEntryOnItsOwnLine()
    {
        DotEnvObject root = DotEnvNode.Parse("HELLO=WORLD\nfoo=bar\n"u8);
        root["HELLO"] = new DotEnvValue("WORLD 2");

        byte[] written = root.ToUtf8Bytes();
        string[] lines = Encoding.UTF8.GetString(written).Split('\n');
        DotEnvObject readBack = DotEnvNode.Parse(written);

        CollectionAssert.AreEqual(new List<string> { "HELLO=WORLD 2", "foo=bar", string.Empty }, lines);
        CollectionAssert.AreEqual(new List<string> { "HELLO", "foo" }, readBack.Keys.ToList());
        Assert.AreEqual("WORLD 2", readBack["HELLO"].Value);
        Assert.AreEqual("bar", readBack["foo"].Value);
    }
}
