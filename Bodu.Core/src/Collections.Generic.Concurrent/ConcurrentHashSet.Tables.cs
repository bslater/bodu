// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet.Tables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T>
{
    /// <summary>
    /// Represents one generation of the bucket table: the bucket array paired with the per-lock element counters.
    /// </summary>
    /// <remarks>
    /// A new <see cref="Tables" /> instance is published atomically whenever the table grows or is cleared. Treating
    /// the bucket array and its counters as a single immutable unit lets a writer that has read the table verify, under
    /// its stripe lock, that no resize has intervened simply by comparing references.
    /// </remarks>
    private sealed class Tables
    {
        /// <summary>
        /// The bucket array. Each slot holds the head of a singly linked <see cref="Node" /> chain, or
        /// <see langword="null" /> when the bucket is empty.
        /// </summary>
        internal readonly Node?[] _buckets;

        /// <summary>
        /// The number of elements owned by each stripe lock, indexed by lock number.
        /// </summary>
        /// <remarks>
        /// Each counter is mutated only while the corresponding stripe lock is held. Summing every counter while all
        /// locks are held yields an exact element count.
        /// </remarks>
        internal readonly int[] _countPerLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tables" /> class.
        /// </summary>
        /// <param name="buckets">The bucket array for this generation.</param>
        /// <param name="countPerLock">The per-lock element counters for this generation.</param>
        internal Tables(Node?[] buckets, int[] countPerLock)
        {
            _buckets = buckets;
            _countPerLock = countPerLock;
        }
    }
}
