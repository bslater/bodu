// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.DeepEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the structural-equality contract of <see cref="BencodeNode.DeepEquals(BencodeNode?, BencodeNode?)" />:
/// <see langword="null" /> handling, kind discrimination, byte-wise byte-string comparison, ordered array comparison,
/// and order-independent object comparison.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that two <see langword="null" /> nodes compare equal.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenBothNull_ShouldReturnTrue()
    {
        Assert.IsTrue(BencodeNode.DeepEquals(null, null));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> node never equals a non-null node.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenOneNull_ShouldReturnFalse()
    {
        var node = BencodeValue.Create(1L);

        Assert.IsFalse(BencodeNode.DeepEquals(node, null));
        Assert.IsFalse(BencodeNode.DeepEquals(null, node));
    }

    /// <summary>
    /// Verifies that a node compares equal to itself by reference without walking its content.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenSameInstance_ShouldReturnTrue()
    {
        var array = new BencodeArray(1, 2, 3);

        Assert.IsTrue(BencodeNode.DeepEquals(array, array));
    }

    /// <summary>
    /// Verifies that nodes of different kinds never compare equal, even when their textual renderings match.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenKindsDiffer_ShouldReturnFalse()
    {
        Assert.IsFalse(BencodeNode.DeepEquals(BencodeValue.Create(42L), BencodeValue.Create("42")));
        Assert.IsFalse(BencodeNode.DeepEquals(new BencodeArray(), new BencodeObject()));
    }

    /// <summary>
    /// Verifies that byte strings compare by byte sequence, including binary content that is not valid UTF-8.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenByteStringsCompared_ShouldCompareByByteSequence()
    {
        Assert.IsTrue(BencodeNode.DeepEquals(
            BencodeValue.Create(new byte[] { 0x00, 0xFF }),
            BencodeValue.Create(new byte[] { 0x00, 0xFF })));
        Assert.IsFalse(BencodeNode.DeepEquals(
            BencodeValue.Create(new byte[] { 0x00, 0xFF }),
            BencodeValue.Create(new byte[] { 0x00, 0xFE })));
    }

    /// <summary>
    /// Verifies that arrays differing in length or in element order never compare equal.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenArraysDifferInLengthOrOrder_ShouldReturnFalse()
    {
        Assert.IsFalse(BencodeNode.DeepEquals(new BencodeArray(1, 2), new BencodeArray(1, 2, 3)));
        Assert.IsFalse(BencodeNode.DeepEquals(new BencodeArray(1, 2), new BencodeArray(2, 1)));
    }

    /// <summary>
    /// Verifies that arrays whose corresponding entries are both <see langword="null" /> compare equal, while a
    /// <see langword="null" /> entry never equals a value.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenArraysContainNullEntries_ShouldCompareEntrywise()
    {
        var firstWithNull = new BencodeArray();
        firstWithNull.Add(null);
        var secondWithNull = new BencodeArray();
        secondWithNull.Add(null);

        Assert.IsTrue(BencodeNode.DeepEquals(firstWithNull, secondWithNull));
        Assert.IsFalse(BencodeNode.DeepEquals(firstWithNull, new BencodeArray(1)));
    }

    /// <summary>
    /// Verifies that nested trees with equal structure and values compare equal.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenNestedTreesEqual_ShouldReturnTrue()
    {
        static BencodeObject Build()
        {
            var inner = new BencodeObject();
            inner["x"] = new BencodeArray(1, "two");

            var root = new BencodeObject();
            root["a"] = inner;
            root["b"] = 3L;
            return root;
        }

        Assert.IsTrue(BencodeNode.DeepEquals(Build(), Build()));
    }
}
