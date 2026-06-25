// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MultiDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies multi-document stream handling and the document-start and document-end markers.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a multi-document stream yields each document in order.</summary>
    [TestMethod]
    public void ParseAllDocuments_WhenMultipleDocuments_ShouldYieldEach()
    {
        var docs = YamlDocument.ParseAllDocuments("---\na: 1\n---\nb: 2\n...\n");
        Assert.AreEqual(2, docs.Count);
        Assert.AreEqual(1L, docs[0].RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(2L, docs[1].RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that multiple documents separated by start markers are returned in order.</summary>
    [TestMethod]
    public void ParseAllDocuments_WhenMultipleStartMarkers_ShouldYieldEach()
    {
        var docs = YamlDocument.ParseAllDocuments("---\nkey1: value1\n---\nkey2: value2\n");
        Assert.AreEqual(2, docs.Count);
        Assert.AreEqual("value1", docs[0].RootElement.GetProperty("key1").GetString());
        Assert.AreEqual("value2", docs[1].RootElement.GetProperty("key2").GetString());
    }

    /// <summary>Verifies that a single-document source still yields one document through the stream API.</summary>
    [TestMethod]
    public void ParseAllDocuments_WhenSingleDocument_ShouldYieldOne()
    {
        var docs = YamlDocument.ParseAllDocuments("key: value\n");
        Assert.AreEqual(1, docs.Count);
        Assert.AreEqual("value", docs[0].RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that a single document followed by a document-end marker parses.</summary>
    [TestMethod]
    public void Parse_WhenDocumentEndMarker_ShouldParse()
    {
        using var d = Doc("key: value\n...\n");
        Assert.AreEqual("value", d.RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that inline content after a document-start marker parses.</summary>
    [TestMethod]
    public void Parse_WhenInlineAfterStartMarker_ShouldParse()
    {
        using var d = Doc("--- key: value\n");
        Assert.AreEqual("value", d.RootElement.GetProperty("key").GetString());
    }
}
