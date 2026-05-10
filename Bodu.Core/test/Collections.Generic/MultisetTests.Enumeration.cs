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

    // --------------------------------------------------------
    // Enumerator — Current before MoveNext
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that accessing <see cref="Multiset{T}.Enumerator.Current"/> before the first call to
    /// <see cref="Multiset{T}.Enumerator.MoveNext"/> throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1);
        Multiset<int>.Enumerator en = sut.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    // --------------------------------------------------------
    // Enumerator — Current after exhaustion
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that accessing <see cref="Multiset{T}.Enumerator.Current"/> after the enumerator is exhausted throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_AfterExhaustion_CurrentShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(42);
        Multiset<int>.Enumerator en = sut.GetEnumerator();
        while (en.MoveNext()) { }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    // --------------------------------------------------------
    // Enumerator — Reset
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Enumerator.Reset"/> repositions the enumerator to before the first element,
    /// allowing the multiset to be enumerated again from the beginning.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenReset_ShouldAllowReEnumeration()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1, 2);
        sut.Add(2, 1);

        Multiset<int>.Enumerator en = sut.GetEnumerator();
        List<int> firstPass = new List<int>();
        while (en.MoveNext())
            firstPass.Add(en.Current);

        en.Reset();

        List<int> secondPass = new List<int>();
        while (en.MoveNext())
            secondPass.Add(en.Current);

        firstPass.Sort();
        secondPass.Sort();
        CollectionAssert.AreEqual(firstPass, secondPass);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Enumerator.Reset"/> throws <see cref="InvalidOperationException"/>
    /// when the multiset has been modified after the enumerator was created.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetAfterModification_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>();
        sut.Add(1);
        Multiset<int>.Enumerator en = sut.GetEnumerator();
        sut.Add(2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            en.Reset();
        });
    }

    // --------------------------------------------------------
    // Enumerator — RemoveAll invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaRemoveAll_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int _ in sut)
                sut.RemoveAll(1);
        });
    }

    // --------------------------------------------------------
    // Enumerator — Add(T, int) invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.Add(T, int)"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaAddWithCount_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int _ in sut)
                sut.Add(99, 3);
        });
    }

    // --------------------------------------------------------
    // Distinct — empty multiset
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Distinct()"/> yields no elements when the multiset is empty.
    /// </summary>
    [TestMethod]
    public void Distinct_WhenEmpty_ShouldYieldNoElements()
    {
        Multiset<string> sut = new Multiset<string>();

        Assert.AreEqual(0, sut.Distinct().Count());
    }

    // --------------------------------------------------------
    // Frequencies — empty multiset
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Frequencies()"/> yields no pairs when the multiset is empty.
    /// </summary>
    [TestMethod]
    public void Frequencies_WhenEmpty_ShouldYieldNoPairs()
    {
        Multiset<string> sut = new Multiset<string>();

        Assert.AreEqual(0, sut.Frequencies().Count());
    }

    // --------------------------------------------------------
    // Distinct — RemoveAll invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Distinct()"/> throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Distinct_WhenModifiedViaRemoveAllDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (int _ in sut.Distinct())
                sut.RemoveAll(1);
        });
    }

    // --------------------------------------------------------
    // Frequencies — RemoveAll invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Frequencies()"/> throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Frequencies_WhenModifiedViaRemoveAllDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        Multiset<int> sut = new Multiset<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> _ in sut.Frequencies())
                sut.RemoveAll(1);
        });
    }
}
