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
/// freshness timestamp. A report written after its month had closed (in a later UTC month) holds every day of the month
/// and never changes, so it is always served regardless of its age. A report written while its month was still open is
/// treated as stale once it exceeds the supplied refresh interval, forcing a re-download, even after the month has
/// closed, because it lacks the days the IMF published after it was written.
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
    /// <remarks>
    /// Intentionally hides the unguarded base member so calls through this type (and through
    /// <see cref="IImfReportCache" />) validate their arguments before touching the file system.
    /// </remarks>
    public new bool TryGet(ImfReportMonth month, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(month);

        return TryGetCore(month, refreshInterval, out bytes);
    }

    /// <inheritdoc cref="IImfReportCache.TryGet(ImfReportMonth, TimeSpan, out byte[], out DateTimeOffset?)" />
    /// <remarks>
    /// The stored time is the cache file's last-write time. Intentionally hides the unguarded base member so calls
    /// through this type (and through <see cref="IImfReportCache" />) validate their arguments before touching the file
    /// system.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="month" /> is <see langword="null" />.</exception>
    public new bool TryGet(ImfReportMonth month, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes, out DateTimeOffset? storedAtUtc)
    {
        ThrowHelper.ThrowIfNull(month);

        return TryGetCore(month, refreshInterval, out bytes, out storedAtUtc);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Intentionally hides the unguarded base member so calls through this type (and through
    /// <see cref="IImfReportCache" />) validate their arguments before touching the file system.
    /// </remarks>
    public new void Store(ImfReportMonth month, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(month);
        ThrowHelper.ThrowIfNull(bytes);

        StoreCore(month, bytes);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    protected override string GetFileName(ImfReportMonth key)
    {
        ThrowHelper.ThrowIfNull(key);

        return key.FileName;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A report written in a UTC month later than its own was written after the month closed, so it is always fresh
    /// regardless of its file age; a report written during its own month (or earlier) is fresh only while its age does
    /// not exceed <paramref name="refreshInterval" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    protected override bool IsFresh(ImfReportMonth key, TimeSpan age, TimeSpan refreshInterval)
    {
        ThrowHelper.ThrowIfNull(key);

        // Whether the month had closed is decided at the time the file was written, not now: a report written while its
        // month was open lacks the days published after it, however long ago the month closed.
        DateOnly writtenOn = DateOnly.FromDateTime(DateTime.UtcNow - age);
        bool closedWhenWritten = key.Year < writtenOn.Year || (key.Year == writtenOn.Year && key.Month < writtenOn.Month);

        return closedWhenWritten || age <= refreshInterval;
    }
}
