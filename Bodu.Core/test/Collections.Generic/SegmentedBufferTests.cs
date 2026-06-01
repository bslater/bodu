// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SegmentedBufferTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

[TestClass]
public sealed class SegmentedBufferTests
{

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.Add" /> increments <see cref="SegmentedBuffer{T}.Count" /> by one for each appended item.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemsAreAppended_ShouldIncrementCount()
    {
        var buffer = new SegmentedBuffer<int>(4);

        for (var i = 0; i < 10; i++)
            buffer.Add(i);

        Assert.AreEqual(10, buffer.Count);
    }

    /// <summary>
    /// Verifies that reference-type elements stored in the buffer are returned without modification.
    /// </summary>
    [TestMethod]
    public void Add_WhenStoringReferenceTypes_ShouldPreserveReferenceIdentity()
    {
        var buffer = new SegmentedBuffer<string>(2);
        var a = new string('a', 3);
        var b = new string('b', 3);
        var c = new string('c', 3);

        buffer.Add(a);
        buffer.Add(b);
        buffer.Add(c);

        Assert.AreSame(a, buffer[0]);
        Assert.AreSame(b, buffer[1]);
        Assert.AreSame(c, buffer[2]);
    }
    /// <summary>
    /// Verifies that the default constructor initialises an empty buffer.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldInitialiseEmptyBuffer()
    {
        var buffer = new SegmentedBuffer<int>();
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that the segment-size constructor throws <see cref="ArgumentOutOfRangeException" /> when the segment size is below one.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenSegmentSizeIsLessThanOne_ShouldThrowExactly(int segmentSize)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new SegmentedBuffer<int>(segmentSize);
        });
    }

    /// <summary>
    /// Verifies that the segment-size constructor initialises an empty buffer.
    /// </summary>
    [TestMethod]
    public void Ctor_WithSegmentSize_ShouldInitialiseEmptyBuffer()
    {
        var buffer = new SegmentedBuffer<int>(4);
        Assert.AreEqual(0, buffer.Count);
    }

    /// <summary>
    /// Verifies that the generic enumerator yields exactly the elements added when the count fills entire segments.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenBufferFillsSegmentsExactly_ShouldYieldAllInsertedElements()
    {
        var buffer = new SegmentedBuffer<int>(3);

        for (var i = 0; i < 6; i++)
            buffer.Add(i);

        var actual = buffer.ToArray();
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SegmentedBuffer{T}.GetEnumerator" /> yields no elements for an empty buffer.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenBufferIsEmpty_ShouldYieldNothing()
    {
        var buffer = new SegmentedBuffer<int>();
        using IEnumerator<int> enumerator = buffer.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the generic <see cref="SegmentedBuffer{T}.GetEnumerator" /> yields all stored elements in insertion order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenBufferSpansSegments_ShouldYieldInsertionOrderedElements()
    {
        var buffer = new SegmentedBuffer<int>(4);

        for (var i = 0; i < 11; i++)
            buffer.Add(i);

        var actual = new List<int>();
        foreach (var value in buffer)
            actual.Add(value);

        CollectionAssert.AreEqual(Enumerable.Range(0, 11).ToArray(), actual);
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="ArgumentOutOfRangeException" /> when the index exceeds the buffer count.
    /// </summary>
    [TestMethod]
    public void Indexer_Get_WhenIndexIsBeyondCount_ShouldThrowExactly()
    {
        var buffer = new SegmentedBuffer<int>(4);
        buffer.Add(0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = buffer[1];
        });
    }

    /// <summary>
    /// Verifies that the indexer getter throws <see cref="ArgumentOutOfRangeException" /> when the index is negative,
    /// rather than indexing a backing segment with a negative offset and surfacing an <see cref="IndexOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Indexer_Get_WhenIndexIsNegative_ShouldThrowExactly(int index)
    {
        var buffer = new SegmentedBuffer<int>(4);
        buffer.Add(0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = buffer[index];
        });
    }

    /// <summary>
    /// Verifies that the indexer setter throws <see cref="ArgumentOutOfRangeException" /> when the index is negative,
    /// rather than writing to a backing segment at a negative offset.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Indexer_Set_WhenIndexIsNegative_ShouldThrowExactly(int index)
    {
        var buffer = new SegmentedBuffer<int>(4);
        buffer.Add(0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            buffer[index] = 1;
        });
    }

    /// <summary>
    /// Verifies that the indexer setter throws <see cref="ArgumentOutOfRangeException" /> when the index equals
    /// <see cref="SegmentedBuffer{T}.Count" />, which is one position past the last element, rather than silently
    /// writing into an unused segment slot.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenIndexEqualsCount_ShouldThrowExactly()
    {
        var buffer = new SegmentedBuffer<int>(4);
        buffer.Add(0);
        buffer.Add(1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            buffer[buffer.Count] = 99;
        });
    }

    /// <summary>
    /// Verifies that the indexer setter throws <see cref="ArgumentOutOfRangeException" /> when the index exceeds the buffer count.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenIndexIsBeyondCount_ShouldThrowExactly()
    {
        var buffer = new SegmentedBuffer<int>(4);
        buffer.Add(0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            buffer[5] = 99;
        });
    }

    /// <summary>
    /// Verifies that the indexer returns each value in the order it was added, spanning multiple segments.
    /// </summary>
    [TestMethod]
    public void Indexer_Get_WhenSpanningSegments_ShouldReturnInsertionOrderedElements()
    {
        var buffer = new SegmentedBuffer<int>(3);

        for (var i = 0; i < 10; i++)
            buffer.Add(i * 11);

        for (var i = 0; i < 10; i++)
            Assert.AreEqual(i * 11, buffer[i], $"Element at index {i} did not match insertion-order expected value.");
    }

    /// <summary>
    /// Verifies that the indexer setter updates the value stored at the supplied index.
    /// </summary>
    [TestMethod]
    public void Indexer_Set_WhenIndexInRange_ShouldUpdateStoredValue()
    {
        var buffer = new SegmentedBuffer<int>(4);

        for (var i = 0; i < 7; i++)
            buffer.Add(i);

        buffer[3] = 42;

        Assert.AreEqual(42, buffer[3]);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> walks the same elements as the generic enumerator.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenBufferHasElements_ShouldYieldSameSequence()
    {
        var buffer = new SegmentedBuffer<int>(2);

        for (var i = 0; i < 5; i++)
            buffer.Add(i);

        IEnumerable nonGeneric = buffer;
        var actual = new List<int>();
        foreach (var item in nonGeneric)
            actual.Add((int)item);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, actual);
    }

}
