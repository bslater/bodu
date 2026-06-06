// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.InterfaceContracts.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that the explicit <see cref="ICollection{T}.Add(T)" /> implementation stores the element.
    /// </summary>
    [TestMethod]
    public void ICollectionT_Add_ShouldStoreElementThroughInterface()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();

        set.Add(7);

        Assert.HasCount(1, set);
        Assert.Contains(7, set);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.IsReadOnly" /> reports <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ICollectionT_IsReadOnly_ShouldBeFalse()
    {
        ICollection<int> set = new ConcurrentHashSet<int>();

        Assert.IsFalse(set.IsReadOnly);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Contains" />, <see cref="ICollection{T}.Remove" />,
    /// <see cref="ICollection{T}.Count" />, and <see cref="ICollection{T}.Clear" /> behave correctly through the
    /// interface.
    /// </summary>
    [TestMethod]
    public void ICollectionT_ContainsRemoveClear_ShouldBehaveThroughInterface()
    {
        ICollection<int> set = new ConcurrentHashSet<int>([1, 2, 3]);

        Assert.Contains(2, set);
        Assert.IsTrue(set.Remove(2));
        Assert.DoesNotContain(2, set);
        Assert.HasCount(2, set);

        set.Clear();
        Assert.IsEmpty(set);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.CopyTo" /> copies every element through the interface.
    /// </summary>
    [TestMethod]
    public void ICollectionT_CopyTo_ShouldCopyEveryElementThroughInterface()
    {
        ICollection<int> set = new ConcurrentHashSet<int>([1, 2, 3]);
        var array = new int[3];

        set.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that the <see cref="ISet{T}.Add(T)" /> implementation reports whether the element was newly added when
    /// invoked through an <see cref="ISet{T}" /> reference.
    /// </summary>
    [TestMethod]
    public void ISet_Add_ShouldReportWhetherElementWasAddedThroughInterface()
    {
        ISet<int> set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.Add(1));
        Assert.IsFalse(set.Add(1));
    }

    /// <summary>
    /// Verifies that the bulk <see cref="ISet{T}" /> operations are reachable and correct through an
    /// <see cref="ISet{T}" />-typed reference.
    /// </summary>
    [TestMethod]
    public void ISet_BulkOperations_ShouldBehaveThroughInterface()
    {
        ISet<int> set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.UnionWith([3, 4]);
        set.ExceptWith([1]);

        Assert.IsTrue(set.SetEquals([2, 3, 4]));
        Assert.IsTrue(set.IsSupersetOf([2, 4]));
        Assert.IsTrue(set.Overlaps([4, 99]));
    }

    /// <summary>
    /// Verifies that <see cref="IReadOnlyCollection{T}.Count" /> and enumeration work through an
    /// <see cref="IReadOnlyCollection{T}" /> reference.
    /// </summary>
    [TestMethod]
    public void IReadOnlyCollection_ShouldExposeCountAndElementsThroughInterface()
    {
        IReadOnlyCollection<int> set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        Assert.HasCount(4, set);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, set.ToList());
    }

    /// <summary>
    /// Verifies that the set round-trips through the <see cref="ICollection{T}" /> contract when consumed by a
    /// collection-copy constructor.
    /// </summary>
    [TestMethod]
    public void ICollectionT_WhenConsumedAsCollectionContract_ShouldRoundTrip()
    {
        ICollection<int> source = new ConcurrentHashSet<int>([5, 6, 7]);

        var copy = new List<int>(source);

        CollectionAssert.AreEquivalent(new[] { 5, 6, 7 }, copy);
    }
}
