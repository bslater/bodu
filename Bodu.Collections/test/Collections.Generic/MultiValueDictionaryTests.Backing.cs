// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Backing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    /// <summary>
    /// Verifies that the default constructor reports <see cref="MultiValueBacking.List" /> backing.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldReportListBacking()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.AreEqual(MultiValueBacking.List, mvd.Backing);
    }

    /// <summary>
    /// Verifies that constructing with <see cref="MultiValueBacking.Set" /> reports set backing.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBackingIsSet_ShouldReportSetBacking()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set);

        Assert.AreEqual(MultiValueBacking.Set, mvd.Backing);
    }

    /// <summary>
    /// Verifies that a null value comparer defaults <see cref="MultiValueDictionary{TKey, TValue}.ValueComparer" /> to
    /// the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueComparerIsNull_ShouldUseDefaultValueComparer()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set, null, null);

        Assert.AreEqual(EqualityComparer<int>.Default, mvd.ValueComparer);
    }

    /// <summary>
    /// Verifies that a custom value comparer is reported by
    /// <see cref="MultiValueDictionary{TKey, TValue}.ValueComparer" /> under set backing.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueComparerIsProvided_ForSetBacking_ShouldReportSpecifiedValueComparer()
    {
        var mvd = new MultiValueDictionary<string, string>(MultiValueBacking.Set, null, StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, mvd.ValueComparer);
    }

    /// <summary>
    /// Verifies that a custom value comparer is stored and reported under list backing even though it is not consulted
    /// by value operations.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueComparerIsProvided_ForListBacking_ShouldReportSpecifiedValueComparer()
    {
        var mvd = new MultiValueDictionary<string, string>(MultiValueBacking.List, null, StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, mvd.ValueComparer);
    }

    /// <summary>
    /// Verifies that the backing constructor overloads compose with a custom key comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBackingAndKeyComparerAreProvided_ShouldUseSpecifiedKeyComparer()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set, StringComparer.OrdinalIgnoreCase);

        mvd.Add("KEY", 1);
        mvd.Add("key", 1);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, mvd.Comparer);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(1, mvd.Count);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="MultiValueBacking" /> value passed to the single-argument constructor
    /// throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBackingIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MultiValueDictionary<string, int>((MultiValueBacking)999);
        });

        Assert.AreEqual("backing", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="MultiValueBacking" /> value passed to the backing-and-key-comparer
    /// constructor throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBackingIsUndefined_ForKeyComparerOverload_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MultiValueDictionary<string, int>((MultiValueBacking)999, StringComparer.Ordinal);
        });

        Assert.AreEqual("backing", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="MultiValueBacking" /> value passed to the full constructor throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBackingIsUndefined_ForValueComparerOverload_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MultiValueDictionary<string, string>((MultiValueBacking)999, StringComparer.Ordinal, StringComparer.Ordinal);
        });

        Assert.AreEqual("backing", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding a duplicate value under set backing leaves the counts and the value view unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenDuplicateValueUnderSetBacking_ShouldIgnoreDuplicate()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set);

        mvd.Add("key", 1);
        mvd.Add("key", 1);

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        CollectionAssert.AreEqual(new[] { 1 }, mvd["key"].ToArray());
    }

    /// <summary>
    /// Verifies that a suppressed duplicate add under set backing is not a structural change and does not invalidate
    /// active enumerators.
    /// </summary>
    [TestMethod]
    public void Add_WhenDuplicateValueUnderSetBacking_ShouldNotInvalidateEnumerators()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set);

        mvd.Add("key", 1);

        using var enumerator = mvd.GetEnumerator();

        mvd.Add("key", 1);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("key", enumerator.Current.Key);
    }

    /// <summary>
    /// Verifies that adding a duplicate value under list backing retains both occurrences, preserving the historical
    /// default behavior.
    /// </summary>
    [TestMethod]
    public void Add_WhenDuplicateValueUnderListBacking_ShouldRetainDuplicate()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.Add("key", 1);
        mvd.Add("key", 1);

        Assert.AreEqual(2, mvd.Count);
        CollectionAssert.AreEqual(new[] { 1, 1 }, mvd["key"].ToArray());
    }

    /// <summary>
    /// Verifies that a custom value comparer drives duplicate suppression under set backing, keeping the first
    /// occurrence's casing.
    /// </summary>
    [TestMethod]
    public void Add_WhenValuesDifferOnlyByCaseUnderSetBacking_ShouldKeepFirstOccurrence()
    {
        var mvd = new MultiValueDictionary<string, string>(MultiValueBacking.Set, null, StringComparer.OrdinalIgnoreCase);

        mvd.Add("key", "Apple");
        mvd.Add("key", "APPLE");
        mvd.Add("key", "apple");

        Assert.AreEqual(1, mvd.Count);
        CollectionAssert.AreEqual(new[] { "Apple" }, mvd["key"].ToArray());
    }

    /// <summary>
    /// Verifies that distinct values added under set backing are kept in insertion order.
    /// </summary>
    [TestMethod]
    public void Add_WhenDistinctValuesUnderSetBacking_ShouldPreserveInsertionOrder()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set);

        mvd.Add("key", 3);
        mvd.Add("key", 1);
        mvd.Add("key", 2);
        mvd.Add("key", 1);

        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, mvd["key"].ToArray());
    }

    /// <summary>
    /// Verifies that a range added under set backing skips values already present and collapses duplicates within the
    /// source to their first occurrence.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenSourceContainsDuplicatesUnderSetBacking_ShouldAddFirstOccurrencesOnly()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set);

        mvd.Add("key", 1);
        mvd.AddRange("key", new[] { 1, 2, 2, 3, 1 });

        Assert.AreEqual(3, mvd.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, mvd["key"].ToArray());
    }

    /// <summary>
    /// Verifies that a range consisting entirely of duplicates under set backing is a no-op that does not invalidate
    /// active enumerators.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenAllValuesAreDuplicatesUnderSetBacking_ShouldNotInvalidateEnumerators()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set);

        mvd.Add("key", 1);
        mvd.Add("key", 2);

        using var enumerator = mvd.GetEnumerator();

        mvd.AddRange("key", new[] { 1, 2, 1 });

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(2, mvd.Count);
    }

    /// <summary>
    /// Verifies that a range added under list backing retains every value, including duplicates, preserving the
    /// historical default behavior.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenSourceContainsDuplicatesUnderListBacking_ShouldRetainAllValues()
    {
        var mvd = new MultiValueDictionary<string, int>();

        mvd.Add("key", 1);
        mvd.AddRange("key", new[] { 1, 2, 2 });

        Assert.AreEqual(4, mvd.Count);
        CollectionAssert.AreEqual(new[] { 1, 1, 2, 2 }, mvd["key"].ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.Remove" /> matches values using the value comparer
    /// under set backing.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueMatchesByValueComparerUnderSetBacking_ShouldRemoveValue()
    {
        var mvd = new MultiValueDictionary<string, string>(MultiValueBacking.Set, null, StringComparer.OrdinalIgnoreCase);

        mvd.Add("key", "apple");
        mvd.Add("key", "banana");

        Assert.IsTrue(mvd.Remove("key", "APPLE"));
        Assert.AreEqual(1, mvd.Count);
        CollectionAssert.AreEqual(new[] { "banana" }, mvd["key"].ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.Remove" /> ignores the stored value comparer under
    /// list backing and uses default equality.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueDiffersByCaseUnderListBacking_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, string>(MultiValueBacking.List, null, StringComparer.OrdinalIgnoreCase);

        mvd.Add("key", "apple");

        Assert.IsFalse(mvd.Remove("key", "APPLE"));
        Assert.AreEqual(1, mvd.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.ContainsValue" /> matches values using the value
    /// comparer under set backing.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueMatchesByValueComparerUnderSetBacking_ShouldReturnTrue()
    {
        var mvd = new MultiValueDictionary<string, string>(MultiValueBacking.Set, null, StringComparer.OrdinalIgnoreCase);

        mvd.Add("key", "apple");

        Assert.IsTrue(mvd.ContainsValue("key", "APPLE"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.ContainsValue" /> ignores the stored value comparer
    /// under list backing and uses default equality.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueDiffersByCaseUnderListBacking_ShouldReturnFalse()
    {
        var mvd = new MultiValueDictionary<string, string>(MultiValueBacking.List, null, StringComparer.OrdinalIgnoreCase);

        mvd.Add("key", "apple");

        Assert.IsFalse(mvd.ContainsValue("key", "APPLE"));
    }

    /// <summary>
    /// Verifies that value views handed out under set backing are live, deduplicated, ordered, and remain read-only.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenDuplicatesSuppressedUnderSetBacking_ShouldReflectDeduplicatedLiveView()
    {
        var mvd = new MultiValueDictionary<string, int>(MultiValueBacking.Set);

        mvd.Add("key", 1);

        IReadOnlyList<int> view = mvd.GetValues("key");

        mvd.Add("key", 1);
        mvd.Add("key", 2);

        CollectionAssert.AreEqual(new[] { 1, 2 }, view.ToArray());
        AssertReadOnlyValueViewCannotBeMutated(view);
    }
}
