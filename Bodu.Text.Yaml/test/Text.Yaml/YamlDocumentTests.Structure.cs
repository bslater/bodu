// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Structure.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies block structural parsing and document markers: mixed mapping/sequence nesting, multi-key mapping
/// entries, and the <c>---</c> / <c>...</c> document markers.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a mapping value that is a block sequence at the key column parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenMappingValueIsBlockSequence_ShouldParse()
    {
        using var doc = YamlDocument.Parse("key1: value1\nkey2:\n  - item1\n  - item2\n");
        Assert.AreEqual("value1", doc.RootElement.GetProperty("key1").GetString());
        Assert.AreEqual(2, doc.RootElement.GetProperty("key2").GetSequenceLength());
        Assert.AreEqual("item1", doc.RootElement.GetProperty("key2")[0].GetString());
    }

    /// <summary>Verifies that a sequence of mappings with multiple keys per entry parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenSequenceOfMultiKeyMappings_ShouldParse()
    {
        using var doc = YamlDocument.Parse("- name: a\n  age: 1\n- name: b\n  age: 2\n");
        Assert.AreEqual("a", doc.RootElement[0].GetProperty("name").GetString());
        Assert.AreEqual(2L, doc.RootElement[1].GetProperty("age").GetInt64());
    }

    /// <summary>Verifies that an inline scalar after a document-start marker parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenInlineScalarFollowsStartMarker_ShouldParse()
    {
        using var doc = YamlDocument.Parse("--- value\n");
        Assert.AreEqual("value", doc.RootElement.GetString());
    }

    /// <summary>Verifies that a block mapping beginning on the document-start line is rejected.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenInlineMappingFollowsStartMarker_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("--- key: value\n");
        });
    }

    /// <summary>Verifies that a single document followed by a document-end marker parses.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenDocumentEndMarkerFollows_ShouldParseBody()
    {
        using var doc = YamlDocument.Parse("key: value\n...\n");
        Assert.AreEqual("value", doc.RootElement.GetProperty("key").GetString());
    }
}
