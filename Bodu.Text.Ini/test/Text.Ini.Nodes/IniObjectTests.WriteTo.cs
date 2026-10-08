// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.WriteTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Contains the member tests for <see cref="IniObject.WriteTo" />: the text a tree writes, its comment trivia, and the
/// two-level limit it enforces.
/// </summary>
public partial class IniObjectTests
{
    /// <summary>
    /// Verifies that a parsed document writes back to identical INI text, including its comment trivia.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenRoundTripped_ShouldPreserveEntriesAndComments()
    {
        const string source = "g=1\n; lead\n[db]\n; note\nhost=x\n; tail\n";
        IniObject root = IniNode.Parse(Encoding.UTF8.GetBytes(source));

        Assert.AreEqual(source, Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that a global value added after a section is still emitted before the first section header, because a
    /// key written after a header would be re-read as belonging to that section.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenGlobalAddedAfterSection_ShouldEmitGlobalEntriesFirst()
    {
        var root = new IniObject();
        var section = new IniObject();
        section["host"] = new IniValue("x");
        root["db"] = section;
        root["g"] = new IniValue("1");

        Assert.AreEqual("g=1\n[db]\nhost=x\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that writing an object nested inside a section throws <see cref="InvalidOperationException" />,
    /// because INI cannot represent a third level.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenObjectNestedInSection_ShouldThrowInvalidOperationException()
    {
        var root = new IniObject();
        var section = new IniObject();
        section["inner"] = new IniObject();
        root["s"] = section;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }
}
