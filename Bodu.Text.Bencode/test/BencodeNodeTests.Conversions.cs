// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Bencode;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeNode" /> conversion and navigation surface: the implicit wrapping and explicit
/// reading operators, the node-level indexers that delegate to the concrete container kinds, and the
/// <see cref="BencodeNode.Parent" /> / <see cref="BencodeNode.Root" /> relationship.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that the implicit operators wrap each supported scalar into a <see cref="BencodeValue" /> of the
    /// matching kind.
    /// </summary>
    [TestMethod]
    public void ImplicitOperators_WhenWrappingScalars_ShouldCreateValues()
    {
        BencodeNode fromString = "text";
        BencodeNode fromInt64 = 42L;
        BencodeNode fromInt32 = 7;
        BencodeNode fromBytes = new byte[] { 1, 2 };

        Assert.AreEqual(BencodeValueKind.ByteString, fromString.GetValueKind());
        Assert.AreEqual(BencodeValueKind.Integer, fromInt64.GetValueKind());
        Assert.AreEqual(7L, fromInt32.GetValue<long>());
        Assert.AreEqual(BencodeValueKind.ByteString, fromBytes.GetValueKind());
    }

    /// <summary>
    /// Verifies that each explicit reading operator throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>node</c> when the node is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ExplicitOperators_WhenNodeIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (long)(BencodeNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (int)(BencodeNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (string)(BencodeNode)null!;
        }, "node");

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = (byte[])(BencodeNode)null!;
        }, "node");
    }

    /// <summary>
    /// Verifies that the explicit reading operators throw <see cref="InvalidOperationException" /> when the node's
    /// kind does not match the requested type.
    /// </summary>
    [TestMethod]
    public void ExplicitOperators_WhenKindMismatched_ShouldThrowInvalidOperationException()
    {
        BencodeNode integer = BencodeValue.Create(1L);
        BencodeNode text = BencodeValue.Create("x");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = (string)integer;
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = (long)text;
        });
    }

    /// <summary>
    /// Verifies that the node-level positional indexer throws <see cref="InvalidOperationException" /> when the node
    /// is not an array.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIntIndexerUsedOnObject_ShouldThrowInvalidOperationException()
    {
        BencodeNode node = new BencodeObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node[0];
        });
    }

    /// <summary>
    /// Verifies that the node-level named indexer throws <see cref="InvalidOperationException" /> when the node is
    /// not an object.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenStringIndexerUsedOnArray_ShouldThrowInvalidOperationException()
    {
        BencodeNode node = new BencodeArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node["key"];
        });
    }

    /// <summary>
    /// Verifies that the node-level indexers delegate get and set access to the concrete container kinds.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenUsedOnMatchingKinds_ShouldDelegateToContainer()
    {
        BencodeNode objectNode = new BencodeObject();
        BencodeNode arrayNode = new BencodeArray(0);

        objectNode["k"] = 1L;
        arrayNode[0] = "x";

        Assert.AreEqual(1L, (long)objectNode["k"]!);
        Assert.AreEqual("x", (string)arrayNode[0]!);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.GetValue{T}" /> throws <see cref="InvalidOperationException" /> when the
    /// node is a container rather than a scalar value.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenNodeIsContainer_ShouldThrowInvalidOperationException()
    {
        BencodeNode node = new BencodeObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.GetValue<long>();
        });
    }

    /// <summary>
    /// Verifies that a node without a container reports a <see langword="null" /> <see cref="BencodeNode.Parent" />
    /// and returns itself from <see cref="BencodeNode.Root" />.
    /// </summary>
    [TestMethod]
    public void Root_WhenNodeHasNoParent_ShouldReturnSelf()
    {
        BencodeValue node = BencodeValue.Create(1L);

        Assert.IsNull(node.Parent);
        Assert.AreSame(node, node.Root);
    }
}
