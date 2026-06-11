// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlArrayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies the <see cref="TomlArray" /> list surface: ordered access, insertion, removal with parent detachment, the
/// single-parent rule on assignment, enumeration semantics, and insertion-order serialization.
/// </summary>
[TestClass]
public class TomlArrayTests
{
    /// <summary>
    /// Verifies that the params constructor preserves the supplied items in order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsProvided_ShouldPreserveOrder()
    {
        var array = new TomlArray(3, "two", 1);

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(3L, (long)array[0]!);
        Assert.AreEqual("two", (string)array[1]!);
        Assert.AreEqual(1L, (long)array[2]!);
    }

    /// <summary>
    /// Verifies that the params constructor throws <see cref="ArgumentNullException" /> when the items array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemsIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TomlArray((TomlNode?[])null!);
        });
    }

    /// <summary>
    /// Verifies that the params constructor throws <see cref="InvalidOperationException" /> when a supplied item
    /// already belongs to another container.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenItemBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        TomlValue owned = TomlValue.Create(1L);
        var owner = new TomlArray();
        owner.Add(owned);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new TomlArray(owned);
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
        var array = new TomlArray(1);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = array[index];
        });
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
        var array = new TomlArray(1);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array[index] = 2L;
        });
    }

    /// <summary>
    /// Verifies that assigning over an existing element detaches the replaced node, clearing its
    /// <see cref="TomlNode.Parent" /> so it can join another container.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetReplacesElement_ShouldDetachPreviousElement()
    {
        TomlValue replaced = TomlValue.Create(1L);
        var array = new TomlArray();
        array.Add(replaced);

        array[0] = 2L;

        Assert.IsNull(replaced.Parent);
        var adopter = new TomlArray();
        adopter.Add(replaced);
        Assert.AreSame(adopter, replaced.Parent);
    }

    /// <summary>
    /// Verifies that re-assigning the same node over itself keeps the node attached to the array.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetSameNodeOverItself_ShouldKeepParent()
    {
        TomlValue value = TomlValue.Create(1L);
        var array = new TomlArray();
        array.Add(value);

        array[0] = value;

        Assert.AreSame(array, value.Parent);
    }

    /// <summary>
    /// Verifies that assigning a node that already belongs to another container throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssignedNodeBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        TomlValue owned = TomlValue.Create(1L);
        var owner = new TomlArray();
        owner.Add(owned);

        var array = new TomlArray(0);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            array[0] = owned;
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.Add(TomlNode?)" /> appends items in order, including a
    /// <see langword="null" /> entry, which is representable in memory.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAdded_ShouldAppendInOrder()
    {
        var array = new TomlArray();
        array.Add(1);
        array.Add(null);
        array.Add("x");

        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(1L, (long)array[0]!);
        Assert.IsNull(array[1]);
        Assert.AreEqual("x", (string)array[2]!);
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.Insert(int, TomlNode?)" /> shifts subsequent elements and accepts the
    /// boundary positions zero and <see cref="TomlArray.Count" />.
    /// </summary>
    [TestMethod]
    public void Insert_WhenIndexWithinBounds_ShouldShiftSubsequentElements()
    {
        var array = new TomlArray(2, 4);

        array.Insert(0, 1);
        array.Insert(2, 3);
        array.Insert(array.Count, 5);

        CollectionAssert.AreEqual(
            new long?[] { 1, 2, 3, 4, 5 },
            array.Select(item => (long?)item!.GetValue<long>()).ToList());
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.Insert(int, TomlNode?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is outside the permitted range.
    /// </summary>
    /// <param name="index">The out-of-range insertion index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void Insert_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        var array = new TomlArray(1);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Insert(index, 2L);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.Remove(TomlNode?)" /> removes the first occurrence, detaches it, and returns
    /// <see langword="false" /> for an absent item.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemPresentOrAbsent_ShouldRemoveFirstOccurrenceAndDetach()
    {
        TomlValue item = TomlValue.Create(1L);
        var array = new TomlArray();
        array.Add(item);
        array.Add(2);

        Assert.IsTrue(array.Remove(item));
        Assert.AreEqual(1, array.Count);
        Assert.IsNull(item.Parent);
        Assert.IsFalse(array.Remove(item));
    }

    /// <summary>
    /// Verifies that a removed element can be re-added to a different container, because removal cleared its
    /// <see cref="TomlNode.Parent" />.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemRemoved_ShouldAllowAdoptionByAnotherContainer()
    {
        TomlValue item = TomlValue.Create(1L);
        var array = new TomlArray();
        array.Add(item);

        _ = array.Remove(item);

        var adopter = new TomlObject();
        adopter["k"] = item;
        Assert.AreSame(adopter, item.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.RemoveAt(int)" /> removes the element at the index and detaches it.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenIndexValid_ShouldRemoveAndDetach()
    {
        TomlValue item = TomlValue.Create(2L);
        var array = new TomlArray();
        array.Add(1);
        array.Add(item);

        array.RemoveAt(1);

        Assert.AreEqual(1, array.Count);
        Assert.IsNull(item.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.RemoveAt(int)" /> throws <see cref="ArgumentOutOfRangeException" /> when the
    /// index is outside the bounds of the list.
    /// </summary>
    /// <param name="index">The out-of-range index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    public void RemoveAt_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException(int index)
    {
        var array = new TomlArray(1);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.RemoveAt(index);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.Clear" /> removes every element and detaches each child node.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldRemoveAllElementsAndDetachChildren()
    {
        TomlValue first = TomlValue.Create(1L);
        TomlValue second = TomlValue.Create("x");
        var array = new TomlArray();
        array.Add(first);
        array.Add(second);

        array.Clear();

        Assert.AreEqual(0, array.Count);
        Assert.IsNull(first.Parent);
        Assert.IsNull(second.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.Contains(TomlNode?)" /> matches by reference identity and reports a stored
    /// <see langword="null" /> entry.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemPresentOrAbsent_ShouldReportMembership()
    {
        TomlValue item = TomlValue.Create(1L);
        var array = new TomlArray();
        array.Add(item);
        array.Add(null);

        Assert.IsTrue(array.Contains(item));
        Assert.IsTrue(array.Contains(null));
        Assert.IsFalse(array.Contains(TomlValue.Create(1L)));
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.IndexOf(TomlNode?)" /> returns the first matching index, or <c>-1</c> when
    /// the item is absent.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenItemPresentOrAbsent_ShouldReturnExpectedIndex()
    {
        TomlValue item = TomlValue.Create(1L);
        var array = new TomlArray();
        array.Add(0);
        array.Add(item);

        Assert.AreEqual(1, array.IndexOf(item));
        Assert.AreEqual(-1, array.IndexOf(null));
        Assert.AreEqual(-1, array.IndexOf(TomlValue.Create(1L)));
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.CopyTo(TomlNode?[], int)" /> copies the elements in order starting at the
    /// supplied index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCalled_ShouldCopyElementsFromArrayIndex()
    {
        TomlValue first = TomlValue.Create(1L);
        TomlValue second = TomlValue.Create(2L);
        var array = new TomlArray();
        array.Add(first);
        array.Add(second);

        var target = new TomlNode?[3];
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
        var array = new TomlArray(1, 2);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (TomlNode? item in array)
                array.Add(3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.IsReadOnly" /> is <see langword="false" /> and
    /// <see cref="TomlArray.GetValueKind" /> reports <see cref="TomlValueKind.Array" />.
    /// </summary>
    [TestMethod]
    public void GetValueKind_WhenArray_ShouldReportArrayKind()
    {
        var array = new TomlArray();

        Assert.AreEqual(TomlValueKind.Array, array.GetValueKind());
        Assert.IsFalse(array.IsReadOnly);
    }

    /// <summary>
    /// Verifies that serialization preserves insertion order — array elements are never sorted.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenElementsAdded_ShouldPreserveInsertionOrder()
    {
        var root = new TomlObject();
        root["a"] = new TomlArray(3, 1, 2);

        Assert.AreEqual("a = [3, 1, 2]\n", root.ToString());
    }

    /// <summary>
    /// Verifies that nested arrays serialize as nested inline arrays.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenArraysNested_ShouldEmitNestedInlineArrays()
    {
        var root = new TomlObject();
        var inner = new TomlArray(2);
        root["a"] = new TomlArray(1, inner);

        Assert.AreEqual("a = [1, [2]]\n", root.ToString());
    }

    /// <summary>
    /// Verifies that an array accepts heterogeneous element kinds, which TOML v1.0 permits.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenElementsHeterogeneous_ShouldEmitEachKind()
    {
        var root = new TomlObject();
        root["a"] = new TomlArray(1, "x", true, 1.5);

        Assert.AreEqual("a = [1, \"x\", true, 1.5]\n", root.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="TomlArray.DeepClone" /> preserves a <see langword="null" /> entry and yields an
    /// independent, parentless tree.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenArrayContainsNullEntry_ShouldPreserveNullInClone()
    {
        var array = new TomlArray();
        array.Add(1);
        array.Add(null);

        TomlArray clone = array.DeepClone().AsArray();

        Assert.IsNull(clone.Parent);
        Assert.AreEqual(2, clone.Count);
        Assert.IsNull(clone[1]);
        Assert.AreNotSame(array[0], clone[0]);
    }
}
