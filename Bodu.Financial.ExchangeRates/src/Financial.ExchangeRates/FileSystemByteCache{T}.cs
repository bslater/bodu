// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemByteCache{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

    /// <summary>The logger that receives the best-effort degradation warnings; never <see langword="null" />.</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemByteCache{TKey}" /> class.
    /// </summary>
    /// <param name="directory">The cache directory, or <see langword="null" />/blank to use the default.</param>
    /// <param name="defaultFolderName">
    /// The folder name (under the system temporary path) to use when <paramref name="directory" /> is
    /// <see langword="null" /> or blank.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a warning when a best-effort file-system failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    protected FileSystemByteCache(string? directory, string defaultFolderName, ILogger? logger = null)
    {
        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(Path.GetTempPath(), defaultFolderName)
            : directory;
        _logger = logger ?? NullLogger.Instance;
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
    /// Resolves the absolute cache file path for a key, ensuring the derived file name cannot escape the cache
    /// directory.
    /// </summary>
    /// <param name="key">The download unit to resolve.</param>
    /// <returns>The cache file path, always inside <see cref="Directory" />.</returns>
    /// <remarks>
    /// The name returned by <see cref="GetFileName(TKey)" /> is reduced to a single, sanitized path segment before
    /// it is combined with the cache directory. A crafted key whose name contains directory separators, a
    /// parent-directory reference, or a rooted path therefore cannot direct a read or write outside the cache
    /// directory. Sanitization is silent (it never throws) so the cache stays best-effort.
    /// </remarks>
    private string ResolveCachePath(TKey key) =>
        Path.Combine(_directory, SanitizeFileName(GetFileName(key)));

    /// <summary>
    /// Reduces a candidate file name to a single, filesystem-safe path segment.
    /// </summary>
    /// <param name="fileName">The candidate file name derived from a key.</param>
    /// <returns>A single path segment with no separators, traversal, or invalid characters.</returns>
    private static string SanitizeFileName(string fileName)
    {
        // Strip any directory portion (rooted path or separators) so only the final segment survives, defeating a
        // key that tries to walk out of the cache directory.
        string name = Path.GetFileName(fileName ?? string.Empty);

        // A bare "." / ".." (or an empty result) is not a usable file name and would still reference a directory;
        // replace it with a safe placeholder.
        if (string.IsNullOrEmpty(name) || name == "." || name == "..")
            return "_";

        char[] invalid = Path.GetInvalidFileNameChars();
        if (name.AsSpan().IndexOfAny(invalid) < 0)
            return name;

        char[] chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0)
                chars[i] = '_';
        }

        return new string(chars);
    }

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
        string path = ResolveCachePath(key);

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
        catch (IOException ex)
        {
            FileSystemByteCacheLog.ReadFailureSwallowed(_logger, path, ex);
            bytes = null;
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            FileSystemByteCacheLog.ReadFailureSwallowed(_logger, path, ex);
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

        string path = ResolveCachePath(key);

        try
        {
            System.IO.Directory.CreateDirectory(_directory);
            File.WriteAllBytes(path, bytes);
        }
        catch (IOException ex)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
            FileSystemByteCacheLog.StoreFailureSwallowed(_logger, path, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
            FileSystemByteCacheLog.StoreFailureSwallowed(_logger, path, ex);
        }
    }
}
