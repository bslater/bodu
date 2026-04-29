// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DoubleEndedRingCollectionTestsBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides the shared test contract for ring-backed collections that expose a full double-ended interface
/// (head-side <c>AddFirst</c> / <c>RemoveFirst</c> / <c>PeekFirst</c> in addition to tail-side operations).
/// Inherited by <see cref="DequeTests"/> (growable mode) and <see cref="DequeFixedCapacityTests"/> (fixed-capacity mode).
/// </summary>
/// <typeparam name="TTest">The concrete test class (CRTP self-reference).</typeparam>
/// <typeparam name="TCollection">The collection type under test.</typeparam>
/// <remarks>
/// Extends <see cref="RingBackedCollectionTestsBase{TTest, TCollection}"/> with hooks for the head-side and
/// tail-side double-ended operations. The Tier 1 contract tests already cover head-side reads
/// (<c>PeekHead</c> / <c>RemoveFromHead</c>) which map naturally to the deque <c>PeekFirst</c> /
/// <c>RemoveFirst</c>. This Tier 2 base adds the symmetric tail-side reads and the head-side adds.
/// </remarks>
public abstract partial class DoubleEndedRingCollectionTestsBase<TTest, TCollection>
    : RingBackedCollectionTestsBase<TTest, TCollection>
    where TTest : DoubleEndedRingCollectionTestsBase<TTest, TCollection>, new()
    where TCollection : class, IEnumerable<int>, ICollection
{
    /// <summary>Adds an item at the head end of the collection.</summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <param name="item">The item to push to the head.</param>
    protected abstract void AddToHead(TCollection collection, int item);

    /// <summary>Attempts to add an item at the head end without throwing when full.</summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <param name="item">The item to push to the head.</param>
    /// <returns><see langword="true"/> if the item was added.</returns>
    protected abstract bool TryAddToHead(TCollection collection, int item);

    /// <summary>Removes and returns the tail element. Throws when empty.</summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <returns>The removed tail element.</returns>
    protected abstract int RemoveFromTail(TCollection collection);

    /// <summary>Attempts to remove and return the tail element without throwing when empty.</summary>
    /// <param name="collection">The collection to mutate.</param>
    /// <param name="item">When this method returns, contains the removed item if successful.</param>
    /// <returns><see langword="true"/> if an element was removed.</returns>
    protected abstract bool TryRemoveFromTail(TCollection collection, out int item);

    /// <summary>Returns the tail element without removing it. Throws when empty.</summary>
    /// <param name="collection">The collection to inspect.</param>
    /// <returns>The current tail element.</returns>
    protected abstract int PeekTail(TCollection collection);

    /// <summary>Attempts to read the tail element without throwing when empty.</summary>
    /// <param name="collection">The collection to inspect.</param>
    /// <param name="item">When this method returns, contains the tail element if available.</param>
    /// <returns><see langword="true"/> if the tail was read.</returns>
    protected abstract bool TryPeekTail(TCollection collection, out int item);
}
