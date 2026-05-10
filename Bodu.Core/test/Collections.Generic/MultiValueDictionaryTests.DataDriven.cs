// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.DataDriven.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    // --------------------------------------------------------
    // Add — parameterised value counts per key
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Add"/> correctly accumulates any number
    /// of values under a single key, across a range of value counts.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(10)]
    [DataRow(100)]
    public void Add_WhenValuesAccumulatedForOneKey_ShouldTrackCountExactly(int valueCount)
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        for (int i = 0; i < valueCount; i++)
            sut.Add("k", i);

        Assert.AreEqual(valueCount, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
        Assert.AreEqual(valueCount, sut["k"].Count);
    }

    // --------------------------------------------------------
    // RemoveAll — parameterised value counts
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> removes all values for a
    /// key regardless of how many values were stored, across a range of value counts.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(10)]
    public void RemoveAll_WhenKeyHasVariousValueCounts_ShouldRemoveAllAndUpdateCount(int valueCount)
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        for (int i = 0; i < valueCount; i++)
            sut.Add("k", i);
        sut.Add("other", 99);

        bool result = sut.RemoveAll("k");

        Assert.IsTrue(result);
        Assert.AreEqual(1, sut.Count);
        Assert.AreEqual(1, sut.KeyCount);
        Assert.IsFalse(sut.ContainsKey("k"));
    }

    // --------------------------------------------------------
    // AddRange — parameterised value counts
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.AddRange"/> appends all elements in the
    /// supplied sequence in insertion order, for a range of sequence lengths.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(10)]
    [DataRow(50)]
    public void AddRange_WhenSequenceHasVaryingLength_ShouldAppendAllInOrder(int length)
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        int[] values = Enumerable.Range(0, length).ToArray();

        sut.AddRange("k", values);

        Assert.AreEqual(length, sut.Count);
        CollectionAssert.AreEqual(values, sut["k"].ToList());
    }

    // --------------------------------------------------------
    // Remove — value at various positions in the list
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> can remove the
    /// value at any position (by value index) within a key's list, for a range of list sizes.
    /// </summary>
    [TestMethod]
    [DataRow(3, 0)]
    [DataRow(3, 1)]
    [DataRow(3, 2)]
    [DataRow(5, 0)]
    [DataRow(5, 4)]
    public void Remove_WhenValueIsAtVariousPositions_ShouldRemoveExactlyOne(int listSize, int positionToRemove)
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        for (int i = 0; i < listSize; i++)
            sut.Add("k", i * 10);

        int valueToRemove = positionToRemove * 10;
        bool result = sut.Remove("k", valueToRemove);

        Assert.IsTrue(result);
        Assert.AreEqual(listSize - 1, sut.Count);
        Assert.IsFalse(sut["k"].Contains(valueToRemove));
    }

    // --------------------------------------------------------
    // GetValues — insertion-order guarantee
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> always returns values in
    /// insertion order, for a range of value counts.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(20)]
    public void GetValues_WhenRetrieved_ShouldPreserveInsertionOrder(int valueCount)
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        int[] expected = Enumerable.Range(0, valueCount).Select(i => i * 7).ToArray();
        foreach (int v in expected)
            sut.Add("k", v);

        System.Collections.Generic.IReadOnlyList<int> actual = sut.GetValues("k");

        CollectionAssert.AreEqual(expected, actual.ToList());
    }
}
