// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.MultiDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies multi-document stream parsing through <see cref="YamlDocument.ParseAllDocuments(string)" />, and how the
/// single-document <see cref="YamlDocument.Parse(string)" /> reads a stream of several documents.
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

    /// <summary>
    /// Verifies that <see cref="YamlDocument.Parse(string)" /> reads the first document of a two-document stream, as the
    /// single-document entry point is documented to do, while <see cref="YamlDocument.ParseAllDocuments(string)" />
    /// returns both documents.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStreamHoldsTwoDocuments_ShouldReturnFirstDocument()
    {
        const string Yaml = "a: 1\n---\nb: 2\n";

        using var document = YamlDocument.Parse(Yaml);
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments(Yaml);

        Assert.AreEqual(YamlValueKind.Mapping, document.RootElement.ValueKind);
        Assert.AreEqual(1L, document.RootElement.GetProperty("a").GetInt64());
        Assert.IsFalse(document.RootElement.TryGetProperty("b", out _));
        Assert.AreEqual(2, documents.Count);
    }

    /// <summary>
    /// Verifies that a byte-order mark before each document's <c>---</c>, as concatenated files carry, is skipped, so
    /// each document reads as it would on its own.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenByteOrderMarkPrecedesEachDocumentStart_ShouldSkipEach()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments(
            "\uFEFF---\na: 1\n\uFEFF---\nb: 2\n...\n\uFEFF---\nc: 3\n");

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, KeysOf(documents));
    }

    /// <summary>
    /// Verifies that a byte-order mark before the <c>---</c> of a later document ends the document before it and is
    /// skipped, rather than being read as a mapping key.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenByteOrderMarkPrecedesLaterDocumentStart_ShouldSkipIt()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments("a: 1\n\uFEFF---\nb: 2\n");

        CollectionAssert.AreEqual(new[] { "a", "b" }, KeysOf(documents));
    }

    /// <summary>
    /// Verifies that a byte-order mark followed by a comment line before a later document's <c>---</c> is skipped
    /// with the comment.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenByteOrderMarkAndCommentPrecedeDocumentStart_ShouldSkipBoth()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments("a: 1\n\uFEFF# next\n---\nb: 2\n");

        CollectionAssert.AreEqual(new[] { "a", "b" }, KeysOf(documents));
    }

    /// <summary>
    /// Verifies that a byte-order mark at the start of the line after a document's <c>---</c> is skipped, so the key
    /// it precedes does not begin with it.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenByteOrderMarkFollowsDocumentStart_ShouldSkipIt()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments("\uFEFFa: 1\n---\n\uFEFFb: 2\n");

        CollectionAssert.AreEqual(new[] { "a", "b" }, KeysOf(documents));
    }

    /// <summary>
    /// Verifies that a byte-order mark before the content of a document that follows a <c>...</c> document end
    /// marker is skipped.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenByteOrderMarkPrecedesContentAfterDocumentEnd_ShouldSkipIt()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments("a: 1\n...\n\uFEFFb: 2\n");

        CollectionAssert.AreEqual(new[] { "a", "b" }, KeysOf(documents));
    }

    /// <summary>
    /// Verifies that a byte-order mark before the directives of a document that follows a <c>...</c> document end
    /// marker is skipped, so the directive applies.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenByteOrderMarkPrecedesDirective_ShouldSkipIt()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments(
            "a: 1\n...\n\uFEFF%YAML 1.2\n---\nb: 2\n");

        CollectionAssert.AreEqual(new[] { "a", "b" }, KeysOf(documents));
    }

    /// <summary>
    /// Verifies that a byte-order mark inside a double-quoted scalar is content, which YAML permits there, and is kept.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenByteOrderMarkIsInsideQuotedScalar_ShouldKeepIt()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments("---\na: \"\uFEFFb\"\n---\nc: 3\n");

        Assert.AreEqual("\uFEFFb", documents[0].RootElement.GetProperty("a").GetString());
    }

    /// <summary>
    /// Verifies that <see cref="YamlDocument.Parse(string)" /> ends the first document at a byte-order mark before a
    /// later document's <c>---</c> and returns it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenByteOrderMarkPrecedesLaterDocumentStart_ShouldReturnFirstDocument()
    {
        using var document = YamlDocument.Parse("a: 1\n\uFEFF---\nb: 2\n");

        CollectionAssert.AreEqual(new[] { "a" }, KeysOf([document]));
    }

    /// <summary>
    /// Verifies that <see cref="YamlDocument.Parse(string)" /> skips a byte-order mark at the start of the line after
    /// the document's <c>---</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenByteOrderMarkFollowsDocumentStart_ShouldSkipIt()
    {
        using var document = YamlDocument.Parse("---\n\uFEFFa: 1\n");

        CollectionAssert.AreEqual(new[] { "a" }, KeysOf([document]));
    }

    /// <summary>
    /// Verifies that content following a complete root node with no <c>---</c> or <c>...</c> marker between them is
    /// rejected, as <see cref="YamlDocument.Parse(string)" /> rejects it, rather than read as a further document.
    /// </summary>
    /// <param name="yaml">The stream: a root node, then more content with no document marker before it.</param>
    [TestMethod]
    [DataRow("one # comment\ntwo\n", DisplayName = "plain scalar and comment, then a plain scalar")]
    [DataRow("word1  # comment\nword2\n", DisplayName = "plain scalar and spaced comment, then a plain scalar")]
    [DataRow("[ 42 ]\nhello\n", DisplayName = "flow sequence, then a plain scalar")]
    [DataRow("- \"foo\"\n - bar\n", DisplayName = "block sequence, then an entry indented past it")]
    [DataRow("foo: \"1\"\n bar: 2\n", DisplayName = "block mapping, then a key indented past it")]
    [DataRow("-\n-0", DisplayName = "block sequence, then a plain scalar")]
    public void ParseAllDocuments_WhenContentFollowsRootNodeWithoutMarker_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            _ = YamlDocument.ParseAllDocuments(yaml);
        });
    }

    /// <summary>
    /// Verifies that text other than a comment after a <c>...</c> document end marker on its line is rejected, whether
    /// the marker ends a document or follows an earlier marker.
    /// </summary>
    /// <param name="yaml">The stream, with text after a <c>...</c> marker.</param>
    [TestMethod]
    [DataRow("---\nkey: value\n... invalid\n", DisplayName = "after an explicit document")]
    [DataRow("hello: world\n... this is no comment\n", DisplayName = "after a bare document")]
    [DataRow("a: 1\n...\n... invalid\n", DisplayName = "after an earlier document end marker")]
    public void ParseAllDocuments_WhenTextFollowsDocumentEndMarker_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            _ = YamlDocument.ParseAllDocuments(yaml);
        });
    }

    /// <summary>
    /// Verifies that a directive after a document with no <c>...</c> document end marker between them is rejected.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenDirectiveFollowsDocumentWithoutDocumentEnd_ShouldThrowYamlFormatException()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            _ = YamlDocument.ParseAllDocuments("---\nscalar1 # comment\n%YAML 1.2\n---\nscalar2\n");
        });
    }

    /// <summary>
    /// Verifies that a comment after a <c>...</c> document end marker on its line is accepted, and the document that
    /// follows is read.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenCommentFollowsDocumentEndMarker_ShouldReadNextDocument()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments("a: 1\n... # end\nb: 2\n");

        CollectionAssert.AreEqual(new[] { "a", "b" }, KeysOf(documents));
    }

    /// <summary>
    /// Verifies that a directive after a <c>...</c> document end marker is accepted and applies to the document that
    /// follows.
    /// </summary>
    [TestMethod]
    public void ParseAllDocuments_WhenDirectiveFollowsDocumentEnd_ShouldApplyToNextDocument()
    {
        IReadOnlyList<YamlDocument> documents = YamlDocument.ParseAllDocuments("a: 1\n...\n%YAML 1.1\n---\nb: yes\n");

        Assert.AreEqual(2, documents.Count);
        Assert.IsTrue(documents[1].RootElement.GetProperty("b").GetBoolean());
    }

    /// <summary>
    /// Lists the keys of each document's root mapping, comma-separated, with any byte-order mark shown as
    /// <c>&lt;BOM&gt;</c> so that a failure names it.
    /// </summary>
    /// <param name="documents">The documents, each with a mapping at its root.</param>
    /// <returns>One entry per document.</returns>
    private static string[] KeysOf(IReadOnlyList<YamlDocument> documents) =>
        [.. documents.Select(document =>
            string.Join(',', document.RootElement.EnumerateMapping().Select(property => property.Name))
                .Replace("\uFEFF", "<BOM>", StringComparison.Ordinal))];
}
