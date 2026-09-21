// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleLevelFold.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

#if SECURITY_CRYPTOGRAPHY
namespace Bodu.Security.Cryptography;
#else
namespace Bodu.Collections.Specialized;
#endif

/// <summary>
/// Folds a stream of leaf hashes into a root level by level with a configurable fan-out, holding only the nodes still
/// waiting for a full group at each level.
/// </summary>
/// <remarks>
/// <para>
/// Nodes at each level are grouped left to right in groups of <c>fanOut</c>; a full group is hashed to a parent as soon
/// as it completes. When the input ends, whatever each level still holds is carried upward from the leaves: a lone
/// leftover node is <em>promoted unchanged</em>, a partial group of two or more is hashed. Promotion rather than
/// re-hashing is what makes a fan-out of two produce RFC 6962's Merkle Tree Hash for every leaf count — the
/// level-by-level walk then visits exactly the nodes of the recursive definition — and an empty input folds to the
/// empty tree's root, <c>H()</c>.
/// </para>
/// <para>
/// A fan-out above two is a sound commitment of its own but is not RFC 6962's tree, which is binary by definition. The
/// fold holds at most <c>fanOut − 1</c> nodes per level and one level per <c>log<sub>fanOut</sub></c> of the leaf
/// count, so memory does not grow with the input.
/// </para>
/// <para>
/// The fold owns neither the hash algorithm nor the observer; the caller supplies both and this type is used from one
/// thread at a time.
/// </para>
/// </remarks>
internal sealed class MerkleLevelFold
{
    /// <summary>The algorithm every internal node is hashed with.</summary>
    private readonly HashAlgorithm _hasher;

    /// <summary>The digest length, in bytes, of <see cref="_hasher" />.</summary>
    private readonly int _hashLength;

    /// <summary>The number of children hashed into each parent.</summary>
    private readonly int _fanOut;

    /// <summary>Receives every leaf and hashed node, or <see langword="null" /> when nothing is recorded.</summary>
    private readonly IMerkleTreeObserver? _observer;

    /// <summary>The nodes at each level still waiting for their group to fill; index zero is the leaf level.</summary>
    private readonly List<List<byte[]>> _pending = [];

    /// <summary>The index the next hashed node at each level receives, reported to the observer.</summary>
    private readonly List<long> _nextIndex = [];

    /// <summary>The number of leaves added so far.</summary>
    private long _leafCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleLevelFold" /> class.
    /// </summary>
    /// <param name="hasher">The algorithm every internal node is hashed with.</param>
    /// <param name="hashLength">The algorithm's digest length, in bytes.</param>
    /// <param name="fanOut">The number of children hashed into each parent. Must be at least two.</param>
    /// <param name="observer">
    /// Receives every leaf and hashed node, or <see langword="null" /> to record nothing.
    /// </param>
    internal MerkleLevelFold(HashAlgorithm hasher, int hashLength, int fanOut, IMerkleTreeObserver? observer = null)
    {
        _hasher = hasher;
        _hashLength = hashLength;
        _fanOut = fanOut;
        _observer = observer;
    }

    /// <summary>
    /// Gets the number of leaves added so far.
    /// </summary>
    internal long LeafCount => _leafCount;

    /// <summary>
    /// Adds the next leaf hash, hashing any group it completes.
    /// </summary>
    /// <param name="leafHash">The leaf's hash, in leaf order.</param>
    internal void Add(byte[] leafHash)
    {
        _observer?.OnLeaf(_leafCount, leafHash);
        _leafCount++;

        Push(0, leafHash);
    }

    /// <summary>
    /// Carries the remaining nodes upward and returns the root; the empty tree's root when no leaf was added.
    /// </summary>
    /// <returns>The root hash.</returns>
    /// <remarks>
    /// The fold is spent after this call; a new computation needs a new instance.
    /// </remarks>
    internal byte[] Finish()
    {
        if (_leafCount == 0)
            return MerkleTreeCore.HashEmpty(_hasher, _hashLength);

        byte[]? carry = null;
        for (int level = 0; ; level++)
        {
            List<byte[]> nodes = level < _pending.Count ? _pending[level] : [];
            if (carry is not null)
                nodes.Add(carry);

            if (nodes.Count == 0)
            {
                carry = null;
                continue;
            }

            // The lone node at the highest occupied level is the root. Below that, a lone node is promoted unchanged
            // and a partial group is hashed — the same rule that applied to full groups on the way up.
            if (nodes.Count == 1 && !HasPendingAbove(level))
                return nodes[0];

            carry = nodes.Count == 1 ? nodes[0] : HashGroup(level, nodes);
            nodes.Clear();
        }
    }

    /// <summary>
    /// Appends a node at a level, hashing the group it completes and pushing the parent one level up.
    /// </summary>
    /// <param name="level">The level the node belongs to.</param>
    /// <param name="node">The node's hash.</param>
    private void Push(int level, byte[] node)
    {
        while (_pending.Count <= level)
        {
            _pending.Add(new List<byte[]>(_fanOut));
            _nextIndex.Add(0);
        }

        List<byte[]> nodes = _pending[level];
        nodes.Add(node);

        if (nodes.Count == _fanOut)
        {
            byte[] parent = HashGroup(level, nodes);
            nodes.Clear();
            Push(level + 1, parent);
        }
    }

    /// <summary>
    /// Hashes a group of nodes at a level into their parent and reports it.
    /// </summary>
    /// <param name="level">The level the group's nodes belong to.</param>
    /// <param name="nodes">The group, in order.</param>
    /// <returns>The parent's hash.</returns>
    private byte[] HashGroup(int level, List<byte[]> nodes)
    {
        byte[][] children = [.. nodes];
        byte[] parent = MerkleTreeCore.HashChildren(_hasher, _hashLength, children);

        if (_observer is not null)
        {
            while (_nextIndex.Count <= level + 1)
                _nextIndex.Add(0);

            _observer.OnNode(level + 1, _nextIndex[level + 1]++, children, parent);
        }

        return parent;
    }

    /// <summary>
    /// Returns whether any level above the specified one still holds a node.
    /// </summary>
    /// <param name="level">The level to look above.</param>
    /// <returns><see langword="true" /> when a higher level has a pending node.</returns>
    private bool HasPendingAbove(int level)
    {
        for (int above = level + 1; above < _pending.Count; above++)
        {
            if (_pending[above].Count > 0)
                return true;
        }

        return false;
    }
}
