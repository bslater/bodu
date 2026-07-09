// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemEcbFeedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IEcbFeedCache" /> that persists downloaded ECB feeds as files in a cache directory.
/// </summary>
/// <remarks>
/// <para>
/// Each feed is stored under its file name in the cache directory; the file's last-write time serves as the freshness
/// timestamp. Because every ECB feed extends to the most recent business day, a cached file is treated as stale once it
/// exceeds the supplied refresh interval, forcing a re-download.
/// </para>
/// <para>
/// The cache is best-effort: any I/O failure while reading is reported as a miss, and any failure while writing is
/// swallowed, so a cache problem never breaks rate retrieval.
/// </para>
/// </remarks>
public sealed class FileSystemEcbFeedCache
    : FileSystemByteCache<EcbRateFeed>, IEcbFeedCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemEcbFeedCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-ecb</c> folder under the system temporary
    /// path is used.
    /// </param>
    public FileSystemEcbFeedCache(string? directory)
        : base(directory, "bodu-ecb") { }

    /// <inheritdoc />
    public bool TryGet(EcbRateFeed feed, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(feed);

        return TryGetCore(feed, refreshInterval, out bytes);
    }

    /// <inheritdoc />
    public void Store(EcbRateFeed feed, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(feed);
        ThrowHelper.ThrowIfNull(bytes);

        StoreCore(feed, bytes);
    }

    /// <inheritdoc />
    protected override string GetFileName(EcbRateFeed key) =>
        key.FileName;
}
