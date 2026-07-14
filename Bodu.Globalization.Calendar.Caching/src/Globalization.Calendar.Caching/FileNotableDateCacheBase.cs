// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileNotableDateCacheBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the file-storage mechanism for an <see cref="INotableDateCache" />: one file per territory under a cache
/// directory, atomic temp-and-move writes, a last-write-time parse memo, and best-effort degradation on storage
/// failures. Derived types implement only the file extension and the serialization of a territory's entry list.
/// </summary>
/// <remarks>
/// <para>
/// Each territory maps to a single file named after the normalized, sanitized territory code. A read that fails, or a
/// file that cannot be deserialized, degrades to an empty result rather than throwing (unless
/// <see cref="NotableDateCacheOptions.ThrowOnStorageFailure" /> is set), and a swallowed storage failure is reported
/// through a rate-limited warning so a sustained outage does not flood the log.
/// </para>
/// <para>
/// Writes are atomic through <see cref="AtomicFileWriter" />, so a concurrent reader never observes a partially written
/// file, and a parse memo keyed by the file's last-write time avoids re-reading and re-parsing an unchanged file while
/// still re-parsing a file changed by another process.
/// </para>
/// </remarks>
public abstract class FileNotableDateCacheBase
    : NotableDateCacheBase<FileNotableDateCacheOptions>
{
    /// <summary>The resolved cache directory the files are stored under.</summary>
    private readonly string _directory;

    /// <summary>The logger that receives best-effort storage warnings.</summary>
    private readonly ILogger _logger;

    /// <summary>Whether a swallowed storage failure is rethrown instead of degrading best-effort.</summary>
    private readonly bool _throwOnFailure;

    /// <summary>Rate-limits the swallowed-storage-failure warning to at most one per cooldown window.</summary>
    private readonly RateLimitedWarningGate _warnGate;

    /// <summary>Memoizes the parsed entries per file path against the file's last-write time, so an unchanged file is not re-read. Bounded with LRU eviction so a long-lived process touching many territories cannot grow it without limit; eviction only forces a re-parse.</summary>
    private readonly FileParseMemo<IReadOnlyList<NotableDateCacheEntry>> _parsed = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileNotableDateCacheBase" /> class.
    /// </summary>
    /// <param name="options">The file-cache options that select the storage directory.</param>
    /// <param name="timeProvider">
    /// The time source the swallowed-failure warning rate-limiting is measured against, or <see langword="null" /> to
    /// use <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a rate-limited warning when a best-effort storage failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected FileNotableDateCacheBase(FileNotableDateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options)
    {
        _directory = ResolveDirectory(options.CacheDirectory);
        _logger = logger ?? NullLogger.Instance;
        _throwOnFailure = options.ThrowOnStorageFailure;
        _warnGate = new RateLimitedWarningGate(timeProvider, RateLimitedWarningGate.DefaultCooldown);

        if (options.ValidateStorageOnStart)
            Directory.CreateDirectory(_directory);
    }

    /// <summary>
    /// Gets the directory the cache files are stored under.
    /// </summary>
    /// <value>The resolved cache directory.</value>
    public string CacheDirectory => _directory;

    /// <summary>
    /// Gets the file extension used by the concrete format, including the leading dot.
    /// </summary>
    /// <value>The file extension, such as <c>.toml</c> or <c>.json</c>.</value>
    protected abstract string FileExtension { get; }

    /// <inheritdoc />
    public override void Clear()
    {
        try
        {
            if (!Directory.Exists(_directory))
                return;

            foreach (string path in Directory.EnumerateFiles(_directory, $"*{FileExtension}"))
            {
                File.Delete(path);
                _parsed.Invalidate(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            OnStorageFailureSwallowed("clear", ex);
        }
    }

    /// <summary>
    /// Serializes a territory's entries to the concrete text format.
    /// </summary>
    /// <param name="territory">The normalized territory the entries belong to.</param>
    /// <param name="entries">The entries to serialize.</param>
    /// <returns>The serialized text.</returns>
    private protected abstract string Serialize(string territory, IReadOnlyList<NotableDateCacheEntry> entries);

    /// <summary>
    /// Deserializes a territory's entries from the concrete text format, reporting a corrupt file as empty.
    /// </summary>
    /// <param name="text">The serialized text.</param>
    /// <param name="path">The full path the text was read from, for corrupt-file reporting.</param>
    /// <returns>The deserialized entries, or an empty list when the text is corrupt.</returns>
    private protected abstract IReadOnlyList<NotableDateCacheEntry> Deserialize(string text, string path);

    /// <summary>
    /// Reports a cache file that failed to deserialize and was treated as empty, so corruption surfaces to operators.
    /// </summary>
    /// <param name="path">The full path of the corrupt cache file.</param>
    /// <param name="exception">The swallowed deserialization exception.</param>
    private protected void OnCacheFileCorrupt(string path, Exception exception) =>
        Log.CacheFileCorrupt(_logger, path, exception);

    /// <inheritdoc />
    protected internal override IReadOnlyList<NotableDateCacheEntry> ReadEntries(string territory)
    {
        string path = FilePath(territory);

        try
        {
            if (!File.Exists(path))
                return Array.Empty<NotableDateCacheEntry>();

            DateTime writeTime = File.GetLastWriteTimeUtc(path);
            if (_parsed.TryGet(path, writeTime, out IReadOnlyList<NotableDateCacheEntry>? memoized))
                return memoized!;

            string text = File.ReadAllText(path);
            IReadOnlyList<NotableDateCacheEntry> entries = Deserialize(text, path);

            _parsed.Set(path, writeTime, entries);
            return entries;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (_throwOnFailure)
                throw;

            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<NotableDateCacheEntry>();
        }
    }

    /// <inheritdoc />
    protected internal override bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries)
    {
        string path = FilePath(territory);

        try
        {
            if (entries.Count == 0)
            {
                if (File.Exists(path))
                    File.Delete(path);

                _parsed.Invalidate(path);
                return true;
            }

            Directory.CreateDirectory(_directory);
            AtomicFileWriter.Write(path, Serialize(territory, entries));

            // Invalidate the parse memo so the next read re-parses from the freshly written file and re-stamps it.
            _parsed.Invalidate(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (_throwOnFailure)
                throw;

            OnStorageFailureSwallowed("store", ex);
            return false;
        }
    }

    /// <summary>
    /// Resolves the cache directory, defaulting to a <c>bodu-notable-dates</c> folder under the system temporary path
    /// when none is configured.
    /// </summary>
    /// <param name="configured">The configured directory, or <see langword="null" /> for the default.</param>
    /// <returns>The resolved directory.</returns>
    private static string ResolveDirectory(string? configured) =>
        string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "bodu-notable-dates")
            : configured;

    /// <summary>
    /// Resolves the full file path for a normalized territory.
    /// </summary>
    /// <param name="territory">The normalized territory key.</param>
    /// <returns>The full file path.</returns>
    private string FilePath(string territory) =>
        Path.Combine(_directory, Sanitize(territory) + FileExtension);

    /// <summary>
    /// Sanitizes a normalized territory into a safe file name stem, replacing any character that is not a letter, digit,
    /// or hyphen with an underscore.
    /// </summary>
    /// <param name="territory">The normalized territory key.</param>
    /// <returns>The sanitized file name stem.</returns>
    private static string Sanitize(string territory)
    {
        Span<char> buffer = territory.Length <= 64 ? stackalloc char[territory.Length] : new char[territory.Length];
        for (int i = 0; i < territory.Length; i++)
        {
            char c = territory[i];
            buffer[i] = char.IsAsciiLetterOrDigit(c) || c == '-' ? c : '_';
        }

        return new string(buffer);
    }

    /// <summary>
    /// Reports a swallowed best-effort storage failure to the logger, rate-limited so at most one warning is emitted per
    /// cooldown window.
    /// </summary>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    /// <param name="exception">The swallowed storage exception.</param>
    private void OnStorageFailureSwallowed(string operation, Exception exception)
    {
        if (_warnGate.TryClaimWarning(out int suppressed))
            Log.FileStorageFailureSwallowed(_logger, operation, suppressed, exception);
    }
}
