// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlObjectTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlObject" /> dictionary surface: keyed access, membership, removal with parent
/// detachment, the single-parent rule on assignment, enumeration semantics, and insertion-ordered serialization.
/// </summary>
[TestClass]
public class TomlObjectTests
{
    /// <summary>
    /// Verifies that the enumerable constructor copies the supplied entries into the table.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsProvided_ShouldContainEntries()
    {
        var items = new List<KeyValuePair<string, TomlNode?>>
        {
            new("a", TomlValue.Create(1L)),
            new("b", TomlValue.Create("x")),
        };

        var obj = new TomlObject(items);

        Assert.AreEqual(2, obj.Count);
        Assert.AreEqual(1L, (long)obj["a"]!);
        Assert.AreEqual("x", (string)obj["b"]!);
    }

    /// <summary>
    /// Verifies that the enumerable constructor throws <see cref="ArgumentNullException" /> with <c>ParamName</c>
    /// <c>items</c> when the sequence is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new TomlObject((IEnumerable<KeyValuePair<string, TomlNode?>>)null!);
        }, "items");
    }

    /// <summary>
    /// Verifies that the enumerable constructor throws <see cref="InvalidOperationException" /> when a supplied value
    /// already belongs to another container.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemValueBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        var owned = TomlValue.Create(1L);
        var owner = new TomlArray();
        owner.Add(owned);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new TomlObject([new KeyValuePair<string, TomlNode?>("a", owned)]);
        });
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="KeyNotFoundException" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenGetWithAbsentKey_ShouldThrowKeyNotFoundException()
    {
        var obj = new TomlObject();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = obj["missing"];
        });
    }

    /// <summary>
    /// Verifies that the indexer setter throws <see cref="ArgumentNullException" /> with <c>ParamName</c>
    /// <c>key</c> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetWithNullKey_ShouldThrowArgumentNullException()
    {
        var obj = new TomlObject();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            obj[null!] = TomlValue.Create(1L);
        }, "key");
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="ArgumentNullException" /> with <c>ParamName</c>
    /// <c>key</c> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenGetWithNullKey_ShouldThrowArgumentNullException()
    {
        var obj = new TomlObject();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = obj[null!];
        }, "key");
    }

    /// <summary>
    /// Verifies that assigning to an existing key replaces the stored value without growing the table.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetExistingKey_ShouldReplaceValue()
    {
        var obj = new TomlObject();
        obj["k"] = 1L;

        obj["k"] = 2L;

        Assert.AreEqual(1, obj.Count);
        Assert.AreEqual(2L, (long)obj["k"]!);
    }

    /// <summary>
    /// Verifies that assigning over an existing entry detaches the replaced node, clearing its
    /// <see cref="TomlNode.Parent" /> so it can join another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetExistingKey_ShouldDetachPreviousValue()
    {
        var obj = new TomlObject();
        var replaced = TomlValue.Create(1L);
        obj["k"] = replaced;

        obj["k"] = 2L;

        Assert.IsNull(replaced.Parent);
        var adopter = new TomlArray();
        adopter.Add(replaced);
        Assert.AreSame(adopter, replaced.Parent);
    }

    /// <summary>
    /// Verifies that re-assigning the same node to its current key keeps the node attached to the table.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetSameNodeOverItself_ShouldKeepParent()
    {
        var obj = new TomlObject();
        var value = TomlValue.Create(1L);
        obj["k"] = value;

        obj["k"] = value;

        Assert.AreSame(obj, value.Parent);
        Assert.AreEqual(1, obj.Count);
    }

    /// <summary>
    /// Verifies that assigning a node that already belongs to another container throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssignedNodeBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        var owned = TomlValue.Create(1L);
        var owner = new TomlObject();
        owner["k"] = owned;

        var other = new TomlObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            other["k"] = owned;
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.Add(string, TomlNode?)" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>key</c> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var obj = new TomlObject();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            obj.Add(null!, TomlValue.Create(1L));
        }, "key");
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.Add(string, TomlNode?)" /> throws <see cref="ArgumentException" /> when the
    /// key already exists.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldThrowArgumentException()
    {
        var obj = new TomlObject();
        obj.Add("k", TomlValue.Create(1L));

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            obj.Add("k", TomlValue.Create(2L));
        });
    }

    /// <summary>
    /// Verifies that removing a present key removes the entry and returns <see langword="true" />, while an absent key
    /// returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyPresentOrAbsent_ShouldReportRemoval()
    {
        var obj = new TomlObject();
        obj["k"] = 1L;

        Assert.IsTrue(obj.Remove("k"));
        Assert.AreEqual(0, obj.Count);
        Assert.IsFalse(obj.Remove("k"));
    }

    /// <summary>
    /// Verifies that removing an entry detaches its value, clearing <see cref="TomlNode.Parent" /> so the node can be
    /// added to another container.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueRemoved_ShouldDetachFromParent()
    {
        var obj = new TomlObject();
        var value = TomlValue.Create(1L);
        obj["k"] = value;

        _ = obj.Remove("k");

        Assert.IsNull(value.Parent);
        var adopter = new TomlObject();
        adopter["other"] = value;
        Assert.AreSame(adopter, value.Parent);
    }

    /// <summary>
    /// Verifies that removing a matching key/value pair detaches the value and returns <see langword="true" />, while
    /// a pair whose value does not match leaves the entry in place.
    /// </summary>
    [TestMethod]
    public void Remove_WhenPairMatches_ShouldRemoveAndDetach()
    {
        var obj = new TomlObject();
        var value = TomlValue.Create(1L);
        obj["k"] = value;

        Assert.IsFalse(obj.Remove(new KeyValuePair<string, TomlNode?>("k", TomlValue.Create(9L))));
        Assert.AreEqual(1, obj.Count);

        Assert.IsTrue(obj.Remove(new KeyValuePair<string, TomlNode?>("k", value)));
        Assert.AreEqual(0, obj.Count);
        Assert.IsNull(value.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.Clear" /> removes every entry and detaches each child node.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllEntriesAndDetachChildren()
    {
        var obj = new TomlObject();
        var first = TomlValue.Create(1L);
        var second = TomlValue.Create("x");
        obj["a"] = first;
        obj["b"] = second;

        obj.Clear();

        Assert.AreEqual(0, obj.Count);
        Assert.IsNull(first.Parent);
        Assert.IsNull(second.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.ContainsKey(string)" /> reports key membership.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyPresentOrAbsent_ShouldReportMembership()
    {
        var obj = new TomlObject();
        obj["present"] = 1L;

        Assert.IsTrue(obj.ContainsKey("present"));
        Assert.IsFalse(obj.ContainsKey("absent"));
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.TryGetPropertyValue(string, out TomlNode?)" /> returns
    /// <see langword="true" /> with the value for a present key and <see langword="false" /> with
    /// <see langword="null" /> for an absent one.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyValue_WhenKeyPresentOrAbsent_ShouldReturnExpected()
    {
        var obj = new TomlObject();
        obj["k"] = 1L;

        Assert.IsTrue(obj.TryGetPropertyValue("k", out TomlNode? present));
        Assert.AreEqual(1L, (long)present!);
        Assert.IsFalse(obj.TryGetPropertyValue("missing", out TomlNode? absent));
        Assert.IsNull(absent);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.Contains(KeyValuePair{string, TomlNode?})" /> matches on both key and value
    /// identity.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPairMatchesKeyAndValue_ShouldReturnTrue()
    {
        var obj = new TomlObject();
        var value = TomlValue.Create(1L);
        obj["k"] = value;

        Assert.IsTrue(obj.Contains(new KeyValuePair<string, TomlNode?>("k", value)));
        Assert.IsFalse(obj.Contains(new KeyValuePair<string, TomlNode?>("k", TomlValue.Create(1L))));
        Assert.IsFalse(obj.Contains(new KeyValuePair<string, TomlNode?>("other", value)));
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.CopyTo(KeyValuePair{string, TomlNode?}[], int)" /> copies every pair
    /// starting at the supplied index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldCopyPairsFromArrayIndex()
    {
        var obj = new TomlObject();
        obj["a"] = 1L;
        obj["b"] = 2L;

        var target = new KeyValuePair<string, TomlNode?>[3];
        obj.CopyTo(target, 1);

        Assert.IsNull(target[0].Key);
        List<string> keys = [target[1].Key, target[2].Key];
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, keys);
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.Keys" /> and <see cref="TomlObject.Values" /> expose every entry.
    /// </summary>
    [TestMethod]
    public void KeysAndValues_WhenEntriesAdded_ShouldExposeAllEntries()
    {
        var obj = new TomlObject();
        var first = TomlValue.Create(1L);
        var second = TomlValue.Create(2L);
        obj["a"] = first;
        obj["b"] = second;

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, obj.Keys.ToList());
        CollectionAssert.AreEquivalent(new TomlNode?[] { first, second }, obj.Values.ToList());
    }

    /// <summary>
    /// Verifies that mutating the table while enumerating its pairs throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCollectionModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var obj = new TomlObject();
        obj["a"] = 1L;
        obj["b"] = 2L;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, TomlNode?> entry in obj)
                obj["c"] = 3L;
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.IsReadOnly" /> is <see langword="false" /> and
    /// <see cref="TomlObject.GetValueKind" /> reports <see cref="TomlValueKind.Table" />.
    /// </summary>
    [TestMethod]
    public void GetValueKind_WhenObject_ShouldReportTableKind()
    {
        var obj = new TomlObject();

        Assert.AreEqual(TomlValueKind.Table, obj.GetValueKind());
        Assert.IsFalse(obj.IsReadOnly);
    }

    /// <summary>
    /// Verifies that serialization preserves insertion order — TOML tables are never key-sorted, in contrast to the
    /// canonical key ordering of Bencode dictionaries.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenKeysInsertedUnsorted_ShouldPreserveInsertionOrder()
    {
        var obj = new TomlObject();
        obj["zebra"] = 1L;
        obj["apple"] = 2L;
        obj["mango"] = 3L;

        Assert.AreEqual("zebra = 1\napple = 2\nmango = 3\n", obj.ToString());
    }

    /// <summary>
    /// Verifies that property lookups honour the case-insensitive comparison selected through
    /// <see cref="TomlNodeOptions" /> while serialization preserves the stored key spelling.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsSelectCaseInsensitive_ShouldMatchKeysIgnoringCase()
    {
        var obj = new TomlObject(new TomlNodeOptions { PropertyNameCaseInsensitive = true });
        obj["Name"] = "x";

        Assert.IsTrue(obj.ContainsKey("name"));
        Assert.IsTrue(obj.ContainsKey("NAME"));
        Assert.AreEqual("Name = \"x\"\n", obj.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.ToString" /> renders an empty table as an empty document, because the TOML
    /// root table has no delimiter tokens.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTableEmpty_ShouldRenderEmptyDocument()
    {
        var obj = new TomlObject();

        Assert.AreEqual(string.Empty, obj.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="TomlObject.DeepClone" /> produces a parentless root whose children belong to the
    /// clone, not to the original.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenCloned_ShouldProduceParentlessRootWithOwnChildren()
    {
        var owner = new TomlArray();
        var obj = new TomlObject();
        obj["a"] = 1L;
        owner.Add(obj);

        TomlObject clone = obj.DeepClone().AsObject();

        Assert.IsNull(clone.Parent);
        Assert.AreSame(clone, clone["a"]!.Parent);
        Assert.AreNotSame(obj["a"], clone["a"]);
    }

    /// <summary>
    /// Verifies that a nested table member serializes as a <c>[header]</c> block following the parent's scalar
    /// members.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenNestedTableFollowsScalars_ShouldEmitHeaderBlockAfterScalars()
    {
        var root = new TomlObject();
        root["port"] = 8080L;
        var sub = new TomlObject();
        sub["k"] = "v";
        root["sub"] = sub;

        Assert.AreEqual("port = 8080\n\n[sub]\nk = \"v\"\n", root.ToString());
    }
}
