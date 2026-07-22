// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentTests.LoadSaveStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Load(TextReader, ConfigurationParseOptions)" /> reads and parses
    /// from a text reader.
    /// </summary>
    [TestMethod]
    public void Load_FromTextReader_ShouldParseDocument()
    {
        using StringReader reader = new("[*]\na = 1\n");

        var document = ConfigurationDocument.Load(reader);

        Assert.AreEqual("1", document.Resolve("any.cs").GetString("a"));
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDocument.Save(IniDocumentBase, Stream,
    /// ConfigurationWriteOptions, bool)" /> writes the document to a stream and honors <c>leaveOpen</c>.
    /// </summary>
    [TestMethod]
    public void Save_ToStream_WhenLeaveOpen_ShouldWriteAndKeepStreamOpen()
    {
        var document = ConfigurationDocument.Parse("[*]\na = 1\n");
        using MemoryStream stream = new();

        ConfigurationDocument.Save(document, stream, options: null, leaveOpen: true);

        Assert.IsGreaterThan(0, stream.Length);
        Assert.IsTrue(stream.CanWrite);
    }
}
