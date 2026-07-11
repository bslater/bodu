// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSet{T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentHashSet<T>
{
    /// <summary>
    /// Represents one node of the lock-free split-ordered list: a data node carrying an element, a bucket sentinel, or
    /// a deletion marker.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The list is a Harris–Michael lock-free ordered linked list. <see cref="_next" /> is the only mutable field and
    /// is accessed exclusively through <see cref="Volatile" /> reads and
    /// <see cref="Interlocked.CompareExchange{T}(ref T, T, T)" />.
    /// </para>
    /// <para>
    /// A node is <b>logically deleted</b> by installing a <b>marker node</b> (<see cref="_isMarker" /> is
    /// <see langword="true" />) into its <see cref="_next" /> field via CAS. The marker's own <see cref="_next" />
    /// holds the deleted node's real successor, fixed at marking time and never mutated afterwards. Folding the mark
    /// into <see cref="_next" /> makes the (mark, successor) pair change atomically: once a node is marked, any
    /// competing CAS on its <see cref="_next" /> — an insert-after or a second delete — necessarily fails. This is the
    /// managed-runtime port of Harris's pointer tag bit (the same scheme used by Java's <c>ConcurrentSkipListMap</c>).
    /// </para>
    /// <para>
    /// Physical unlinking (swinging the predecessor's <see cref="_next" /> past the marked node and its marker) is
    /// performed best-effort by the deleter and cooperatively by any traversal that encounters the marker. Because
    /// every insertion allocates a fresh node, a CAS comparand reference can never recur with a different meaning, so
    /// the ABA problem cannot arise; the garbage collector keeps an unlinked node alive for any traversal still
    /// positioned on it, and that node's <see cref="_next" /> chain still leads back into the live list.
    /// </para>
    /// <para>
    /// Bucket <b>sentinels</b> carry an even split-order key and no element; they are never marked or removed, which is
    /// what keeps the bucket shortcut array permanently valid. Markers carry no key of their own (their
    /// <see cref="_key" /> is unused) and never appear as list members in their own right — they are only ever
    /// reachable through the <see cref="_next" /> field of the node they mark.
    /// </para>
    /// </remarks>
    private sealed class Node
    {
        /// <summary>The split-order key: <c>(ReverseBits(hash) &lt;&lt; 1) | 1</c> for data nodes, <c>ReverseBits(bucket) &lt;&lt; 1</c> for sentinels. Unused (zero) for markers.</summary>
        internal readonly ulong _key;

        /// <summary>The element stored in a data node; the type's default value for sentinels and markers.</summary>
        internal readonly T _item;

        /// <summary>Indicates whether this node is a deletion marker rather than a list member.</summary>
        internal readonly bool _isMarker;

        /// <summary>The next node in split-order, or <see langword="null" /> at the end of the list.</summary>
        /// <remarks>
        /// The only mutable field. Read via <see cref="Volatile" /> and written only via
        /// <see cref="Interlocked.CompareExchange{T}(ref T, T, T)" />, except during construction. When this field
        /// references a marker, the owning node is logically deleted and the marker's <see cref="_next" /> is the real
        /// successor.
        /// </remarks>
        internal Node? _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="key">The split-order key of the node.</param>
        /// <param name="item">The element stored in the node, or the default value for sentinels and markers.</param>
        /// <param name="isMarker">Whether the node is a deletion marker.</param>
        /// <param name="next">The initial successor, or <see langword="null" />.</param>
        private Node(ulong key, T item, bool isMarker, Node? next)
        {
            _key = key;
            _item = item;
            _isMarker = isMarker;
            _next = next;
        }

        /// <summary>
        /// Creates a data node carrying an element.
        /// </summary>
        /// <param name="item">The element to store.</param>
        /// <param name="key">The element's split-order key (odd).</param>
        /// <param name="next">The initial successor, or <see langword="null" />.</param>
        /// <returns>A new unmarked data node.</returns>
        internal static Node CreateData(T item, ulong key, Node? next) =>
            new(key, item, isMarker: false, next);

        /// <summary>
        /// Creates a bucket sentinel node.
        /// </summary>
        /// <param name="key">The bucket's split-order key (even).</param>
        /// <param name="next">The initial successor, or <see langword="null" />.</param>
        /// <returns>A new sentinel node.</returns>
        internal static Node CreateSentinel(ulong key, Node? next) =>
            new(key, default!, isMarker: false, next);

        /// <summary>
        /// Creates a deletion marker whose successor is fixed for the marker's lifetime.
        /// </summary>
        /// <param name="successor">The marked node's real successor at marking time.</param>
        /// <returns>A new marker node.</returns>
        internal static Node CreateMarker(Node? successor) =>
            new(0UL, default!, isMarker: true, successor);
    }
}
