// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Controls how a <see cref="PstFile" /> is opened and validated.
/// </summary>
public sealed class PstFileOptions
{
    /// <summary>The default materialization limit: 256 MiB.</summary>
    internal const long DefaultMaxNodeDataLength = 256L * 1024 * 1024;

    /// <summary>The default data-tree fan-out limit: 65,536 leaf blocks (about 512 MiB of 8 KiB blocks).</summary>
    internal const int DefaultMaxDataTreeLeaves = 65_536;

    /// <summary>The backing field for <see cref="BlockCacheSize" />.</summary>
    private readonly int _blockCacheSize = 256;

    /// <summary>The backing field for <see cref="MaxNodeDataLength" />.</summary>
    private readonly long _maxNodeDataLength = DefaultMaxNodeDataLength;

    /// <summary>The backing field for <see cref="MaxDataTreeLeaves" />.</summary>
    private readonly int _maxDataTreeLeaves = DefaultMaxDataTreeLeaves;

    /// <summary>
    /// Gets the shared default options instance.
    /// </summary>
    /// <value>The default options.</value>
    internal static PstFileOptions Default { get; } = new();

    /// <summary>
    /// Gets how strictly the file's structures and checksums are validated.
    /// </summary>
    /// <value>The validation level; <see cref="PstValidationLevel.Compatible" /> by default.</value>
    public PstValidationLevel ValidationLevel { get; init; } = PstValidationLevel.Compatible;

    /// <summary>
    /// Gets the maximum number of decoded pages and block payloads the session keeps in its least-recently-used
    /// cache.
    /// </summary>
    /// <value>The cache entry budget; <c>256</c> by default. <c>0</c> disables caching entirely.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    /// <remarks>
    /// Every cached entry is at most one block (8,192 bytes), so the default budget bounds the cache at roughly 2 MB
    /// per open session. Repeated structural reads — B-tree walks, property-context and table-context access over the
    /// same nodes — are served from the cache instead of re-reading and re-decoding from the source stream.
    /// </remarks>
    public int BlockCacheSize
    {
        get => _blockCacheSize;
        init
        {
            ThrowHelper.ThrowIfNegative(value);

            _blockCacheSize = value;
        }
    }

    /// <summary>
    /// Gets the largest node payload, in bytes, the session materializes in memory.
    /// </summary>
    /// <value>The materialization limit; 256 MiB by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    /// <remarks>
    /// <para>
    /// The limit governs every read that assembles a node's whole payload — <see cref="PstNode.ReadAllBytes" />, the
    /// heap-on-node parse behind the property and table contexts, and subnode-resident property values. It does not
    /// govern <see cref="PstNode.OpenDataStream" />, which reads one block at a time and is unbounded by design.
    /// </para>
    /// <para>
    /// A data tree declaring more than this is refused with <see cref="PstFileFormatException" /> and
    /// <see cref="PstFileError.LimitExceeded" /> at every validation level: a crafted tree can reference the same
    /// physical block many thousands of times, so the declared size — not the file size — is what bounds the
    /// allocation.
    /// </para>
    /// </remarks>
    public long MaxNodeDataLength
    {
        get => _maxNodeDataLength;
        init
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);

            _maxNodeDataLength = value;
        }
    }

    /// <summary>
    /// Gets the largest number of leaf data blocks a node's data tree may reference.
    /// </summary>
    /// <value>The fan-out limit; 65,536 by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    /// <remarks>
    /// The limit is enforced while the tree's internal blocks are walked, before any leaf payload is read, and applies
    /// to streaming and buffered reads alike — the leaf list itself is the allocation it bounds. A tree exceeding it
    /// is refused with <see cref="PstFileFormatException" /> and <see cref="PstFileError.LimitExceeded" />.
    /// </remarks>
    public int MaxDataTreeLeaves
    {
        get => _maxDataTreeLeaves;
        init
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);

            _maxDataTreeLeaves = value;
        }
    }
}
