// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemBoeResponseCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Boe;

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
    : IBoeResponseCache
{
    /// <summary>
    /// The directory in which cached responses are stored.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBoeResponseCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-boe</c> folder under the system temporary
    /// path is used.
    /// </param>
    public FileSystemBoeResponseCache(string? directory)
    {
        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Path.GetTempPath(), "bodu-boe")
            : directory;
    }

    /// <summary>
    /// Gets the directory in which cached responses are stored.
    /// </summary>
    /// <returns>The absolute or relative cache directory path.</returns>
    public string Directory => _directory;

    /// <inheritdoc />
    public bool TryGet(DateOnly startDate, DateOnly endDate, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        string path = Path.Combine(_directory, FileName(startDate, endDate));

        try
        {
            if (!File.Exists(path))
            {
                bytes = null;
                return false;
            }

            if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > refreshInterval)
            {
                bytes = null;
                return false;
            }

            bytes = File.ReadAllBytes(path);
            return true;
        }
        catch (IOException)
        {
            bytes = null;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            bytes = null;
            return false;
        }
    }

    /// <inheritdoc />
    public void Store(DateOnly startDate, DateOnly endDate, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);

        try
        {
            System.IO.Directory.CreateDirectory(_directory);
            File.WriteAllBytes(Path.Combine(_directory, FileName(startDate, endDate)), bytes);
        }
        catch (IOException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
    }

    /// <summary>
    /// Derives the cache file name for an inclusive date range.
    /// </summary>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns>The cache file name.</returns>
    private static string FileName(DateOnly startDate, DateOnly endDate) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "boe_{0:yyyyMMdd}_{1:yyyyMMdd}.csv",
            startDate,
            endDate);
}
