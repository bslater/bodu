// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemEcbFeedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IByteCache{TKey}" /> that persists downloaded ECB feeds as files in a cache directory, keyed by the
/// feed that produced them.
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
    : FileSystemByteCache<EcbRateFeed>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemEcbFeedCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-ecb</c> folder under the system temporary
    /// path is used.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when a best-effort file-system failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    public FileSystemEcbFeedCache(string? directory, ILogger? logger = null)
        : base(directory, "bodu-ecb", logger) { }

    /// <inheritdoc />
    protected override string GetFileName(EcbRateFeed key)
    {
        ThrowHelper.ThrowIfNull(key);

        return key.FileName;
    }
}
