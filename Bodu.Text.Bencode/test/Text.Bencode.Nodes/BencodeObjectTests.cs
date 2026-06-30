// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeObjectTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeObject" /> dictionary surface: keyed access, membership, removal with parent
/// detachment, the single-parent rule on assignment, enumeration semantics, and canonical key-sorted serialization.
/// </summary>
[TestClass]
public class BencodeObjectTests
{
    /// <summary>
    /// Verifies that the enumerable constructor copies the supplied entries into the object.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsProvided_ShouldContainEntries()
    {
        var items = new List<KeyValuePair<string, BencodeNode?>>
        {
            new("a", BencodeValue.Create(1L)),
            new("b", BencodeValue.Create("x")),
        };

        var obj = new BencodeObject(items);

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
            _ = new BencodeObject((IEnumerable<KeyValuePair<string, BencodeNode?>>)null!);
        }, "items");
    }

    /// <summary>
    /// Verifies that the enumerable constructor throws <see cref="InvalidOperationException" /> when a supplied value
    /// already belongs to another container.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemValueBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        var owned = BencodeValue.Create(1L);
        var owner = new BencodeArray();
        owner.Add(owned);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new BencodeObject([new KeyValuePair<string, BencodeNode?>("a", owned)]);
        });
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="KeyNotFoundException" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenGetWithAbsentKey_ShouldThrowKeyNotFoundException()
    {
        var obj = new BencodeObject();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = obj["missing"];
        });
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="ArgumentNullException" /> with <c>ParamName</c>
    /// <c>key</c> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenGetWithNullKey_ShouldThrowArgumentNullException()
    {
        var obj = new BencodeObject();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = obj[null!];
        }, "key");
    }

    /// <summary>
    /// Verifies that the indexer setter throws <see cref="ArgumentNullException" /> with <c>ParamName</c>
    /// <c>key</c> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetWithNullKey_ShouldThrowArgumentNullException()
    {
        var obj = new BencodeObject();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            obj[null!] = BencodeValue.Create(1L);
        }, "key");
    }

    /// <summary>
    /// Verifies that assigning to an existing key replaces the stored value.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetExistingKey_ShouldReplaceValue()
    {
        var obj = new BencodeObject();
        obj["k"] = 1L;

        obj["k"] = 2L;

        Assert.AreEqual(1, obj.Count);
        Assert.AreEqual(2L, (long)obj["k"]!);
    }

    /// <summary>
    /// Verifies that assigning over an existing entry detaches the replaced node, clearing its
    /// <see cref="BencodeNode.Parent" /> so it can join another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetExistingKey_ShouldDetachPreviousValue()
    {
        var obj = new BencodeObject();
        var replaced = BencodeValue.Create(1L);
        obj["k"] = replaced;

        obj["k"] = 2L;

        Assert.IsNull(replaced.Parent);
        var adopter = new BencodeArray();
        adopter.Add(replaced);
        Assert.AreSame(adopter, replaced.Parent);
    }

    /// <summary>
    /// Verifies that assigning a node that already belongs to another container throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssignedNodeBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        var owned = BencodeValue.Create(1L);
        var owner = new BencodeObject();
        owner["k"] = owned;

        var other = new BencodeObject();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            other["k"] = owned;
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.Add(string, BencodeNode?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>key</c> when the key is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var obj = new BencodeObject();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            obj.Add(null!, BencodeValue.Create(1L));
        }, "key");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.Add(string, BencodeNode?)" /> throws <see cref="ArgumentException" />
    /// when the key already exists.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldThrowArgumentException()
    {
        var obj = new BencodeObject();
        obj.Add("k", BencodeValue.Create(1L));

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            obj.Add("k", BencodeValue.Create(2L));
        });
    }

    /// <summary>
    /// Verifies that removing a present key removes the entry and returns <see langword="true" />, while an absent
    /// key returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyPresentOrAbsent_ShouldReportRemoval()
    {
        var obj = new BencodeObject();
        obj["k"] = 1L;

        Assert.IsTrue(obj.Remove("k"));
        Assert.AreEqual(0, obj.Count);
        Assert.IsFalse(obj.Remove("k"));
    }

    /// <summary>
    /// Verifies that removing an entry detaches its value, clearing <see cref="BencodeNode.Parent" /> so the node can
    /// be added to another container.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueRemoved_ShouldDetachFromParent()
    {
        var obj = new BencodeObject();
        var value = BencodeValue.Create(1L);
        obj["k"] = value;

        _ = obj.Remove("k");

        Assert.IsNull(value.Parent);
        var adopter = new BencodeObject();
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
        var obj = new BencodeObject();
        var value = BencodeValue.Create(1L);
        obj["k"] = value;

        Assert.IsFalse(obj.Remove(new KeyValuePair<string, BencodeNode?>("k", BencodeValue.Create(9L))));
        Assert.AreEqual(1, obj.Count);

        Assert.IsTrue(obj.Remove(new KeyValuePair<string, BencodeNode?>("k", value)));
        Assert.AreEqual(0, obj.Count);
        Assert.IsNull(value.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.Clear" /> removes every entry and detaches each child node.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllEntriesAndDetachChildren()
    {
        var obj = new BencodeObject();
        var first = BencodeValue.Create(1L);
        var second = BencodeValue.Create("x");
        obj["a"] = first;
        obj["b"] = second;

        obj.Clear();

        Assert.AreEqual(0, obj.Count);
        Assert.IsNull(first.Parent);
        Assert.IsNull(second.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.ContainsKey(string)" /> reports key membership.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyPresentOrAbsent_ShouldReportMembership()
    {
        var obj = new BencodeObject();
        obj["present"] = 1L;

        Assert.IsTrue(obj.ContainsKey("present"));
        Assert.IsFalse(obj.ContainsKey("absent"));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.TryGetPropertyValue(string, out BencodeNode?)" /> returns
    /// <see langword="true" /> with the value for a present key and <see langword="false" /> with
    /// <see langword="null" /> for an absent one.
    /// </summary>
    [TestMethod]
    public void TryGetPropertyValue_WhenKeyPresentOrAbsent_ShouldReturnExpected()
    {
        var obj = new BencodeObject();
        obj["k"] = 1L;

        Assert.IsTrue(obj.TryGetPropertyValue("k", out BencodeNode? present));
        Assert.AreEqual(1L, (long)present!);
        Assert.IsFalse(obj.TryGetPropertyValue("missing", out BencodeNode? absent));
        Assert.IsNull(absent);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.Contains(KeyValuePair{string, BencodeNode?})" /> matches on both key and
    /// value identity.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPairMatchesKeyAndValue_ShouldReturnTrue()
    {
        var obj = new BencodeObject();
        var value = BencodeValue.Create(1L);
        obj["k"] = value;

        Assert.IsTrue(obj.Contains(new KeyValuePair<string, BencodeNode?>("k", value)));
        Assert.IsFalse(obj.Contains(new KeyValuePair<string, BencodeNode?>("k", BencodeValue.Create(1L))));
        Assert.IsFalse(obj.Contains(new KeyValuePair<string, BencodeNode?>("other", value)));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.CopyTo(KeyValuePair{string, BencodeNode?}[], int)" /> copies every pair
    /// starting at the supplied index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldCopyPairsFromArrayIndex()
    {
        var obj = new BencodeObject();
        obj["a"] = 1L;
        obj["b"] = 2L;

        var target = new KeyValuePair<string, BencodeNode?>[3];
        obj.CopyTo(target, 1);

        Assert.IsNull(target[0].Key);
        List<string> keys = [target[1].Key, target[2].Key];
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, keys);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.Keys" /> and <see cref="BencodeObject.Values" /> expose every entry.
    /// </summary>
    [TestMethod]
    public void KeysAndValues_WhenEntriesAdded_ShouldExposeAllEntries()
    {
        var obj = new BencodeObject();
        var first = BencodeValue.Create(1L);
        var second = BencodeValue.Create(2L);
        obj["a"] = first;
        obj["b"] = second;

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, obj.Keys.ToList());
        CollectionAssert.AreEquivalent(new BencodeNode?[] { first, second }, obj.Values.ToList());
    }

    /// <summary>
    /// Verifies that mutating the object while enumerating its pairs throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCollectionModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var obj = new BencodeObject();
        obj["a"] = 1L;
        obj["b"] = 2L;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, BencodeNode?> entry in obj)
                obj["c"] = 3L;
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.IsReadOnly" /> is <see langword="false" /> and
    /// <see cref="BencodeObject.GetValueKind" /> reports <see cref="BencodeValueKind.Object" />.
    /// </summary>
    [TestMethod]
    public void GetValueKind_WhenObject_ShouldReportObjectKind()
    {
        var obj = new BencodeObject();

        Assert.AreEqual(BencodeValueKind.Object, obj.GetValueKind());
        Assert.IsFalse(obj.IsReadOnly);
    }

    /// <summary>
    /// Verifies that serialization sorts keys bytewise — shorter prefixes first and uppercase (lower byte values)
    /// before lowercase — regardless of insertion order.
    /// </summary>
    [TestMethod]
    public void ToByteArray_WhenKeysDifferByCaseAndLength_ShouldSortBytewise()
    {
        var obj = new BencodeObject();
        obj["b"] = 1L;
        obj["aa"] = 2L;
        obj["Z"] = 3L;
        obj["a"] = 4L;

        byte[] bytes = obj.ToByteArray();

        Assert.AreEqual("d1:Zi3e1:ai4e2:aai2e1:bi1ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.ToString" /> renders the canonical serialized text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSerialized_ShouldRenderCanonicalText()
    {
        var obj = new BencodeObject();
        obj["b"] = 2L;
        obj["a"] = "x";

        Assert.AreEqual("d1:a1:x1:bi2ee", obj.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObject.DeepClone" /> produces a parentless root whose children belong to the
    /// clone, not to the original.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenCloned_ShouldProduceParentlessRootWithOwnChildren()
    {
        var owner = new BencodeArray();
        var obj = new BencodeObject();
        obj["a"] = 1L;
        owner.Add(obj);

        BencodeObject clone = obj.DeepClone().AsObject();

        Assert.IsNull(clone.Parent);
        Assert.AreSame(clone, clone["a"]!.Parent);
        Assert.AreNotSame(obj["a"], clone["a"]);
    }
}
