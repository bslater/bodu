// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemByteCache{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a best-effort, file-backed cache of downloaded response bytes keyed by a provider-specific download unit.
/// </summary>
/// <typeparam name="TKey">The type identifying a cached download unit (a date range, feed, era, and so on).</typeparam>
/// <remarks>
/// <para>
/// Each key maps to a single file in the cache directory, named by <see cref="GetFileName(TKey)" />; the file's
/// last-write time serves as its freshness timestamp. A cached file is served only while
/// <see cref="IsFresh(TKey, TimeSpan, TimeSpan)" /> accepts its age against the supplied refresh interval.
/// </para>
/// <para>
/// The cache is best-effort: any I/O failure while reading is reported as a miss, and any failure while writing is
/// swallowed, so a cache problem never breaks rate retrieval.
/// </para>
/// </remarks>
public abstract class FileSystemByteCache<TKey>
{
    /// <summary>The directory in which cached response bytes are stored.</summary>
    private readonly string _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemByteCache{TKey}" /> class.
    /// </summary>
    /// <param name="directory">The cache directory, or <see langword="null" />/blank to use the default.</param>
    /// <param name="defaultFolderName">
    /// The folder name (under the system temporary path) to use when <paramref name="directory" /> is
    /// <see langword="null" /> or blank.
    /// </param>
    protected FileSystemByteCache(string? directory, string defaultFolderName)
    {
        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Path.GetTempPath(), defaultFolderName)
            : directory;
    }

    /// <summary>
    /// Gets the directory in which cached response bytes are stored.
    /// </summary>
    /// <value>The absolute or relative cache directory path.</value>
    public string Directory => _directory;

    /// <summary>
    /// Derives the cache file name for a key.
    /// </summary>
    /// <param name="key">The download unit to name.</param>
    /// <returns>The cache file name.</returns>
    protected abstract string GetFileName(TKey key);

    /// <summary>
    /// Determines whether a cached file of the supplied age is still fresh for a key.
    /// </summary>
    /// <param name="key">The download unit the file was cached for.</param>
    /// <param name="age">The elapsed time since the file was last written.</param>
    /// <param name="refreshInterval">The maximum age a cached file may reach before it is treated as stale.</param>
    /// <returns>
    /// <see langword="true" /> when the file may still be served; otherwise <see langword="false" />. The default
    /// treats a file as fresh while its age does not exceed <paramref name="refreshInterval" />.
    /// </returns>
    protected virtual bool IsFresh(TKey key, TimeSpan age, TimeSpan refreshInterval) =>
        age <= refreshInterval;

    /// <summary>
    /// Attempts to read the cached bytes for a key when a fresh file exists.
    /// </summary>
    /// <param name="key">The download unit to look up.</param>
    /// <param name="refreshInterval">The maximum age a cached file may reach before it is treated as stale.</param>
    /// <param name="bytes">
    /// When this method returns <see langword="true" />, the cached bytes; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when fresh bytes were read; otherwise <see langword="false" />.</returns>
    protected bool TryGetCore(TKey key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
    {
        string path = Path.Combine(_directory, GetFileName(key));

        try
        {
            if (!File.Exists(path))
            {
                bytes = null;
                return false;
            }

            if (!IsFresh(key, DateTime.UtcNow - File.GetLastWriteTimeUtc(path), refreshInterval))
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

    /// <summary>
    /// Writes the response bytes for a key, swallowing any I/O failure.
    /// </summary>
    /// <param name="key">The download unit the bytes belong to.</param>
    /// <param name="bytes">The response bytes to persist.</param>
    /// <exception cref="ArgumentNullException"><paramref name="bytes" /> is <see langword="null" />.</exception>
    protected void StoreCore(TKey key, byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);

        try
        {
            System.IO.Directory.CreateDirectory(_directory);
            File.WriteAllBytes(Path.Combine(_directory, GetFileName(key)), bytes);
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
