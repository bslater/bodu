// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Anchors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies anchor and alias composition: an alias resolves to its anchor's value or subtree, and an undefined alias,
/// a node with two anchors, an empty anchor name, and a property glued to its content are rejected.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that an alias resolves to the value of its anchor.</summary>
    [TestMethod]
    public void Parse_WhenAlias_ShouldResolveToAnchorValue()
    {
        using var doc = YamlDocument.Parse("a: &id 42\nb: *id\n");
        Assert.AreEqual(42L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(42L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that an alias to an anchored mapping resolves to the whole mapping.</summary>
    [TestMethod]
    public void Parse_WhenAliasToMapping_ShouldResolveSubtree()
    {
        using var doc = YamlDocument.Parse("base: &b\n  x: 1\n  y: 2\nuse: *b\n");
        YamlElement use = doc.RootElement.GetProperty("use");
        Assert.AreEqual(YamlValueKind.Mapping, use.ValueKind);
        Assert.AreEqual(1L, use.GetProperty("x").GetInt64());
        Assert.AreEqual(2L, use.GetProperty("y").GetInt64());
    }

    /// <summary>Verifies that an anchored mapping aliased elsewhere resolves to the same content.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenAnchoredMappingAliased_ShouldResolveContent()
    {
        using var doc = YamlDocument.Parse("defaults: &x\n  a: 1\n  b: 2\nconfig: *x\n");
        Assert.AreEqual(1L, doc.RootElement.GetProperty("config").GetProperty("a").GetInt64());
        Assert.AreEqual(2L, doc.RootElement.GetProperty("config").GetProperty("b").GetInt64());
    }

    /// <summary>Verifies that an undefined alias is rejected.</summary>
    [TestMethod]
    public void Parse_WhenUndefinedAlias_ShouldThrow()
    {
        YamlFormatException ex = Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var doc = YamlDocument.Parse("a: *missing\n");
        });

        Assert.IsTrue(ex.Message.Contains("missing", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a node with two anchors is rejected, rather than the second anchor being read as the start of a
    /// plain scalar or key.
    /// </summary>
    /// <param name="yaml">A document with a node that carries two anchors.</param>
    [TestMethod]
    [DataRow("&a &b x", DisplayName = "a scalar")]
    [DataRow("- &a &b x", DisplayName = "a sequence entry")]
    [DataRow("[ &a &b x ]", DisplayName = "a flow sequence entry")]
    [DataRow("&a &b key: v", DisplayName = "an implicit key")]
    public void Parse_WhenNodeHasTwoAnchors_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that an anchor with an empty name is rejected, whatever follows the <c>&amp;</c>.
    /// </summary>
    /// <param name="yaml">A document with an anchor indicator that has no name.</param>
    [TestMethod]
    [DataRow("- & foo", DisplayName = "followed by a space")]
    [DataRow("- &", DisplayName = "at the end of the input")]
    [DataRow("a: &\nb: 1\n", DisplayName = "at the end of a line")]
    [DataRow("[ &, a ]", DisplayName = "followed by a comma")]
    public void Parse_WhenAnchorNameIsEmpty_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that a node property followed directly by a flow collection, with no separating space, is rejected.
    /// </summary>
    /// <param name="yaml">A document with a node property glued to a flow collection.</param>
    [TestMethod]
    [DataRow("&a{}", DisplayName = "an anchor before a flow mapping")]
    [DataRow("&a[]", DisplayName = "an anchor before a flow sequence")]
    [DataRow("!!map{}", DisplayName = "a tag before a flow mapping")]
    [DataRow("- &a[b]", DisplayName = "an anchor before a flow sequence entry value")]
    [DataRow("[ &a[b] ]", DisplayName = "an anchor before a nested flow sequence")]
    public void Parse_WhenPropertyIsNotSeparatedFromContent_ShouldThrowYamlFormatException(string yaml)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var document = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Verifies that in a flow collection a node property may be followed directly by the comma or bracket that ends
    /// the node, which leaves the node empty.
    /// </summary>
    /// <param name="yaml">A flow collection holding an anchored empty node.</param>
    /// <param name="expected">The collection expected, as JSON.</param>
    [TestMethod]
    [DataRow("[ &a, b ]", """[null,"b"]""", DisplayName = "followed by a comma")]
    [DataRow("[ b, &a]", """["b",null]""", DisplayName = "followed by a closing bracket")]
    public void Parse_WhenFlowPropertyEndsTheNode_ShouldReadEmptyNode(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that an anchor or tag before a flow collection applies to the collection, even when a <c>: </c>
    /// inside the collection makes the line look like an implicit key.
    /// </summary>
    /// <param name="yaml">A document with node properties before a flow collection that holds a <c>: </c>.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("- &a { x: 1 }", """[{"x":1}]""", DisplayName = "an anchor in a sequence entry")]
    [DataRow("- &A { x: 1 }\n- *A", """[{"x":1},{"x":1}]""", DisplayName = "an anchor an alias refers to")]
    [DataRow("&a { x: 1 }", """{"x":1}""", DisplayName = "an anchor on the root")]
    [DataRow("- !!map { x: 1 }", """[{"x":1}]""", DisplayName = "a tag in a sequence entry")]
    [DataRow("- &a [ { x: 1 } ]", """[[{"x":1}]]""", DisplayName = "an anchor before a flow sequence")]
    public void Parse_WhenPropertiesPrecedeFlowCollectionWithColon_ShouldApplyToCollection(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that an anchor name runs to the next white space, so a name that ends in <c>:</c> keeps the colon,
    /// whether it anchors a scalar or an implicit key (YAML test suite case 2SXE).
    /// </summary>
    /// <param name="yaml">A document with an anchor whose name ends in a colon.</param>
    /// <param name="expected">The document expected, as JSON.</param>
    [TestMethod]
    [DataRow("&: value", "\"value\"", DisplayName = "an anchor named by a colon alone, on a scalar")]
    [DataRow("&a: key: &a value\nfoo:\n  *a:\n", """{"key":"value","foo":"key"}""", DisplayName = "2SXE")]
    public void Parse_WhenAnchorNameEndsInColon_ShouldKeepTheColon(string yaml, string expected)
    {
        using var document = YamlDocument.Parse(yaml);

        Assert.AreEqual(expected, ToJson(document.RootElement));
    }

    /// <summary>
    /// Verifies that an anchor on an implicit mapping key defines the key's scalar, so an alias can refer to it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAliasRefersToAnchoredImplicitKey_ShouldResolveToKey()
    {
        using var document = YamlDocument.Parse("&k key: v\nother: *k\n");

        Assert.AreEqual("""{"key":"v","other":"key"}""", ToJson(document.RootElement));
    }
}
