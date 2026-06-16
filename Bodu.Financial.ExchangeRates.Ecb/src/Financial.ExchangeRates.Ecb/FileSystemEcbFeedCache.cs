// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemEcbFeedCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates.Ecb;

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
    : IEcbFeedCache
{
    /// <summary>
    /// The directory in which cached feeds are stored.
    /// </summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemEcbFeedCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-ecb</c> folder under the system temporary
    /// path is used.
    /// </param>
    public FileSystemEcbFeedCache(string? directory)
    {
        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Path.GetTempPath(), "bodu-ecb")
            : directory;
    }

    /// <summary>
    /// Gets the directory in which cached feeds are stored.
    /// </summary>
    /// <returns>The absolute or relative cache directory path.</returns>
    public string Directory => _directory;

    /// <inheritdoc />
    public bool TryGet(EcbExchangeRateFeed feed, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(feed);

        string path = Path.Combine(_directory, feed.FileName);

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
    public void Store(EcbExchangeRateFeed feed, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(feed);
        ThrowHelper.ThrowIfNull(bytes);

        try
        {
            System.IO.Directory.CreateDirectory(_directory);
            File.WriteAllBytes(Path.Combine(_directory, feed.FileName), bytes);
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
}
