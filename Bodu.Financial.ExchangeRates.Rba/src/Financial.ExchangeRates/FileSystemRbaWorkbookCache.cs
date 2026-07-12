// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IByteCache{TKey}" /> that persists downloaded RBA workbooks as files in a cache directory, keyed by
/// the era workbook that produced them.
/// </summary>
/// <remarks>
/// <para>
/// Each era is stored under its file name in the cache directory; the file's last-write time serves as the freshness
/// timestamp. Immutable eras are served from the cache regardless of age, while the open-ended current era is treated
/// as stale once it exceeds the supplied refresh interval, forcing a re-download.
/// </para>
/// <para>
/// The cache is best-effort: any I/O failure while reading is reported as a miss, and any failure while writing is
/// swallowed, so a cache problem never breaks rate retrieval.
/// </para>
/// </remarks>
public sealed class FileSystemRbaWorkbookCache
    : FileSystemByteCache<RbaEraWorkbook>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemRbaWorkbookCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-rba</c> folder under the system temporary
    /// path is used.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when a best-effort file-system failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    public FileSystemRbaWorkbookCache(string? directory, ILogger? logger = null)
        : base(directory, "bodu-rba", logger) { }

    /// <inheritdoc />
    protected override string GetFileName(RbaEraWorkbook key)
    {
        ThrowHelper.ThrowIfNull(key);

        return key.FileName;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The open-ended current era expires on the refresh interval; fixed eras are immutable and never expire.
    /// </remarks>
    protected override bool IsFresh(RbaEraWorkbook key, TimeSpan age, TimeSpan refreshInterval) =>
        key.End is not null || age <= refreshInterval;
}
