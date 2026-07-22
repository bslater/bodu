// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Contains tests for <see cref="IniDocumentReader" />, verifying the normalized object-of-objects token stream and
/// the constructor-time policy enforcement.
/// </summary>
[TestClass]
public class IniDocumentReaderTests
{
    /// <summary>
    /// Reads every token and returns a compact transcript.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <returns>The token transcript.</returns>
    private static List<string> Transcribe(string source)
    {
        var reader = new IniDocumentReader(Encoding.UTF8.GetBytes(source));
        var tokens = new List<string>();

        while (reader.Read())
        {
            tokens.Add(reader.TokenType switch
            {
                IniTokenType.PropertyName => $"Name:{reader.GetString()}",
                IniTokenType.String => $"String:{reader.GetString()}",
                _ => reader.TokenType.ToString(),
            });
        }

        return tokens;
    }

    /// <summary>
    /// Verifies that duplicate sections merge into one object and global keys hoist to the root, producing the
    /// normalized token stream.
    /// </summary>
    [TestMethod]
    public void Read_WhenMergedDuplicateSections_ShouldEmitNormalizedStream()
    {
        List<string> tokens = Transcribe("key0=a\n[db]\nhost=x\n[db]\nport=5\n");

        CollectionAssert.AreEqual(
            new List<string>
            {
                "StartObject",
                "Name:key0", "String:a",
                "Name:db",
                "StartObject",
                "Name:host", "String:x",
                "Name:port", "String:5",
                "EndObject",
                "EndObject",
            },
            tokens);
    }

    /// <summary>
    /// Verifies that an empty document produces only the root object tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDocument_ShouldEmitRootObjectOnly()
    {
        List<string> tokens = Transcribe(string.Empty);

        CollectionAssert.AreEqual(new List<string> { "StartObject", "EndObject" }, tokens);
    }

    /// <summary>
    /// Verifies that a duplicate section under <see cref="IniDuplicateSectionBehavior.Disallowed" /> throws
    /// <see cref="IniFormatException" /> from the constructor.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDuplicateSectionDisallowed_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = new IniDocumentReader(
                "[db]\nhost=x\n[db]\nport=5\n"u8,
                IniReaderOptions.Default,
                new IniDocumentOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed });
        });
    }

    /// <summary>
    /// Verifies that a global key colliding with a section name throws <see cref="IniFormatException" /> from the
    /// constructor.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGlobalKeyCollidesWithSection_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = new IniDocumentReader("db=x\n[db]\nhost=y\n"u8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniDocumentReader.CurrentDepth" /> reports one while positioned inside a section
    /// object.
    /// </summary>
    [TestMethod]
    public void CurrentDepth_WhenInsideSection_ShouldBeOne()
    {
        var reader = new IniDocumentReader("[db]\nhost=x\n"u8);

        Assert.IsTrue(reader.Read()); // StartObject (root)
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.IsTrue(reader.Read()); // PropertyName "db"
        Assert.IsTrue(reader.Read()); // StartObject (section)
        Assert.IsTrue(reader.Read()); // PropertyName "host"
        Assert.AreEqual(IniTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocumentReader.GetString" /> on a structural token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetString_WhenStructuralToken_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new IniDocumentReader("a=1\n"u8);
            _ = reader.Read(); // StartObject
            _ = reader.GetString();
        });
    }
}
