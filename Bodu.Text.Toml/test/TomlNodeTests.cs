// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the mutable TOML document object model (<see cref="TomlNode" /> and its subtypes) and its bridge through the
/// <see cref="TomlSerializer" />.
/// </summary>
[TestClass]
public class TomlNodeTests
{
    /// <summary>
    /// Verifies that parsing a table document yields a <see cref="TomlObject" /> whose entries are readable through the
    /// indexer and report the table value kind.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Parse_WhenInputIsTable_ShouldReturnReadableObject()
    {
        byte[] data = Encoding.UTF8.GetBytes("name = \"x\"\nport = 8080\n");

        var node = TomlNode.Parse(data);

        Assert.IsInstanceOfType<TomlObject>(node);
        Assert.AreEqual(TomlValueKind.Table, node!.GetValueKind());

        TomlObject obj = node.AsObject();
        Assert.AreEqual("x", obj["name"]!.GetValue<string>());
        Assert.AreEqual(8080, obj["port"]!.GetValue<int>());
    }

    /// <summary>
    /// Verifies that the indexer and <see cref="TomlNode.GetValue{T}" /> read each TOML scalar kind back as its CLR
    /// type, including an offset date-time.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenReadingScalarKinds_ShouldReturnTypedValues()
    {
        byte[] data = Encoding.UTF8.GetBytes(
            "s = \"hi\"\ni = 42\nf = 1.5\nb = true\nwhen = 1979-05-27T07:32:00Z\n");

        TomlObject obj = TomlNode.Parse(data)!.AsObject();

        Assert.AreEqual("hi", obj["s"]!.GetValue<string>());
        Assert.AreEqual(42L, obj["i"]!.GetValue<long>());
        Assert.AreEqual(1.5d, obj["f"]!.GetValue<double>());
        Assert.IsTrue(obj["b"]!.GetValue<bool>());
        Assert.AreEqual(
            new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero),
            obj["when"]!.GetValue<DateTimeOffset>());
    }

    /// <summary>
    /// Verifies that a tree built with implicit conversions renders the expected canonical TOML through both
    /// <see cref="TomlNode.ToString" /> and <see cref="TomlNode.ToUtf8Bytes" />.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenTreeBuiltWithImplicitConversions_ShouldEmitCanonicalToml()
    {
        var o = new TomlObject();
        o["name"] = "x";
        o["port"] = 8080;
        o["ratio"] = 1.5;
        o["enabled"] = true;
        o["nums"] = new TomlArray(1, 2, 3);

        const string expected = "name = \"x\"\nport = 8080\nratio = 1.5\nenabled = true\nnums = [1, 2, 3]\n";

        Assert.AreEqual(expected, o.ToString());
        Assert.AreEqual(expected, Encoding.UTF8.GetString(o.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that parsing a document and re-serializing the resulting tree reproduces the source bytes.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenRoundTrippingCanonicalInput_ShouldBeByteEqual()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("name = \"x\"\nport = 8080\nnums = [1, 2, 3]\n\n[sub]\nk = \"v\"\n");

        byte[] roundTripped = TomlNode.Parse(bytes)!.ToUtf8Bytes();

        CollectionAssert.AreEqual(bytes, roundTripped);
    }

    /// <summary>
    /// Verifies that a nested <see cref="TomlObject" /> is emitted as a <c>[header]</c> table block.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenNestedTable_ShouldEmitHeaderBlock()
    {
        var root = new TomlObject();
        var sub = new TomlObject();
        sub["k"] = "v";
        root["sub"] = sub;

        Assert.AreEqual("[sub]\nk = \"v\"\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.Parse(ReadOnlySpan{byte})" /> always yields a table root, since a TOML document
    /// has no top-level scalar or array form.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDocumentHasMembers_ShouldYieldTableRoot()
    {
        var node = TomlNode.Parse(Encoding.UTF8.GetBytes("a = 1\n"));

        Assert.IsInstanceOfType<TomlObject>(node);
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.ToUtf8Bytes" /> throws <see cref="TomlSerializationException" /> when the root
    /// node is a scalar, because a TOML document's root must be a table.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenRootIsScalar_ShouldThrowTomlSerializationException()
    {
        TomlNode node = TomlValue.Create(42L);

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = node.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.ToUtf8Bytes" /> throws <see cref="TomlSerializationException" /> when the root
    /// node is an array, because a TOML document's root must be a table.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenRootIsArray_ShouldThrowTomlSerializationException()
    {
        TomlNode node = new TomlArray(1, 2, 3);

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = node.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.AsObject" /> throws when the node is not a table.
    /// </summary>
    [TestMethod]
    public void AsObject_WhenNodeIsNotObject_ShouldThrowInvalidOperationException()
    {
        TomlNode node = TomlValue.Create(1L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.AsObject();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.AsArray" /> throws when the node is not an array.
    /// </summary>
    [TestMethod]
    public void AsArray_WhenNodeIsNotArray_ShouldThrowInvalidOperationException()
    {
        TomlNode node = TomlValue.Create("text");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.AsArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.AsValue" /> throws when the node is a container.
    /// </summary>
    [TestMethod]
    public void AsValue_WhenNodeIsContainer_ShouldThrowInvalidOperationException()
    {
        TomlNode node = new TomlArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = node.AsValue();
        });
    }

    /// <summary>
    /// Verifies that reading a string value as a 64-bit integer throws.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenStringReadAsInt64_ShouldThrowInvalidOperationException()
    {
        var value = TomlValue.Create("hello");

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
        var value = TomlValue.Create(7L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<string>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlValue.TryGetValue{T}" /> returns <see langword="false" /> for an incompatible
    /// kind/type combination.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenStringReadAsInt64_ShouldReturnFalse()
    {
        var value = TomlValue.Create("hello");

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
        var value = TomlValue.Create(200L);

        Assert.AreEqual((byte)200, value.GetValue<byte>());
        Assert.AreEqual(200, value.GetValue<int>());
    }

    /// <summary>
    /// Verifies that reading an integer value as a narrower type whose range it exceeds throws.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenIntegerOutOfRangeOfNarrowerType_ShouldThrowInvalidOperationException()
    {
        var value = TomlValue.Create(40000L);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<short>();
        });
    }

    /// <summary>
    /// Verifies that a float value reads back as a single-precision <see cref="float" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenFloatReadAsSingle_ShouldConvert()
    {
        var value = TomlValue.Create(1.5d);

        Assert.AreEqual(1.5f, value.GetValue<float>());
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.DeepClone" /> produces an independent tree whose mutation does not affect the
    /// original.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenCloneMutated_ShouldNotAffectOriginal()
    {
        var original = new TomlObject();
        original["list"] = new TomlArray(1, 2, 3);

        TomlObject clone = original.DeepClone().AsObject();
        clone["list"]!.AsArray().Add(4);

        Assert.AreEqual(3, original["list"]!.AsArray().Count);
        Assert.AreEqual(4, clone["list"]!.AsArray().Count);
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.DeepEquals" /> reports equality for a deep clone and inequality after a change.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenComparingCloneAndModifiedClone_ShouldReflectStructuralEquality()
    {
        var original = new TomlObject();
        original["a"] = 1L;
        original["b"] = new TomlArray("x", "y");

        TomlNode clone = original.DeepClone();
        Assert.IsTrue(TomlNode.DeepEquals(original, clone));

        clone.AsObject()["a"] = 2L;
        Assert.IsFalse(TomlNode.DeepEquals(original, clone));
    }

    /// <summary>
    /// Verifies that <see cref="TomlNode.DeepEquals" /> ignores in-memory key order when comparing tables.
    /// </summary>
    [TestMethod]
    public void DeepEquals_WhenObjectsDifferOnlyByKeyOrder_ShouldReturnTrue()
    {
        var first = new TomlObject();
        first["a"] = 1L;
        first["b"] = 2L;

        var second = new TomlObject();
        second["b"] = 2L;
        second["a"] = 1L;

        Assert.IsTrue(TomlNode.DeepEquals(first, second));
    }

    /// <summary>
    /// Verifies that adding a node that already belongs to another container throws.
    /// </summary>
    [TestMethod]
    public void Add_WhenNodeAlreadyHasParent_ShouldThrowInvalidOperationException()
    {
        var shared = TomlValue.Create(1L);
        var first = new TomlArray();
        first.Add(shared);

        var second = new TomlArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            second.Add(shared);
        });
    }

    /// <summary>
    /// Verifies that serializing an array containing a <see langword="null" /> element throws because TOML has no null
    /// token.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenArrayContainsNullElement_ShouldThrowTomlSerializationException()
    {
        var root = new TomlObject();
        var array = new TomlArray();
        array.Add(null);
        root["items"] = array;

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that serializing a table containing a <see langword="null" /> value throws because TOML has no null
    /// token.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenObjectContainsNullValue_ShouldThrowTomlSerializationException()
    {
        var obj = new TomlObject();
        obj["empty"] = null;

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = obj.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that the <see cref="TomlNode.Root" /> property walks parents to the topmost node.
    /// </summary>
    [TestMethod]
    public void Root_WhenNodeIsNested_ShouldReturnTopmostNode()
    {
        var root = new TomlObject();
        var middle = new TomlArray();
        var leaf = TomlValue.Create(1L);
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
        TomlNode integer = TomlValue.Create(42L);
        TomlNode text = TomlValue.Create("hi");
        TomlNode flag = TomlValue.Create(true);

        Assert.AreEqual(42L, (long)integer);
        Assert.AreEqual(42, (int)integer);
        Assert.AreEqual("hi", (string)text);
        Assert.IsTrue((bool)flag);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlObject" /> built with case-insensitive <see cref="TomlNodeOptions" /> resolves
    /// property lookups regardless of case.
    /// </summary>
    [TestMethod]
    public void TomlNodeOptions_WhenCaseInsensitive_ShouldMatchPropertyNamesIgnoringCase()
    {
        var options = new TomlNodeOptions { PropertyNameCaseInsensitive = true };
        TomlObject obj = TomlNode.Parse(Encoding.UTF8.GetBytes("Name = \"x\"\n"), options)!.AsObject();

        Assert.IsTrue(obj.TryGetPropertyValue("name", out TomlNode? value));
        Assert.AreEqual("x", value!.GetValue<string>());
    }

    /// <summary>
    /// Verifies that serializing a node tree through <see cref="TomlSerializer" /> matches the tree's own canonical
    /// bytes.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueIsNodeTree_ShouldMatchToUtf8Bytes()
    {
        var o = new TomlObject();
        o["name"] = "x";
        o["port"] = 8080;

        string serialized = TomlSerializer.Serialize((TomlNode)o);

        Assert.AreEqual(Encoding.UTF8.GetString(o.ToUtf8Bytes()), serialized);
    }

    /// <summary>
    /// Verifies that serializing a node tree whose root is not a table through <see cref="TomlSerializer" /> throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNodeRootIsScalar_ShouldThrowTomlSerializationException()
    {
        TomlNode node = TomlValue.Create(42L);

        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(node);
        });
    }

    /// <summary>
    /// Verifies that deserializing through <see cref="TomlSerializer" /> into a <see cref="TomlObject" /> yields a
    /// structurally equal tree.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsTomlObject_ShouldReturnEqualTree()
    {
        TomlObject deserialized = TomlSerializer.Deserialize<TomlObject>("name = \"x\"\nport = 8080\n");

        var expected = new TomlObject();
        expected["name"] = "x";
        expected["port"] = 8080L;

        Assert.IsTrue(TomlNode.DeepEquals(expected, deserialized));
    }

    /// <summary>
    /// Verifies that deserializing a table with keys that match no normal member captures them into a
    /// <see cref="TomlObject" /> member annotated with <see cref="TomlExtensionDataAttribute" />, and that the captured
    /// entries write back out alongside the type's normal members.
    /// </summary>
    [TestMethod]
    public void ExtensionData_WhenUnmatchedKeys_ShouldCaptureIntoTomlObjectAndRoundTrip()
    {
        var model = TomlSerializer.Deserialize<ExtensionDataModel>("Name = \"test\"\na = 1\nz = 9\n");

        Assert.AreEqual("test", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(2, model.Extra!.Count);
        Assert.AreEqual(1L, model.Extra["a"]!.GetValue<long>());
        Assert.AreEqual(9L, model.Extra["z"]!.GetValue<long>());

        var rebuilt = new ExtensionDataModel
        {
            Name = "test",
            Extra = new TomlObject
            {
                ["a"] = TomlValue.Create(1),
                ["z"] = TomlValue.Create(9),
            },
        };

        Assert.AreEqual("Name = \"test\"\na = 1\nz = 9\n", TomlSerializer.Serialize(rebuilt));
    }

    /// <summary>
    /// A model with a normal member and a <see cref="TomlExtensionDataAttribute" /> member typed as the
    /// <see cref="TomlObject" /> document object model node, which captures unmatched table entries.
    /// </summary>
    private sealed class ExtensionDataModel
    {
        /// <summary>Gets or sets the name.</summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the captured entries that match no other member.</summary>
        /// <returns>The captured entries.</returns>
        [TomlExtensionData]
        public TomlObject? Extra { get; set; }
    }
}
