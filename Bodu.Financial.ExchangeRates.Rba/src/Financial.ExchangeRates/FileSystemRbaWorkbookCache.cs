// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IRbaWorkbookCache" /> that persists downloaded RBA workbooks as files in a cache directory.
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
    : FileSystemByteCache<RbaEraWorkbook>, IRbaWorkbookCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemRbaWorkbookCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-rba</c> folder under the system temporary
    /// path is used.
    /// </param>
    public FileSystemRbaWorkbookCache(string? directory)
        : base(directory, "bodu-rba") { }

    /// <inheritdoc />
    public bool TryGet(RbaEraWorkbook era, TimeSpan currentEraRefreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(era);

        return TryGetCore(era, currentEraRefreshInterval, out bytes);
    }

    /// <inheritdoc />
    public void Store(RbaEraWorkbook era, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(era);
        ThrowHelper.ThrowIfNull(bytes);

        StoreCore(era, bytes);
    }

    /// <inheritdoc />
    protected override string GetFileName(RbaEraWorkbook key) =>
        key.FileName;

    /// <inheritdoc />
    /// <remarks>
    /// The open-ended current era expires on the refresh interval; fixed eras are immutable and never expire.
    /// </remarks>
    protected override bool IsFresh(RbaEraWorkbook key, TimeSpan age, TimeSpan refreshInterval) =>
        key.End is not null || age <= refreshInterval;
}
