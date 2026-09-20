// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockComputation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Specialized;

/// <summary>
/// Represents the result of one block-mode Merkle computation: the tree's root, the shape of the input it was taken
/// over, and the ordered leaf hashes an authentication path is built from.
/// </summary>
/// <remarks>
/// <para>
/// This type exists so that a caller which has just streamed a large input past itself receives both the root <em>and</em>
/// the leaf hashes from the same pass. Generating an authentication path needs the leaf hashes; without them the caller
/// would have to stream the input a second time.
/// </para>
/// <para>
/// The leaf hashes cost <c>leafCount × hashLength</c> bytes to retain — for a 512 MiB input at one-mebibyte blocks that
/// is 512 hashes, or 16 KiB. A caller that needs only the root and not a path should use
/// <see cref="Rfc6962MerkleTree.ComputeRootOfBlocks(System.IO.Stream, int, System.Threading.CancellationToken)" />,
/// which folds the tree as it reads and never holds more than a logarithmic number of hashes.
/// </para>
/// </remarks>
public sealed class MerkleBlockComputation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MerkleBlockComputation" /> class.
    /// </summary>
    /// <param name="root">The tree's root hash.</param>
    /// <param name="inputLength">The total number of input bytes the tree was computed over.</param>
    /// <param name="blockSize">The block size the input was divided by.</param>
    /// <param name="leafHashes">The ordered leaf hashes.</param>
    internal MerkleBlockComputation(byte[] root, long inputLength, int blockSize, IReadOnlyList<byte[]> leafHashes)
    {
        Root = root;
        InputLength = inputLength;
        BlockSize = blockSize;
        LeafHashes = leafHashes;
    }

    /// <summary>
    /// Gets the tree's root hash — the unbound Merkle Tree Hash, not a length-bound root.
    /// </summary>
    /// <value>
    /// The root, which is the hash of zero bytes when <see cref="InputLength" /> is zero, and the sole leaf's hash when
    /// the input produced exactly one block.
    /// </value>
    /// <remarks>
    /// To publish a commitment that also pins the input's length, pass this value and <see cref="InputLength" /> to
    /// <see cref="Rfc6962MerkleTree.BindRoot(ReadOnlySpan{byte}, long)" />.
    /// </remarks>
    public byte[] Root { get; }

    /// <summary>
    /// Gets the total number of input bytes the tree was computed over.
    /// </summary>
    /// <value>The input length in bytes; zero for an empty input.</value>
    public long InputLength { get; }

    /// <summary>
    /// Gets the block size the input was divided into leaves by.
    /// </summary>
    /// <value>The block size in bytes.</value>
    public int BlockSize { get; }

    /// <summary>
    /// Gets the ordered leaf hashes, one per block.
    /// </summary>
    /// <value>
    /// The leaf hashes in block order, which is empty for a zero-length input because such an input has zero blocks
    /// rather than one empty block.
    /// </value>
    /// <remarks>
    /// Pass this list to <see cref="Rfc6962MerkleTree.AuthenticationPath(IReadOnlyList{byte[]}, long)" /> to produce an
    /// authentication path without re-reading the input.
    /// </remarks>
    public IReadOnlyList<byte[]> LeafHashes { get; }
}
