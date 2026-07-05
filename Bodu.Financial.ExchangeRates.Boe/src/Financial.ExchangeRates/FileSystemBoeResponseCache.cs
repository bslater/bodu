// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// An <see cref="IBoeResponseCache" /> that persists downloaded IADB range responses as files in a cache directory.
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
    : FileSystemByteCache<(DateOnly StartDate, DateOnly EndDate)>, IBoeResponseCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBoeResponseCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-boe</c> folder under the system temporary
    /// path is used.
    /// </param>
    public FileSystemBoeResponseCache(string? directory)
        : base(directory, "bodu-boe") { }

    /// <inheritdoc />
    public bool TryGet(DateOnly startDate, DateOnly endDate, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes) =>
        TryGetCore((startDate, endDate), refreshInterval, out bytes);

    /// <inheritdoc />
    public void Store(DateOnly startDate, DateOnly endDate, byte[] bytes) =>
        StoreCore((startDate, endDate), bytes);

    /// <inheritdoc />
    protected override string GetFileName((DateOnly StartDate, DateOnly EndDate) key) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "boe_{0:yyyyMMdd}_{1:yyyyMMdd}.csv",
            key.StartDate,
            key.EndDate);
}
