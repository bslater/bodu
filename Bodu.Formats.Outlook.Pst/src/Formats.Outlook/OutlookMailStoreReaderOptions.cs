// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Controls how an <see cref="OutlookMailStore" /> reads its PST container and decodes message content.
/// </summary>
public sealed class OutlookMailStoreReaderOptions
{
    /// <summary>The backing field for <see cref="BlockCacheSize" />.</summary>
    private readonly int _blockCacheSize = 256;

    /// <summary>
    /// Gets the shared default options instance.
    /// </summary>
    /// <value>The default options.</value>
    internal static OutlookMailStoreReaderOptions Default { get; } = new();

    /// <summary>
    /// Gets how strictly the container's structures and checksums are validated.
    /// </summary>
    /// <value>The validation level; <see cref="PstValidationLevel.Compatible" /> by default.</value>
    public PstValidationLevel ValidationLevel { get; init; } = PstValidationLevel.Compatible;

    /// <summary>
    /// Gets the decoded page/block cache budget of the underlying container session, in entries.
    /// </summary>
    /// <value>The cache entry budget; <c>256</c> by default. <c>0</c> disables caching entirely.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
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
    /// Gets a value indicating whether the compressed RTF body (<c>PidTagRtfCompressed</c>) is decompressed when read
    /// through the body conveniences.
    /// </summary>
    /// <value><see langword="true" /> by default; when disabled the RTF body convenience returns <see langword="null" />.</value>
    public bool DecompressRtf { get; init; } = true;

    /// <summary>
    /// Builds the container options this instance implies.
    /// </summary>
    /// <returns>The equivalent <see cref="PstFileOptions" />.</returns>
    internal PstFileOptions ToPstFileOptions() =>
        new() { ValidationLevel = ValidationLevel, BlockCacheSize = BlockCacheSize };
}
