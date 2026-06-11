// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.DomBridges.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the serializer ⇄ DOM bridges — <see cref="BencodeSerializer.SerializeToNode{T}" />,
/// <see cref="BencodeSerializer.SerializeToDocument{T}" />, and
/// <see cref="BencodeSerializer.Deserialize{T}(BencodeNode, BencodeSerializerOptions?)" /> — that mirror the
/// equivalent <see cref="System.Text.Json.JsonSerializer" /> members.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.SerializeToNode{T}" /> produces a node tree exposing the value's
    /// members under their wire names.
    /// </summary>
    [TestMethod]
    public void SerializeToNode_WhenValueSerialized_ShouldExposeMembersAsNodeTree()
    {
        var model = new BridgeModel { Id = 7, Label = "x" };

        BencodeNode? node = BencodeSerializer.SerializeToNode(model);

        Assert.IsNotNull(node);
        Assert.AreEqual(BencodeValueKind.Object, node.GetValueKind());
        Assert.AreEqual(7L, node["Id"]!.GetValue<long>());
        Assert.AreEqual("x", node["Label"]!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.SerializeToDocument{T}" /> produces a read-only document over the
    /// value's canonical encoding.
    /// </summary>
    [TestMethod]
    public void SerializeToDocument_WhenValueSerialized_ShouldExposeMembersAsDocument()
    {
        var model = new BridgeModel { Id = 7, Label = "x" };

        using BencodeDocument document = BencodeSerializer.SerializeToDocument(model);

        Assert.AreEqual(7L, document.RootElement.GetProperty("Id").GetInt64());
        Assert.AreEqual("x", document.RootElement.GetProperty("Label").GetString());
        CollectionAssert.AreEqual(BencodeSerializer.Serialize(model), document.RootElement.GetRawBytes());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(BencodeNode, BencodeSerializerOptions?)" /> binds a
    /// node tree to a value, completing the node round trip.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNode_ShouldBindValue()
    {
        var node = new BencodeObject
        {
            ["Id"] = 7,
            ["Label"] = "x",
        };

        BridgeModel model = BencodeSerializer.Deserialize<BridgeModel>(node);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that a value round-trips unchanged through <see cref="BencodeSerializer.SerializeToNode{T}" /> and
    /// <see cref="BencodeSerializer.Deserialize{T}(BencodeNode, BencodeSerializerOptions?)" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNodeRoundTrip_ShouldReturnEqualValue()
    {
        var original = new BridgeModel { Id = 42, Label = "héllo" };

        BridgeModel roundTripped = BencodeSerializer.Deserialize<BridgeModel>(BencodeSerializer.SerializeToNode(original)!);

        Assert.AreEqual(original.Id, roundTripped.Id);
        Assert.AreEqual(original.Label, roundTripped.Label);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(BencodeNode, BencodeSerializerOptions?)" /> with a
    /// null node throws <see cref="ArgumentNullException" />, naming the <c>node</c> parameter.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNodeIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeSerializer.Deserialize<BridgeModel>((BencodeNode)null!);
        }, "node");
    }

    /// <summary>
    /// A small model used by the serializer ⇄ DOM bridge tests.
    /// </summary>
    private sealed class BridgeModel
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <returns>The identifier.</returns>
        public int Id { get; set; }

        /// <summary>Gets or sets the label.</summary>
        /// <returns>The label.</returns>
        public string Label { get; set; } = string.Empty;
    }
}
