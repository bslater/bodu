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
    /// <summary>The backing field for <see cref="BlockCacheSize" />.</summary>
    private readonly int _blockCacheSize = 256;

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
}
