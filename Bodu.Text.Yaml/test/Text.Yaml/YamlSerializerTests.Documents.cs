// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Documents.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the read-only DOM↔serializer bridges: deserializing into <see cref="YamlElement" /> and
/// <see cref="YamlDocument" />, element-typed members inside object graphs, and re-serializing documents and elements.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that deserializing a mapping into <see cref="YamlElement" /> produces an element view carrying the
    /// mapping's typed scalar values.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsYamlElement_ShouldProduceElementView()
    {
        YamlElement element = YamlSerializer.Deserialize<YamlElement>("name: svc\nport: 8080\n");

        Assert.AreEqual(YamlValueKind.Mapping, element.ValueKind);
        Assert.AreEqual("svc", element.GetProperty("name").GetString());
        Assert.AreEqual(8080L, element.GetProperty("port").GetInt64());
    }

    /// <summary>
    /// Verifies that deserializing into <see cref="YamlDocument" /> produces a document whose root element views the
    /// value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsYamlDocument_ShouldProduceDocument()
    {
        YamlDocument document = YamlSerializer.Deserialize<YamlDocument>("- 1\n- 2\n");

        Assert.AreEqual(YamlValueKind.Sequence, document.RootElement.ValueKind);
        Assert.AreEqual(2, document.RootElement.GetSequenceLength());
        Assert.AreEqual(2L, document.RootElement[1].GetInt64());
    }

    /// <summary>
    /// Verifies that a document read by the serializer re-serializes to the same canonical text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDocument_ShouldRoundTripCanonicalText()
    {
        const string canonical = "name: svc\ntags:\n  - a\n  - b\n";

        YamlDocument document = YamlSerializer.Deserialize<YamlDocument>(canonical);

        Assert.AreEqual(canonical, YamlSerializer.Serialize(document));
    }

    /// <summary>
    /// Verifies that a <see cref="YamlElement" />-typed member participates in an object graph on both write and read,
    /// viewing only its own subtree.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenElementTypedMember_ShouldRoundTripSubtree()
    {
        ElementHost host = YamlSerializer.Deserialize<ElementHost>("Name: cfg\nPayload:\n  k: 1\n");

        Assert.AreEqual("cfg", host.Name);
        Assert.AreEqual(YamlValueKind.Mapping, host.Payload.ValueKind);
        Assert.AreEqual(1L, host.Payload.GetProperty("k").GetInt64());

        string text = YamlSerializer.Serialize(host);
        Assert.AreEqual("Name: cfg\nPayload:\n  k: 1\n", text);
    }

    /// <summary>
    /// Verifies that after an element-typed member consumes its subtree, subsequent members of the enclosing mapping
    /// still bind — pinning the bridge's reader-positioning contract.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenElementMemberFollowedByScalar_ShouldBindBothMembers()
    {
        ElementHost host = YamlSerializer.Deserialize<ElementHost>("Payload:\n  k: 1\nName: after\n");

        Assert.AreEqual(1L, host.Payload.GetProperty("k").GetInt64());
        Assert.AreEqual("after", host.Name);
    }

    /// <summary>
    /// Verifies that writing a default <see cref="YamlElement" />, which belongs to no document, throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDefaultElement_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = YamlSerializer.Serialize(default(YamlElement));
        });
    }

    /// <summary>
    /// Verifies that writing a disposed <see cref="YamlDocument" /> surfaces the document's own
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDisposedDocument_ShouldThrowObjectDisposedException()
    {
        YamlDocument document = YamlSerializer.Deserialize<YamlDocument>("a: 1\n");
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = YamlSerializer.Serialize(document);
        });
    }

    /// <summary>
    /// Verifies that an aliased subtree deserialized into an element materializes the anchored value, because aliases
    /// are resolved at parse time.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAliasedValueIntoElement_ShouldMaterializeResolvedValue()
    {
        YamlElement element = YamlSerializer.Deserialize<YamlElement>("a: &x 17\nb: *x\n");

        Assert.AreEqual(17L, element.GetProperty("b").GetInt64());
    }

    /// <summary>
    /// A model carrying a <see cref="YamlElement" />-typed member alongside an ordinary scalar member.
    /// </summary>
    private sealed class ElementHost
    {
        /// <summary>
        /// Gets or sets the host name.
        /// </summary>
        /// <value>The name.</value>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the free-form payload subtree.
        /// </summary>
        /// <value>The payload element.</value>
        public YamlElement Payload { get; set; }
    }
}
