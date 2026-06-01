// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonGenericCollectionContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic.Contracts;

/// <summary>
/// Reusable behavioural contract test base for the non-generic <see cref="ICollection" /> surface that
/// most BCL-compatible collections implement implicitly. Concrete subclasses override
/// <see cref="CreateEmpty" /> and <see cref="Create" /> to plug their type into the inherited tests.
/// </summary>
/// <typeparam name="TCollection">The collection type under test.</typeparam>
/// <remarks>
/// <para>
/// Covers <see cref="ICollection.Count" />, <see cref="ICollection.IsSynchronized" />,
/// <see cref="ICollection.SyncRoot" /> idempotence, and <see cref="ICollection.CopyTo" /> to a
/// <see cref="Array" /> destination. Skips concurrency-stress tests — those remain concrete on the
/// implementing type.
/// </para>
/// <para>
/// Must implement <see cref="ICollection" />.
/// </para>
/// </remarks>
public abstract class NonGenericCollectionContractTests<TCollection>
    where TCollection : ICollection
{
    /// <summary>
    /// Creates an empty collection of the type under test.
    /// </summary>
    /// <returns>A new empty collection instance.</returns>
    protected abstract TCollection CreateEmpty();

    /// <summary>
    /// Creates a representative non-empty collection of the type under test. The contents are opaque to
    /// the base; the inherited tests inspect <see cref="ICollection.Count" /> and <c>CopyTo</c> behaviour.
    /// </summary>
    /// <returns>A new non-empty collection instance.</returns>
    protected abstract TCollection Create();

    /// <summary>
    /// Verifies that an empty collection reports <see cref="ICollection.Count" /> of zero through the
    /// non-generic surface.
    /// </summary>
    [TestMethod]
    public void Count_WhenEmpty_ShouldBeZero()
    {
        TCollection collection = CreateEmpty();

        Assert.AreEqual(0, collection.Count);
    }

    /// <summary>
    /// Indicates whether the collection under test supports <see cref="ICollection.SyncRoot" />.
    /// Concurrent collections (for example <c>ConcurrentCircularBuffer</c>, <c>ConcurrentHashSet</c>)
    /// intentionally throw <see cref="NotSupportedException" /> from <c>SyncRoot</c> to prevent callers
    /// from taking the same lock used internally; override and return <see langword="false" /> in such
    /// subclasses to skip the idempotence assertion and instead verify that the property throws.
    /// </summary>
    protected virtual bool SyncRootSupported => true;

    /// <summary>
    /// Verifies that successive reads of <see cref="ICollection.SyncRoot" /> return the same instance,
    /// or — when <see cref="SyncRootSupported" /> is <see langword="false" /> — that the property
    /// throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void SyncRoot_WhenReadTwice_ShouldReturnSameInstance()
    {
        TCollection collection = CreateEmpty();

        if (!SyncRootSupported)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                _ = collection.SyncRoot;
            });
            return;
        }

        var syncRoot1 = collection.SyncRoot;
        var syncRoot2 = collection.SyncRoot;

        Assert.AreSame(syncRoot1, syncRoot2);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.IsSynchronized" /> returns a non-throwing value. The base does
    /// not prescribe <see langword="true" /> or <see langword="false" /> because both are valid contracts
    /// depending on the collection's threading model.
    /// </summary>
    [TestMethod]
    public void IsSynchronized_WhenAccessed_ShouldNotThrow()
    {
        TCollection collection = CreateEmpty();

        _ = collection.IsSynchronized;
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo" /> succeeds for a destination array of sufficient
    /// size and copies the same number of elements as <see cref="ICollection.Count" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationLargeEnough_ShouldNotThrow()
    {
        TCollection collection = Create();
        var destination = Array.CreateInstance(typeof(object), collection.Count + 4);

        collection.CopyTo(destination, 2);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection.CopyTo" /> throws <see cref="ArgumentNullException" /> for a
    /// null destination.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsNull_ShouldThrowArgumentNullException()
    {
        TCollection collection = Create();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            collection.CopyTo(null!, 0);
        });
    }
}
