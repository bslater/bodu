// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadOnlyCollectionContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Reusable behavioural contract test base for types that expose the <see cref="IReadOnlyCollection{T}" />
/// surface but not <see cref="ICollection{T}" />. Concrete subclasses override <see cref="CreateEmpty" />
/// and <see cref="Create(TItem[])" /> to plug their type into the inherited contract tests.
/// </summary>
/// <typeparam name="TCollection">The collection type under test.</typeparam>
/// <typeparam name="TItem">The element type held by the collection.</typeparam>
public abstract class ReadOnlyCollectionContractTests<TCollection, TItem>
    where TCollection : IReadOnlyCollection<TItem>
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
    /// Returns a distinct item suitable for use at position <paramref name="index" />.
    /// </summary>
    /// <param name="index">The zero-based ordinal of the requested item.</param>
    /// <returns>A distinct item for the given ordinal.</returns>
    protected abstract TItem CreateItem(int index);

    /// <summary>
    /// Verifies that an empty collection reports <see cref="IReadOnlyCollection{T}.Count" /> of zero.
    /// </summary>
    [TestMethod]
    public void Count_WhenCollectionIsEmpty_ShouldBeZero()
    {
        TCollection collection = CreateEmpty();

        Assert.AreEqual(0, collection.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IReadOnlyCollection{T}.Count" /> reflects the number of seeded items.
    /// </summary>
    [TestMethod]
    public void Count_WhenSeededWithItems_ShouldReflectItemCount()
    {
        TItem[] items = [CreateItem(0), CreateItem(1), CreateItem(2)];
        TCollection collection = Create(items);

        Assert.AreEqual(items.Length, collection.Count);
    }

    /// <summary>
    /// Verifies that an empty collection yields no items when enumerated.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEmpty_ShouldYieldNoItems()
    {
        TCollection collection = CreateEmpty();

        var observed = 0;
        foreach (TItem _ in collection)
        {
            observed++;
        }

        Assert.AreEqual(0, observed);
    }

    /// <summary>
    /// Verifies that the enumerator yields a number of elements equal to
    /// <see cref="IReadOnlyCollection{T}.Count" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSeeded_ShouldYieldExpectedCount()
    {
        TItem[] source = [CreateItem(0), CreateItem(1), CreateItem(2)];
        TCollection collection = Create(source);

        var observed = 0;
        foreach (TItem _ in collection)
        {
            observed++;
        }

        Assert.AreEqual(collection.Count, observed);
    }
}
