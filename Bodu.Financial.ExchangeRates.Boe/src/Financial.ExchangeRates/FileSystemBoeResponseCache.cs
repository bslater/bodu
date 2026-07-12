// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IByteCache{TKey}" /> that persists downloaded IADB range responses as files in a cache directory,
/// keyed by the inclusive date range that produced them.
/// </summary>
/// <remarks>
/// <para>
/// Each range is stored under a file name derived from its inclusive bounds; the file's last-write time serves as the
/// freshness timestamp. A cached response is treated as stale once it exceeds the supplied refresh interval, forcing a
/// re-download.
/// </para>
/// <para>
/// The cache is best-effort: any I/O failure while reading is reported as a miss, and any failure while writing is
/// swallowed, so a cache problem never breaks rate retrieval.
/// </para>
/// </remarks>
public sealed class FileSystemBoeResponseCache
    : FileSystemByteCache<(DateOnly StartDate, DateOnly EndDate)>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBoeResponseCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-boe</c> folder under the system temporary
    /// path is used.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when a best-effort file-system failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    public FileSystemBoeResponseCache(string? directory, ILogger? logger = null)
        : base(directory, "bodu-boe", logger) { }

    /// <inheritdoc />
    protected override string GetFileName((DateOnly StartDate, DateOnly EndDate) key) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "boe_{0:yyyyMMdd}_{1:yyyyMMdd}.csv",
            key.StartDate,
            key.EndDate);
}
