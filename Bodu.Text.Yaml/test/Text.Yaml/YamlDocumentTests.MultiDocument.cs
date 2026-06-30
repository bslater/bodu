// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MultiDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies multi-document stream parsing through <see cref="YamlDocument.ParseAllDocuments(string)" />.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a multi-document stream yields each document in order.</summary>
    [TestMethod]
    public void ParseAllDocuments_WhenMultipleDocuments_ShouldYieldEach()
    {
        IReadOnlyList<YamlDocument> docs = YamlDocument.ParseAllDocuments("---\na: 1\n---\nb: 2\n...\n");
        Assert.AreEqual(2, docs.Count);
        Assert.AreEqual(1L, docs[0].RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(2L, docs[1].RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that a single-document source still yields one document through the stream API.</summary>
    [TestMethod]
    public void ParseAllDocuments_WhenSingleDocument_ShouldYieldOne()
    {
        IReadOnlyList<YamlDocument> docs = YamlDocument.ParseAllDocuments("key: value\n");
        Assert.AreEqual(1, docs.Count);
        Assert.AreEqual("value", docs[0].RootElement.GetProperty("key").GetString());
    }

    /// <summary>Verifies that documents separated by start markers are returned in order.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void ParseAllDocuments_WhenDocumentsSeparatedByMarkers_ShouldYieldInOrder()
    {
        IReadOnlyList<YamlDocument> docs = YamlDocument.ParseAllDocuments("---\nkey1: value1\n---\nkey2: value2\n");
        Assert.AreEqual(2, docs.Count);
        Assert.AreEqual("value1", docs[0].RootElement.GetProperty("key1").GetString());
        Assert.AreEqual("value2", docs[1].RootElement.GetProperty("key2").GetString());
    }
}
