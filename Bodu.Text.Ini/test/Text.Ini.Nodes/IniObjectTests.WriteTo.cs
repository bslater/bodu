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

    /// <summary>
    /// Verifies that writing a tree whose value contains a line break throws <see cref="ArgumentException" /> rather
    /// than splitting the entry across lines.
    /// </summary>
    /// <param name="value">The value of the entry.</param>
    [TestMethod]
    [DataRow("a\nb", DisplayName = "LF")]
    [DataRow("a\rb", DisplayName = "CR")]
    [DataRow("a\r\nb", DisplayName = "CRLF")]
    public void WriteTo_WhenValueContainsLineBreak_ShouldThrowArgumentException(string value)
    {
        var root = new IniObject();
        root["key"] = new IniValue(value);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that writing a tree whose value begins or ends with whitespace throws <see cref="ArgumentException" />
    /// rather than writing a value the reader would trim.
    /// </summary>
    /// <param name="value">The value of the entry.</param>
    [TestMethod]
    [DataRow(" a", DisplayName = "leading space")]
    [DataRow("a\t", DisplayName = "trailing tab")]
    [DataRow(" ", DisplayName = "only a space")]
    public void WriteTo_WhenValueHasSurroundingWhitespace_ShouldThrowArgumentException(string value)
    {
        var root = new IniObject();
        root["key"] = new IniValue(value);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that writing a tree with a key the reader would read back as something else throws
    /// <see cref="ArgumentException" /> rather than writing a different document.
    /// </summary>
    /// <param name="name">The key of the entry.</param>
    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow(" k", DisplayName = "leading space")]
    [DataRow("a=b", DisplayName = "equals sign")]
    [DataRow("a\nb", DisplayName = "line break")]
    [DataRow("[disturbing]", DisplayName = "leading bracket")]
    [DataRow(";k", DisplayName = "leading semicolon")]
    [DataRow("#k", DisplayName = "leading hash")]
    public void WriteTo_WhenKeyWouldReadBackDifferently_ShouldThrowArgumentException(string name)
    {
        var root = new IniObject();
        root[name] = new IniValue("v");

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that writing a tree with a section name the reader would read back as something else throws
    /// <see cref="ArgumentException" /> rather than writing a different document.
    /// </summary>
    /// <param name="name">The name of the section.</param>
    [TestMethod]
    [DataRow("", DisplayName = "empty")]
    [DataRow("s ", DisplayName = "trailing space")]
    [DataRow("a\rb", DisplayName = "line break")]
    [DataRow("a];b", DisplayName = "bracket then comment marker")]
    public void WriteTo_WhenSectionNameWouldReadBackDifferently_ShouldThrowArgumentException(string name)
    {
        var root = new IniObject();
        var section = new IniObject();
        section["k"] = new IniValue("v");
        root[name] = section;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that a leading comment holding a line break is written as one comment line per line of its text.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenCommentContainsLineBreak_ShouldWriteEachLineAsAComment()
    {
        var root = new IniObject();
        var value = new IniValue("v");
        value.LeadingComments.Add("first\r\nsecond");
        root["key"] = value;

        Assert.AreEqual(";first\n;second\nkey=v\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }
}
