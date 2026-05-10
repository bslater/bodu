// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.Enumeration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{
    // --------------------------------------------------------
    // GetEnumerator / foreach
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that enumerating the multiset yields each element as many times as its occurrence count.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEnumerated_ShouldYieldElementsWithMultiplicity()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1, 3);
        sut.Add(2, 2);

        List<int> result = sut.ToList();
        result.Sort();

        CollectionAssert.AreEqual(new[] { 1, 1, 1, 2, 2 }, result);
    }

    /// <summary>
    /// Verifies that enumerating an empty multiset produces no elements.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEmpty_ShouldProduceNoElements()
    {
        Multiset<string> sut = new Multiset<string>();

        Assert.AreEqual(0, sut.Count());
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified during enumeration via Add.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaAdd_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int _ in sut)
                sut.Add(99);
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified during enumeration via Remove.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaRemove_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int _ in sut)
                sut.Remove(1);
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is cleared during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int _ in sut)
                sut.Clear();
        });
    }

    // --------------------------------------------------------
    // Distinct()
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Distinct()"/> yields each element exactly once.
    /// </summary>
    [TestMethod]
    public void Distinct_WhenCalled_ShouldYieldEachDistinctElementOnce()
    {
        Multiset<string> sut = new Multiset<string>(["a", "a", "b", "c", "c", "c"]);

        List<string> distinct = sut.Distinct().OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, distinct);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Distinct()"/> throws <see cref="InvalidOperationException"/> when modified during enumeration.
    /// </summary>
    [TestMethod]
    public void Distinct_WhenModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int _ in sut.Distinct())
                sut.Add(99);
        });
    }

    // --------------------------------------------------------
    // Frequencies()
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Frequencies()"/> returns correct element–count pairs.
    /// </summary>
    [TestMethod]
    public void Frequencies_WhenCalled_ShouldReturnCorrectPairs()
    {
        Multiset<string> sut = new Multiset<string>(["a", "a", "b"]);

        Dictionary<string, int> freqs = sut.Frequencies().ToDictionary(p => p.Key, p => p.Value);

        Assert.AreEqual(2, freqs["a"]);
        Assert.AreEqual(1, freqs["b"]);
        Assert.AreEqual(2, freqs.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Frequencies()"/> throws <see cref="InvalidOperationException"/> when modified during enumeration.
    /// </summary>
    [TestMethod]
    public void Frequencies_WhenModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> _ in sut.Frequencies())
                sut.Add(99);
        });
    }

    // --------------------------------------------------------
    // ICollection explicit (non-generic)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the non-generic <c>ICollection.CopyTo</c> copies all elements with multiplicity into an object array.
    /// </summary>
    [TestMethod]
    public void ICollectionCopyTo_WhenCalled_ShouldCopyAllElementsWithMultiplicity()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(5, 2);
        sut.Add(6, 1);

        object[] dest = new object[3];
        ((System.Collections.ICollection)sut).CopyTo(dest, 0);

        int[] values = dest.Cast<int>().OrderBy(x => x).ToArray();
        CollectionAssert.AreEqual(new[] { 5, 5, 6 }, values);
    }
}
