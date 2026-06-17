// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemRbaWorkbookCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates.Rba;

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
    : IRbaWorkbookCache
{
    /// <summary>The directory in which cached workbooks are stored.</summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemRbaWorkbookCache" /> class.
    /// </summary>
    /// <param name="directory">
    /// The cache directory. When <see langword="null" /> or empty, a <c>bodu-rba</c> folder under the system temporary
    /// path is used.
    /// </param>
    public FileSystemRbaWorkbookCache(string? directory)
    {
        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Path.GetTempPath(), "bodu-rba")
            : directory;
    }

    /// <summary>
    /// Gets the directory in which cached workbooks are stored.
    /// </summary>
    /// <returns>The absolute or relative cache directory path.</returns>
    public string Directory => _directory;

    /// <inheritdoc />
    public bool TryGet(RbaEra era, TimeSpan currentEraRefreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(era);

        string path = Path.Combine(_directory, era.FileName);

        try
        {
            if (!File.Exists(path))
            {
                bytes = null;
                return false;
            }

            // The open-ended current era expires; fixed eras are immutable and never expire.
            if (era.End is null && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > currentEraRefreshInterval)
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
    public void Store(RbaEra era, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(era);
        ThrowHelper.ThrowIfNull(bytes);

        try
        {
            System.IO.Directory.CreateDirectory(_directory);
            File.WriteAllBytes(Path.Combine(_directory, era.FileName), bytes);
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
