// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for types that expose the <see cref="ICollection{T}" /> surface.
/// Concrete subclasses override <see cref="CreateEmpty" />, <see cref="Create(TItem[])" />, and
/// <see cref="CreateItem" /> to plug their type into the inherited <c>[TestMethod]</c>s; MSTest discovers
/// each inherited test as part of the concrete class.
/// </summary>
/// <typeparam name="TCollection">The collection type under test.</typeparam>
/// <typeparam name="TItem">The element type held by the collection.</typeparam>
/// <remarks>
/// <para>
/// The base focuses on the core <see cref="ICollection{T}" /> contract — count, contains, add, remove,
/// clear, copy-to, enumeration — and does not exercise type-specific or concurrency-specific behaviour.
/// Subclasses are free to add bespoke tests for behaviour outside the shared contract.
/// </para>
/// </remarks>
public abstract class CollectionContractTests<TCollection, TItem>
    where TCollection : ICollection<TItem>
{
    /// <summary>
    /// Creates an empty collection of the type under test.
    /// </summary>
    /// <returns>A new empty collection instance.</returns>
    protected abstract TCollection CreateEmpty();

    /// <summary>
    /// Creates a collection of the type under test seeded with <paramref name="items" /> in the order
    /// supplied.
    /// </summary>
    /// <param name="items">The items to seed the collection with.</param>
    /// <returns>A new collection instance containing <paramref name="items" />.</returns>
    protected abstract TCollection Create(params TItem[] items);

    /// <summary>
    /// Returns a distinct item suitable for use at position <paramref name="index" />. Implementations
    /// must return a value whose equality / hash semantics match what <see cref="ICollection{T}.Contains" />
    /// expects for this collection.
    /// </summary>
    /// <param name="index">The zero-based ordinal of the requested item.</param>
    /// <returns>A distinct item for the given ordinal.</returns>
    protected abstract TItem CreateItem(int index);

    /// <summary>
    /// Verifies that an empty collection reports <see cref="ICollection{T}.Count" /> of zero.
    /// </summary>
    [TestMethod]
    public void Count_WhenCollectionIsEmpty_ShouldBeZero()
    {
        TCollection collection = CreateEmpty();

        Assert.AreEqual(0, collection.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Count" /> reflects the number of seeded items.
    /// </summary>
    [TestMethod]
    public void Count_WhenSeededWithItems_ShouldReflectItemCount()
    {
        TItem[] items = [CreateItem(0), CreateItem(1), CreateItem(2)];
        TCollection collection = Create(items);

        Assert.AreEqual(items.Length, collection.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Add" /> increases <see cref="ICollection{T}.Count" /> by one.
    /// </summary>
    [TestMethod]
    public void Add_WhenCalled_ShouldIncreaseCountByOne()
    {
        TCollection collection = CreateEmpty();

        collection.Add(CreateItem(0));

        Assert.AreEqual(1, collection.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Contains" /> returns <see langword="true" /> for an item
    /// that was added to the collection.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemWasAdded_ShouldReturnTrue()
    {
        TItem item = CreateItem(0);
        TCollection collection = Create(item);

        Assert.IsTrue(collection.Contains(item));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Contains" /> returns <see langword="false" /> for an item
    /// that is not present in the collection.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsAbsent_ShouldReturnFalse()
    {
        TItem present = CreateItem(0);
        TItem absent = CreateItem(1);
        TCollection collection = Create(present);

        Assert.IsFalse(collection.Contains(absent));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Remove" /> returns <see langword="true" />, removes the item,
    /// and decreases the count by one when the item is present.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsPresent_ShouldReturnTrueAndDecreaseCount()
    {
        TItem item = CreateItem(0);
        TCollection collection = Create(item);

        bool removed = collection.Remove(item);

        Assert.IsTrue(removed);
        Assert.AreEqual(0, collection.Count);
        Assert.IsFalse(collection.Contains(item));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Remove" /> returns <see langword="false" /> and leaves the
    /// collection unchanged when the item is absent.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsAbsent_ShouldReturnFalse()
    {
        TItem present = CreateItem(0);
        TItem absent = CreateItem(1);
        TCollection collection = Create(present);

        bool removed = collection.Remove(absent);

        Assert.IsFalse(removed);
        Assert.AreEqual(1, collection.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Clear" /> empties the collection.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCollectionIsNonEmpty_ShouldResetCountToZero()
    {
        TCollection collection = Create(CreateItem(0), CreateItem(1), CreateItem(2));

        collection.Clear();

        Assert.AreEqual(0, collection.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.CopyTo" /> copies every item into a destination array of
    /// sufficient size starting at the supplied index.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsLargeEnough_ShouldCopyAllItems()
    {
        TItem[] source = [CreateItem(0), CreateItem(1), CreateItem(2)];
        TCollection collection = Create(source);
        TItem[] destination = new TItem[source.Length + 2];

        collection.CopyTo(destination, 1);

        for (int i = 0; i < source.Length; i++)
        {
            Assert.IsTrue(collection.Contains(destination[i + 1]));
        }
    }

    /// <summary>
    /// Verifies that the enumerator yields a number of elements equal to <see cref="ICollection{T}.Count" />.
    /// Element identity is verified via <see cref="ICollection{T}.Contains" /> to remain agnostic of
    /// iteration order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSeeded_ShouldYieldEveryItem()
    {
        TItem[] source = [CreateItem(0), CreateItem(1), CreateItem(2)];
        TCollection collection = Create(source);

        int observed = 0;
        foreach (TItem item in collection)
        {
            Assert.IsTrue(collection.Contains(item));
            observed++;
        }

        Assert.AreEqual(collection.Count, observed);
    }
}
