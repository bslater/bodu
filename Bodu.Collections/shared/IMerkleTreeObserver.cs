// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IMerkleTreeObserver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if SECURITY_CRYPTOGRAPHY
namespace Bodu.Security.Cryptography;
#else
namespace Bodu.Collections.Specialized;
#endif

/// <summary>
/// Receives every node a Merkle computation produces, in the order it is produced, so a trace can be recorded without
/// the computation retaining anything itself.
/// </summary>
/// <remarks>
/// A promoted node — a lone leftover carried up a level unchanged — is not reported again: it is the same node, already
/// reported at the level that produced it. Only leaves and hashed internal nodes arrive here.
/// </remarks>
internal interface IMerkleTreeObserver
{
    /// <summary>
    /// Reports a leaf hash.
    /// </summary>
    /// <param name="index">The zero-based index of the leaf.</param>
    /// <param name="hash">The leaf's hash.</param>
    void OnLeaf(long index, byte[] hash);

    /// <summary>
    /// Reports an internal node hashed from a group of children.
    /// </summary>
    /// <param name="level">The node's level, one above the level of its children; leaves are level zero.</param>
    /// <param name="index">The zero-based index of the node within its level.</param>
    /// <param name="children">The child hashes the node was hashed from, in order.</param>
    /// <param name="hash">The node's hash.</param>
    void OnNode(int level, long index, byte[][] children, byte[] hash);
}
