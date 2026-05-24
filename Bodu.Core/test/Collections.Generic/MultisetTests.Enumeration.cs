// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.Enumeration.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{

    // --------------------------------------------------------
    // Distinct()
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Distinct()"/> yields each element exactly once.
    /// </summary>
    [TestMethod]
    public void Distinct_WhenCalled_ShouldYieldEachDistinctElementOnce()
    {
        var mvd = new Multiset<string>(["a", "a", "b", "c", "c", "c"]);

        var distinct = mvd.Distinct().OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, distinct);
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
        var mvd = new Multiset<string>();

        Assert.IsEmpty(mvd.Distinct());
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Distinct()"/> throws <see cref="InvalidOperationException"/> when modified during enumeration.
    /// </summary>
    [TestMethod]
    public void Distinct_WhenModifiedDuringEnumeration_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd.Distinct())
                mvd.Add(99);
        });
    }

    // --------------------------------------------------------
    // Distinct — RemoveAll invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Distinct()"/> throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Distinct_WhenModifiedViaRemoveAllDuringEnumeration_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd.Distinct())
                mvd.RemoveAll(1);
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
        var mvd = new Multiset<int>();
        mvd.Add(42);
        Multiset<int>.Enumerator en = mvd.GetEnumerator();
        while (en.MoveNext()) { }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    // --------------------------------------------------------
    // Enumerator — Current before MoveNext
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that accessing <see cref="Multiset{T}.Enumerator.Current"/> before the first call to
    /// <see cref="Multiset{T}.Enumerator.MoveNext"/> throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1);
        Multiset<int>.Enumerator en = mvd.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is cleared during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenClearedDuringEnumeration_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd)
                mvd.Clear();
        });
    }

    /// <summary>
    /// Verifies that enumerating an empty multiset produces no elements.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEmpty_ShouldProduceNoElements()
    {
        var mvd = new Multiset<string>();

        Assert.IsEmpty(mvd);
    }
    // --------------------------------------------------------
    // GetEnumerator / foreach
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that enumerating the multiset yields each element as many times as its occurrence count.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEnumerated_ShouldYieldElementsWithMultiplicity()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1, 3);
        mvd.Add(2, 2);

        var result = mvd.ToList();
        result.Sort();

        CollectionAssert.AreEqual(new[] { 1, 1, 1, 2, 2 }, result);
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified during enumeration via Add.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaAdd_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd)
                mvd.Add(99);
        });
    }

    // --------------------------------------------------------
    // Enumerator — Add(T, int) invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.Add(T, int)"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaAddWithCount_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd)
                mvd.Add(99, 3);
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified during enumeration via Remove.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaRemove_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd)
                mvd.Remove(1);
        });
    }

    // --------------------------------------------------------
    // Enumerator — RemoveAll invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumerationViaRemoveAll_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (var _ in mvd)
                mvd.RemoveAll(1);
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
        var mvd = new Multiset<int>();
        mvd.Add(1, 2);
        mvd.Add(2, 1);

        Multiset<int>.Enumerator en = mvd.GetEnumerator();
        var firstPass = new List<int>();
        while (en.MoveNext())
            firstPass.Add(en.Current);

        en.Reset();

        var secondPass = new List<int>();
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
    public void Enumerator_WhenResetAfterModification_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>();
        mvd.Add(1);
        Multiset<int>.Enumerator en = mvd.GetEnumerator();
        mvd.Add(2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            en.Reset();
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
        var mvd = new Multiset<string>(["a", "a", "b"]);

        var freqs = mvd.Frequencies().ToDictionary(p => p.Key, p => p.Value);

        Assert.AreEqual(2, freqs["a"]);
        Assert.AreEqual(1, freqs["b"]);
        Assert.HasCount(2, freqs);
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
        var mvd = new Multiset<string>();

        Assert.IsEmpty(mvd.Frequencies());
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Frequencies()"/> throws <see cref="InvalidOperationException"/> when modified during enumeration.
    /// </summary>
    [TestMethod]
    public void Frequencies_WhenModifiedDuringEnumeration_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> _ in mvd.Frequencies())
                mvd.Add(99);
        });
    }

    // --------------------------------------------------------
    // Frequencies — RemoveAll invalidation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Frequencies()"/> throws <see cref="InvalidOperationException"/> when the multiset is modified via <see cref="Multiset{T}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Frequencies_WhenModifiedViaRemoveAllDuringEnumeration_ShouldThrowExactly()
    {
        var mvd = new Multiset<int>([1, 2, 3]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<int, int> _ in mvd.Frequencies())
                mvd.RemoveAll(1);
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
        var mvd = new Multiset<int>();
        mvd.Add(5, 2);
        mvd.Add(6, 1);

        var dest = new object[3];
        ((System.Collections.ICollection)mvd).CopyTo(dest, 0);

        var values = dest.Cast<int>().OrderBy(x => x).ToArray();
        CollectionAssert.AreEqual(new[] { 5, 5, 6 }, values);
    }

}
