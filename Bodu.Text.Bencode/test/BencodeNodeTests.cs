// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the mutable Bencode document object model (<see cref="BencodeNode" /> and its subtypes) and its bridge
/// through the <see cref="BencodeSerializer" />.
/// </summary>
[TestClass]
public partial class BencodeNodeTests
{
    /// <summary>
    /// Decodes the supplied Latin-1 text to bytes so binary content survives unchanged.
    /// </summary>
    /// <param name="text">The Latin-1 text to decode.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Bytes(string text) => Encoding.Latin1.GetBytes(text);

    /// <summary>
    /// Verifies that parsing a dictionary document yields a <see cref="BencodeObject" /> whose entries are readable
    /// through the indexer and report the object value kind.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Parse_WhenInputIsDictionary_ShouldReturnReadableObject()
    {
        byte[] data = Encoding.UTF8.GetBytes("d3:cow3:moo4:spam4:eggse");

        BencodeNode? node = BencodeNode.Parse(data);

        Assert.IsInstanceOfType<BencodeObject>(node);
        Assert.AreEqual(BencodeValueKind.Object, node!.GetValueKind());

        BencodeObject obj = node.AsObject();
        Assert.AreEqual("moo", obj["cow"]!.GetValue<string>());
        Assert.AreEqual("eggs", obj["spam"]!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that a tree built with implicit conversions serializes to the exact canonical bytes, with dictionary
    /// keys emitted in ascending order.
    /// </summary>
    [TestMethod]
    public void ToByteArray_WhenTreeBuiltWithImplicitConversions_ShouldEmitCanonicalBytes()
    {
        var o = new BencodeObject();
        o["name"] = "x";
        o["len"] = 5L;
        o["list"] = new BencodeArray(1, 2, 3);

        byte[] bytes = o.ToByteArray();

        Assert.AreEqual("d3:leni5e4:listli1ei2ei3ee4:name1:xe", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that parsing a canonical document and re-serializing the resulting tree reproduces the source bytes.
    /// </summary>
    [TestMethod]
    public void ToByteArray_WhenRoundTrippingCanonicalInput_ShouldBeByteEqual()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("d8:announce17:http://tracker.tx4:infod6:lengthi1024e4:name4:testee");

        byte[] roundTripped = BencodeNode.Parse(bytes)!.ToByteArray();

        CollectionAssert.AreEqual(bytes, roundTripped);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.AsObject" /> throws when the node is not a dictionary.
    /// </summary>
    [TestMethod]
    public void AsObject_WhenNodeIsNotObject_ShouldThrowInvalidOperationException()
    {
        BencodeNode node = BencodeValue.Create(1L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.AsObject();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.AsArray" /> throws when the node is not a list.
    /// </summary>
    [TestMethod]
    public void AsArray_WhenNodeIsNotArray_ShouldThrowInvalidOperationException()
    {
        BencodeNode node = BencodeValue.Create("text");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.AsArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.AsValue" /> throws when the node is a container.
    /// </summary>
    [TestMethod]
    public void AsValue_WhenNodeIsContainer_ShouldThrowInvalidOperationException()
    {
        BencodeNode node = new BencodeArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.AsValue();
        });
    }

    /// <summary>
    /// Verifies that reading a byte-string value as a 64-bit integer throws.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenByteStringReadAsInt64_ShouldThrowInvalidOperationException()
    {
        BencodeValue value = BencodeValue.Create("hello");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<long>();
        });
    }

    /// <summary>
    /// Verifies that reading an integer value as a string throws.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenIntegerReadAsString_ShouldThrowInvalidOperationException()
    {
        BencodeValue value = BencodeValue.Create(7L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<string>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeValue.TryGetValue{T}" /> returns <see langword="false" /> for an incompatible
    /// kind/type combination.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenByteStringReadAsInt64_ShouldReturnFalse()
    {
        BencodeValue value = BencodeValue.Create("hello");

        bool result = value.TryGetValue(out long converted);

        Assert.IsFalse(result);
        Assert.AreEqual(0L, converted);
    }

    /// <summary>
    /// Verifies that an integer value reads back as a narrower integer type when it is in range.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenIntegerInRangeOfNarrowerType_ShouldConvert()
    {
        BencodeValue value = BencodeValue.Create(200L);

        Assert.AreEqual((byte)200, value.GetValue<byte>());
        Assert.AreEqual(200, value.GetValue<int>());
    }

    /// <summary>
    /// Verifies that reading an integer value as a narrower type whose range it exceeds throws.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenIntegerOutOfRangeOfNarrowerType_ShouldThrowInvalidOperationException()
    {
        BencodeValue value = BencodeValue.Create(40000L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<short>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.DeepClone" /> produces an independent tree whose mutation does not affect
    /// the original.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenCloneMutated_ShouldNotAffectOriginal()
    {
        var original = new BencodeObject();
        original["list"] = new BencodeArray(1, 2, 3);

        BencodeObject clone = original.DeepClone().AsObject();
        clone["list"]!.AsArray().Add(4);

        Assert.AreEqual(3, original["list"]!.AsArray().Count);
        Assert.AreEqual(4, clone["list"]!.AsArray().Count);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.DeepEquals" /> reports equality for a deep clone and inequality after a
    /// change.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenComparingCloneAndModifiedClone_ShouldReflectStructuralEquality()
    {
        var original = new BencodeObject();
        original["a"] = 1L;
        original["b"] = new BencodeArray("x", "y");

        BencodeNode clone = original.DeepClone();
        Assert.IsTrue(BencodeNode.DeepEquals(original, clone));

        clone.AsObject()["a"] = 2L;
        Assert.IsFalse(BencodeNode.DeepEquals(original, clone));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.DeepEquals" /> ignores in-memory key order when comparing objects.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenObjectsDifferOnlyByKeyOrder_ShouldReturnTrue()
    {
        var first = new BencodeObject();
        first["a"] = 1L;
        first["b"] = 2L;

        var second = new BencodeObject();
        second["b"] = 2L;
        second["a"] = 1L;

        Assert.IsTrue(BencodeNode.DeepEquals(first, second));
    }

    /// <summary>
    /// Verifies that adding a node that already belongs to another container throws.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeAlreadyHasParent_ShouldThrowInvalidOperationException()
    {
        BencodeValue shared = BencodeValue.Create(1L);
        var first = new BencodeArray();
        first.Add(shared);

        var second = new BencodeArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            second.Add(shared);
        });
    }

    /// <summary>
    /// Verifies that serializing an array containing a <see langword="null" /> entry throws because Bencode has no null
    /// token.
    /// </summary>
    [TestMethod]
    public void ToByteArray_WhenArrayContainsNullEntry_ShouldThrowBencodeSerializationException()
    {
        var array = new BencodeArray();
        array.Add(null);

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = array.ToByteArray();
        });
    }

    /// <summary>
    /// Verifies that serializing an object containing a <see langword="null" /> value throws because Bencode has no
    /// null token.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenObjectContainsNullValue_ShouldThrowBencodeSerializationException()
    {
        var obj = new BencodeObject();
        obj["empty"] = null;

        _ = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = obj.ToByteArray();
        });
    }

    /// <summary>
    /// Verifies that the <see cref="Root" /> property walks parents to the topmost node.
    /// </summary>
    [TestMethod]
    public void Root_WhenNodeIsNested_ShouldReturnTopmostNode()
    {
        var root = new BencodeObject();
        var middle = new BencodeArray();
        BencodeValue leaf = BencodeValue.Create(1L);
        middle.Add(leaf);
        root["items"] = middle;

        Assert.AreSame(root, leaf.Root);
    }

    /// <summary>
    /// Verifies that the explicit conversion operators read scalar node values.
    /// </summary>
    [TestMethod]
    public void ExplicitOperators_WhenReadingScalarNodes_ShouldReturnUnderlyingValues()
    {
        BencodeNode integer = BencodeValue.Create(42L);
        BencodeNode text = BencodeValue.Create("hi");

        Assert.AreEqual(42L, (long)integer);
        Assert.AreEqual(42, (int)integer);
        Assert.AreEqual("hi", (string)text);
    }

    /// <summary>
    /// Verifies that serializing a node tree through <see cref="BencodeSerializer" /> matches the tree's own canonical
    /// bytes.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsNodeTree_ShouldMatchToByteArray()
    {
        var o = new BencodeObject();
        o["announce"] = "http://t";
        o["length"] = 1024L;

        byte[] serialized = BencodeSerializer.Serialize((BencodeNode)o);

        CollectionAssert.AreEqual(o.ToByteArray(), serialized);
    }

    /// <summary>
    /// Verifies that deserializing through <see cref="BencodeSerializer" /> into a <see cref="BencodeObject" /> yields a
    /// structurally equal tree.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsBencodeObject_ShouldReturnEqualTree()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("d8:announce8:http://t6:lengthi1024ee");

        BencodeObject deserialized = BencodeSerializer.Deserialize<BencodeObject>(bytes);

        var expected = new BencodeObject();
        expected["announce"] = "http://t";
        expected["length"] = 1024L;

        Assert.IsTrue(BencodeNode.DeepEquals(expected, deserialized));
    }
}
