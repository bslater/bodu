// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T>
{
    /// <summary>
    /// Represents a single entry in a bucket chain — an immutable element paired with its cached hash code and a
    /// volatile link to the next entry in the same bucket.
    /// </summary>
    private sealed class Node
    {
        /// <summary>The element stored in this node.</summary>
        internal readonly T _item;

        /// <summary>The cached hash code of <see cref="_item" />, retained so lookups and resizes never recompute it.</summary>
        internal readonly int _hashCode;

        /// <summary>The next node in the same bucket chain, or <see langword="null" /> at the end of the chain.</summary>
        /// <remarks>
        /// Declared <see langword="volatile" /> so that a lock-free reader walking the chain always observes a fully
        /// published successor rather than a torn or stale reference.
        /// </remarks>
        internal volatile Node? _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="item">The element stored in the node.</param>
        /// <param name="hashCode">The cached hash code of <paramref name="item" />.</param>
        /// <param name="next">The next node in the bucket chain, or <see langword="null" />.</param>
        internal Node(T item, int hashCode, Node? next)
        {
            _item = item;
            _hashCode = hashCode;
            _next = next;
        }
    }
}
