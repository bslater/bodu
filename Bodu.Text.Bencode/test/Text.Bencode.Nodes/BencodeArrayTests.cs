// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeArrayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeArray" /> list surface: ordered access, insertion, removal with parent detachment,
/// the single-parent rule on assignment, enumeration semantics, and insertion-order serialization.
/// </summary>
[TestClass]
public class BencodeArrayTests
{
    /// <summary>
    /// Verifies that the params constructor preserves the supplied items in order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsProvided_ShouldPreserveOrder()
    {
        var array = new BencodeArray(3, "two", 1);

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(3L, (long)array[0]!);
        Assert.AreEqual("two", (string)array[1]!);
        Assert.AreEqual(1L, (long)array[2]!);
    }

    /// <summary>
    /// Verifies that the params constructor throws <see cref="ArgumentNullException" /> with <c>ParamName</c>
    /// <c>items</c> when the items array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new BencodeArray((BencodeNode?[])null!);
        }, "items");
    }

    /// <summary>
    /// Verifies that the params constructor throws <see cref="InvalidOperationException" /> when a supplied item
    /// already belongs to another container.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        var owned = BencodeValue.Create(1L);
        var owner = new BencodeArray();
        owner.Add(owned);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new BencodeArray(owned);
        });
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="ArgumentOutOfRangeException" /> when the index is outside
    /// the bounds of the list.
    /// </summary>
    /// <param name="index">The out-of-range index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    public void Indexer_WhenGetOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        var array = new BencodeArray(1);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = array[index];
        }, "index");
    }

    /// <summary>
    /// Verifies that the indexer setter throws <see cref="ArgumentOutOfRangeException" /> when the index is outside
    /// the bounds of the list.
    /// </summary>
    /// <param name="index">The out-of-range index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    public void Indexer_WhenSetOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        var array = new BencodeArray(1);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            array[index] = 2L;
        }, "index");
    }

    /// <summary>
    /// Verifies that assigning over an existing element detaches the replaced node, clearing its
    /// <see cref="BencodeNode.Parent" /> so it can join another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetReplacesElement_ShouldDetachPreviousElement()
    {
        var replaced = BencodeValue.Create(1L);
        var array = new BencodeArray();
        array.Add(replaced);

        array[0] = 2L;

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
        var owner = new BencodeArray();
        owner.Add(owned);

        var array = new BencodeArray(0);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array[0] = owned;
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.Add(BencodeNode?)" /> appends items in order, including a
    /// <see langword="null" /> entry, which is representable in memory.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAdded_ShouldAppendInOrder()
    {
        var array = new BencodeArray();
        array.Add(1);
        array.Add(null);
        array.Add("x");

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(1L, (long)array[0]!);
        Assert.IsNull(array[1]);
        Assert.AreEqual("x", (string)array[2]!);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.Insert(int, BencodeNode?)" /> shifts subsequent elements and accepts the
    /// boundary positions zero and <see cref="BencodeArray.Count" />.
    /// </summary>
    [TestMethod]
    public void Insert_WhenIndexWithinBounds_ShouldShiftSubsequentElements()
    {
        var array = new BencodeArray(2, 4);

        array.Insert(0, 1);
        array.Insert(2, 3);
        array.Insert(array.Count, 5);

        Assert.AreEqual("li1ei2ei3ei4ei5ee", array.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.Insert(int, BencodeNode?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is outside the permitted range.
    /// </summary>
    /// <param name="index">The out-of-range insertion index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void Insert_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        var array = new BencodeArray(1);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            array.Insert(index, 2L);
        }, "index");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.Remove(BencodeNode?)" /> removes the first occurrence, detaches it, and
    /// returns <see langword="false" /> for an absent item.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemPresentOrAbsent_ShouldRemoveFirstOccurrenceAndDetach()
    {
        var item = BencodeValue.Create(1L);
        var array = new BencodeArray();
        array.Add(item);
        array.Add(2);

        Assert.IsTrue(array.Remove(item));
        Assert.AreEqual(1, array.Count);
        Assert.IsNull(item.Parent);
        Assert.IsFalse(array.Remove(item));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.RemoveAt(int)" /> removes the element at the index and detaches it.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenIndexValid_ShouldRemoveAndDetach()
    {
        var item = BencodeValue.Create(2L);
        var array = new BencodeArray();
        array.Add(1);
        array.Add(item);

        array.RemoveAt(1);

        Assert.AreEqual(1, array.Count);
        Assert.IsNull(item.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.RemoveAt(int)" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// the index is outside the bounds of the list.
    /// </summary>
    /// <param name="index">The out-of-range index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    public void RemoveAt_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        var array = new BencodeArray(1);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            array.RemoveAt(index);
        }, "index");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.Clear" /> removes every element and detaches each child node.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllElementsAndDetachChildren()
    {
        var first = BencodeValue.Create(1L);
        var second = BencodeValue.Create("x");
        var array = new BencodeArray();
        array.Add(first);
        array.Add(second);

        array.Clear();

        Assert.AreEqual(0, array.Count);
        Assert.IsNull(first.Parent);
        Assert.IsNull(second.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.Contains(BencodeNode?)" /> matches by reference identity and reports a
    /// stored <see langword="null" /> entry.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemPresentOrAbsent_ShouldReportMembership()
    {
        var item = BencodeValue.Create(1L);
        var array = new BencodeArray();
        array.Add(item);
        array.Add(null);

        Assert.IsTrue(array.Contains(item));
        Assert.IsTrue(array.Contains(null));
        Assert.IsFalse(array.Contains(BencodeValue.Create(1L)));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.IndexOf(BencodeNode?)" /> returns the first matching index, or
    /// <c>-1</c> when the item is absent.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenItemPresentOrAbsent_ShouldReturnExpectedIndex()
    {
        var item = BencodeValue.Create(1L);
        var array = new BencodeArray();
        array.Add(0);
        array.Add(item);

        Assert.AreEqual(1, array.IndexOf(item));
        Assert.AreEqual(-1, array.IndexOf(null));
        Assert.AreEqual(-1, array.IndexOf(BencodeValue.Create(1L)));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.CopyTo(BencodeNode?[], int)" /> copies the elements in order starting at
    /// the supplied index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldCopyElementsFromArrayIndex()
    {
        var first = BencodeValue.Create(1L);
        var second = BencodeValue.Create(2L);
        var array = new BencodeArray();
        array.Add(first);
        array.Add(second);

        var target = new BencodeNode?[3];
        array.CopyTo(target, 1);

        Assert.IsNull(target[0]);
        Assert.AreSame(first, target[1]);
        Assert.AreSame(second, target[2]);
    }

    /// <summary>
    /// Verifies that mutating the array while enumerating its elements throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCollectionModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var array = new BencodeArray(1, 2);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (BencodeNode? item in array)
                array.Add(3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.IsReadOnly" /> is <see langword="false" /> and
    /// <see cref="BencodeArray.GetValueKind" /> reports <see cref="BencodeValueKind.Array" />.
    /// </summary>
    [TestMethod]
    public void GetValueKind_WhenArray_ShouldReportArrayKind()
    {
        var array = new BencodeArray();

        Assert.AreEqual(BencodeValueKind.Array, array.GetValueKind());
        Assert.IsFalse(array.IsReadOnly);
    }

    /// <summary>
    /// Verifies that serialization preserves insertion order — list elements are never sorted.
    /// </summary>
    [TestMethod]
    public void ToByteArray_WhenElementsAdded_ShouldPreserveInsertionOrder()
    {
        var array = new BencodeArray(3, 1, 2);

        byte[] bytes = array.ToByteArray();

        Assert.AreEqual("li3ei1ei2ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that nested arrays serialize as nested lists.
    /// </summary>
    [TestMethod]
    public void ToByteArray_WhenArraysNested_ShouldEmitNestedLists()
    {
        var inner = new BencodeArray(2);
        var array = new BencodeArray(1, inner);

        Assert.AreEqual("li1eli2eee", array.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeArray.DeepClone" /> preserves a <see langword="null" /> entry and yields an
    /// independent, parentless tree.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenArrayContainsNullEntry_ShouldPreserveNullInClone()
    {
        var array = new BencodeArray();
        array.Add(1);
        array.Add(null);

        BencodeArray clone = array.DeepClone().AsArray();

        Assert.IsNull(clone.Parent);
        Assert.AreEqual(2, clone.Count);
        Assert.IsNull(clone[1]);
        Assert.AreNotSame(array[0], clone[0]);
    }
}
