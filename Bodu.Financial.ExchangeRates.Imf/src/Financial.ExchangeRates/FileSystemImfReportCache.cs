// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemImfReportCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IImfReportCache" /> that persists downloaded IMF monthly reports as files in a cache directory.
/// </summary>
/// <remarks>
/// <para>
/// Each report is stored under its month's file name in the cache directory; the file's last-write time serves as the
/// freshness timestamp. A report for a month strictly before the current UTC month is immutable and is therefore always
/// served regardless of its age; the report for the current month is treated as stale once it exceeds the supplied
/// refresh interval, forcing a re-download.
/// </para>
/// <para>
/// The cache is best-effort: any I/O failure while reading is reported as a miss, and any failure while writing is
/// swallowed, so a cache problem never breaks rate retrieval.
/// </para>
/// </remarks>
public sealed class FileSystemImfReportCache
    : FileSystemByteCache<ImfReportMonth>, IImfReportCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemImfReportCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-imf</c> folder under the system temporary
    /// path is used.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when a best-effort file-system failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    public FileSystemImfReportCache(string? directory, ILogger? logger = null)
        : base(directory, "bodu-imf", logger) { }

    /// <inheritdoc />
    public bool TryGet(ImfReportMonth month, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(month);

        return TryGetCore(month, refreshInterval, out bytes);
    }

    /// <inheritdoc />
    public void Store(ImfReportMonth month, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(month);
        ThrowHelper.ThrowIfNull(bytes);

        StoreCore(month, bytes);
    }

    /// <inheritdoc />
    protected override string GetFileName(ImfReportMonth key) =>
        key.FileName;

    /// <inheritdoc />
    /// <remarks>
    /// A report for a month strictly before the current UTC month is closed and immutable, so it is always fresh
    /// regardless of its file age; only the current (still-accumulating) month is subject to the refresh interval.
    /// </remarks>
    protected override bool IsFresh(ImfReportMonth key, TimeSpan age, TimeSpan refreshInterval)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool isClosedMonth = key.Year < today.Year || (key.Year == today.Year && key.Month < today.Month);

        return isClosedMonth || age <= refreshInterval;
    }
}
