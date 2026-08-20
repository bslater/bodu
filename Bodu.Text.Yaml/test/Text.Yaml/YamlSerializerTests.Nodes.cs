// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Nodes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the DOM↔serializer node bridge: serializing a mutable <see cref="YamlNode" /> tree, deserializing into the
/// node types, node-typed members inside object graphs, and the converter-resolution precedence that keeps
/// <see cref="YamlObject" /> from being claimed structurally.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a hand-built node tree serializes to its canonical block YAML.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNodeTree_ShouldEmitBlockYaml()
    {
        var root = new YamlObject
        {
            ["name"] = YamlValue.Create("svc"),
            ["port"] = YamlValue.Create(8080L),
            ["tags"] = new YamlArray { YamlValue.Create("a"), YamlValue.Create("b") },
        };

        string text = YamlSerializer.Serialize<YamlNode>(root);

        Assert.AreEqual("name: svc\nport: 8080\ntags:\n  - a\n  - b\n", text);
    }

    /// <summary>
    /// Verifies that deserializing a mapping into <see cref="YamlNode" /> produces a <see cref="YamlObject" /> carrying
    /// the mapping's typed scalar values.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsYamlNode_ShouldProduceYamlObject()
    {
        YamlNode node = YamlSerializer.Deserialize<YamlNode>("name: svc\nport: 8080\nactive: true\n");

        var obj = (YamlObject)node;
        Assert.AreEqual(3, obj.Count);
        Assert.AreEqual("svc", obj["name"]!.AsValue().GetValue<string>());
        Assert.AreEqual(8080L, obj["port"]!.AsValue().GetValue<long>());
        Assert.IsTrue(obj["active"]!.AsValue().GetValue<bool>());
    }

    /// <summary>
    /// Verifies that deserializing directly into <see cref="YamlObject" /> succeeds, pinning the converter-resolution
    /// order: the node bridge must claim the object node before the dictionary and collection factories that would
    /// otherwise match its enumerable surface.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsYamlObject_ShouldUseNodeBridgeNotStructuralConverters()
    {
        YamlObject obj = YamlSerializer.Deserialize<YamlObject>("a: 1\nb: 2\n");

        Assert.AreEqual(2, obj.Count);
        Assert.AreEqual(1L, obj["a"]!.AsValue().GetValue<long>());
        Assert.AreEqual(2L, obj["b"]!.AsValue().GetValue<long>());
    }

    /// <summary>
    /// Verifies that deserializing a sequence into <see cref="YamlArray" /> produces the array node with each item's
    /// resolved kind.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsYamlArray_ShouldProduceArrayNode()
    {
        YamlArray array = YamlSerializer.Deserialize<YamlArray>("- 1\n- two\n- true\n");

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(1L, array[0]!.AsValue().GetValue<long>());
        Assert.AreEqual("two", array[1]!.AsValue().GetValue<string>());
        Assert.IsTrue(array[2]!.AsValue().GetValue<bool>());
    }

    /// <summary>
    /// Verifies that deserializing a scalar into <see cref="YamlValue" /> preserves the scalar's implicitly resolved
    /// kind.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsYamlValue_ShouldPreserveResolvedKind()
    {
        YamlValue value = YamlSerializer.Deserialize<YamlValue>("true\n");

        Assert.AreEqual(YamlValueKind.Boolean, value.ValueKind);
        Assert.IsTrue(value.GetValue<bool>());
    }

    /// <summary>
    /// Verifies that a node tree read from YAML re-serializes to the same canonical text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNodeTree_ShouldRoundTripCanonicalText()
    {
        const string canonical = "name: svc\nport: 8080\ntags:\n  - a\n  - b\n";

        YamlNode node = YamlSerializer.Deserialize<YamlNode>(canonical);

        Assert.AreEqual(canonical, YamlSerializer.Serialize(node));
    }

    /// <summary>
    /// Verifies that the node bridge observes the reader's alias resolution, so an aliased scalar materializes as its
    /// anchored value in the node tree.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAliasedScalar_ShouldMaterializeResolvedValue()
    {
        YamlObject obj = YamlSerializer.Deserialize<YamlObject>("a: &x 17\nb: *x\n");

        Assert.AreEqual(17L, obj["b"]!.AsValue().GetValue<long>());
    }

    /// <summary>
    /// Verifies that a <see cref="YamlNode" />-typed member participates in an object graph on both write and read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNodeTypedMember_ShouldRoundTrip()
    {
        var model = new NodeHost
        {
            Name = "cfg",
            Payload = new YamlObject { ["k"] = YamlValue.Create(1L) },
        };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Name: cfg\nPayload:\n  k: 1\n", text);

        NodeHost roundTripped = YamlSerializer.Deserialize<NodeHost>(text);
        var payload = (YamlObject)roundTripped.Payload!;
        Assert.AreEqual(1L, payload["k"]!.AsValue().GetValue<long>());
    }

    /// <summary>
    /// Verifies that a null scalar deserialized into <see cref="YamlNode" /> yields a null reference, matching the
    /// mutable DOM's representation of the null scalar.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNullScalarIntoNode_ShouldReturnNullReference()
    {
        YamlNode? node = YamlSerializer.Deserialize<YamlNode>("null\n");

        Assert.IsNull(node);
    }

    /// <summary>
    /// Verifies that serializing a null root through the node-typed overload writes the YAML null scalar via the
    /// serializer's null-root shortcut.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNodeRootIsNull_ShouldWriteNullScalar()
    {
        string text = YamlSerializer.Serialize<YamlNode>(null!);

        Assert.AreEqual("null\n", text);
    }

    /// <summary>
    /// A model carrying a <see cref="YamlNode" />-typed member alongside an ordinary scalar member.
    /// </summary>
    private sealed class NodeHost
    {
        /// <summary>
        /// Gets or sets the host name.
        /// </summary>
        /// <value>The name.</value>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the free-form payload subtree.
        /// </summary>
        /// <value>The payload node, or <see langword="null" /> when absent.</value>
        public YamlNode? Payload { get; set; }
    }
}
