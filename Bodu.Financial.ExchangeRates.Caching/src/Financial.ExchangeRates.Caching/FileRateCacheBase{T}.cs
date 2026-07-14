// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileRateCacheBase{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Bodu.Caching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the file-storage mechanism for an <see cref="IRateCache" />: layout-driven directory and file-name
/// resolution, optional date partitioning, and best-effort file read and write. Derived types supply only the
/// serialization format.
/// </summary>
/// <typeparam name="TOptions">
/// The file-cache options type carrying the bound provider, storage directory, and file layout.
/// </typeparam>
/// <remarks>
/// <para>
/// A cache instance is bound to one provider, and the <see cref="FileRateCacheOptions.Layout" /> decides where each
/// pair's rows are stored: a single file per pair (the default), or — when the layout is partitioned — one file per
/// calendar period under a per-pair folder. Reads and writes are best-effort: any <see cref="IOException" /> or
/// <see cref="UnauthorizedAccessException" /> surfaces as an empty read result or a skipped write, so a storage problem
/// never breaks rate retrieval. Derived types are expected to treat malformed content the same way by returning an
/// empty list from <see cref="Deserialize" />.
/// </para>
/// <para>
/// For a partitioned layout a pair's rows are split across files by their observation date, and a recorded coverage
/// window is split at partition boundaries so each file carries only the coverage for its own period; a read
/// concatenates every partition file in the pair's folder and the shared cache rules re-merge the halves, so the split
/// is lossless.
/// </para>
/// </remarks>
public abstract class FileRateCacheBase<TOptions>
    : RateCacheBase<TOptions>, IFileRateCache
    where TOptions : FileRateCacheOptions
{
    /// <summary>The resolved root directory in which this provider's cached rate files are stored.</summary>
    private readonly string _directory;

    /// <summary>The layout deciding the folder hierarchy, file names, and date partitioning for each pair.</summary>
    private readonly RateCacheFileLayout _layout;

    /// <summary>The logger that receives the best-effort degradation messages; never <see langword="null" />.</summary>
    private readonly ILogger _logger;

    /// <summary>Rate-limits the swallowed-failure warning to at most one emission per <see cref="RateLimitedWarningGate.DefaultCooldown" /> window.</summary>
    private readonly RateLimitedWarningGate _warnGate;

    /// <summary>Memoizes the most recently parsed state per file, keyed by full path, against the file's last-write instant, so a repeated read of an unchanged file serves the cached parse rather than re-reading and re-deserializing it on every lookup. An entry is invalidated when this instance writes or deletes the file, and a differing last-write instant — an external or cross-process change — is detected on the next read, so the memo never serves data from a file whose timestamp has moved. Keying by path (not by pair) lets one pair span many partition files.</summary>
    private readonly ConcurrentDictionary<string, (DateTime StampUtc, CachePairState State)> _parsed = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileRateCacheBase{TOptions}" /> class.
    /// </summary>
    /// <param name="options">
    /// The file-cache options selecting the bound provider, storage directory, and layout.
    /// </param>
    /// <param name="timeProvider">
    /// The time source the swallowed-failure warning rate-limiting is measured against, or <see langword="null" /> to
    /// use <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a rate-limited warning when a best-effort storage failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected FileRateCacheBase(TOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options)
    {
        _warnGate = new RateLimitedWarningGate(timeProvider, RateLimitedWarningGate.DefaultCooldown);
        _logger = logger ?? NullLogger.Instance;
        _directory = string.IsNullOrWhiteSpace(options.CacheDirectory)
            ? Path.Combine(Path.GetTempPath(), "bodu-exchange-rates")
            : options.CacheDirectory!;
        _layout = Options.Layout;

        if (Options.ValidateStorageOnStart)
        {
            // Eagerly create the cache root so a misconfigured or unwritable location surfaces at construction rather
            // than on the first write. The failure propagates by design; this mirrors the startup probe the SQLite and
            // distributed backends run under the same option. The per-pair directory depends on a pair the probe does
            // not have, so the root itself is the layout-agnostic location to validate.
            Directory.CreateDirectory(_directory);
        }
    }

    /// <inheritdoc />
    public string CacheDirectory => _directory;

    /// <summary>
    /// Gets a value indicating whether a caught storage failure should degrade to a best-effort fallback rather than
    /// propagate. Used as the exception filter on the read and write catch blocks so a strict cache fails fast.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="RateCacheOptions.ThrowOnStorageFailure" /> is not set; otherwise
    /// <see langword="false" />, so the failure propagates.
    /// </value>
    private bool ShouldSwallowStorageFailure => !Options.ThrowOnStorageFailure;

    /// <summary>
    /// Gets the file extension, including the leading period, applied to cached rate files.
    /// </summary>
    /// <value>The file extension used by the serialization format, for example <c>.toml</c>.</value>
    protected abstract string FileExtension { get; }

    /// <inheritdoc />
    public string ResolveFilePath(CurrencyPair pair)
    {
        if (_layout.IsPartitioned)
            throw new InvalidOperationException(CachingResourceStrings.Op_Invalid_PartitionedNoSingleFile);

        return SingleFilePath(pair);
    }

    /// <inheritdoc />
    public string ResolveDirectory(CurrencyPair pair) =>
        _layout.ResolveDirectory(_directory, Provider, pair);

    /// <inheritdoc />
    public string ResolvePartitionPath(CurrencyPair pair, DateOnly date)
    {
        string key = _layout.PartitionStrategy.GetPartitionKey(date);
        return Path.Combine(ResolveDirectory(pair), _layout.ResolveFileName(Provider, pair, key, FileExtension));
    }

    /// <inheritdoc />
    internal sealed override CachePairState ReadState(CurrencyPair pair)
    {
        try
        {
            return _layout.IsPartitioned ? ReadPartitioned(pair) : ReadSingle(pair);
        }
        catch (IOException ex) when (ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return CachePairState.Empty;
        }
        catch (UnauthorizedAccessException ex) when (ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return CachePairState.Empty;
        }
    }

    /// <inheritdoc />
    internal sealed override bool WriteState(CurrencyPair pair, CachePairState state)
    {
        try
        {
            if (_layout.IsPartitioned)
                WritePartitioned(pair, state);
            else
                WriteSingle(pair, state);

            return true;
        }
        catch (IOException ex) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a failed write must not break rate retrieval. Report the failure so the caller can
            // refetch rather than trust coverage that was never persisted. When ThrowOnStorageFailure is set the
            // failure propagates instead.
            OnStorageFailureSwallowed("store", ex);
            return false;
        }
        catch (UnauthorizedAccessException ex) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: see above.
            OnStorageFailureSwallowed("store", ex);
            return false;
        }
    }

    /// <summary>
    /// Serializes the supplied state to the cache file's text format.
    /// </summary>
    /// <param name="pair">The currency pair the state belongs to, so the format can record it in the file body.</param>
    /// <param name="state">The per-pair state — cached rows and coverage windows — to persist.</param>
    /// <returns>The serialized text to write to the cache file.</returns>
    /// <remarks>
    /// The pair is supplied so a format can embed the provider and currency pair in the file body, making each file
    /// self-describing rather than identified solely by its name and directory. Declared
    /// <see langword="private protected" /> because <see cref="CachePairState" /> is an internal storage detail shared
    /// only with same-assembly backends.
    /// </remarks>
    private protected abstract string Serialize(CurrencyPair pair, CachePairState state);

    /// <summary>
    /// Deserializes the cache file's text into per-pair state, returning <see cref="CachePairState.Empty" /> when the
    /// content is malformed.
    /// </summary>
    /// <param name="text">The file text to parse.</param>
    /// <param name="path">The full path of the file the text was read from, used for diagnostics.</param>
    /// <returns>The parsed state, or <see cref="CachePairState.Empty" /> when the content cannot be parsed.</returns>
    /// <remarks>
    /// Declared <see langword="private protected" /> because <see cref="CachePairState" /> is an internal storage
    /// detail shared only with same-assembly backends.
    /// </remarks>
    private protected abstract CachePairState Deserialize(string text, string path);

    /// <summary>
    /// Resolves the single backing file path for a pair under a non-partitioned layout.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The full path of the pair's file.</returns>
    private string SingleFilePath(CurrencyPair pair) =>
        Path.Combine(_layout.ResolveDirectory(_directory, Provider, pair), _layout.ResolveFileName(Provider, pair, string.Empty, FileExtension));

    /// <summary>
    /// Reads the single backing file for a pair under a non-partitioned layout, serving a memoized parse when the file
    /// is unchanged.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The parsed state, or <see cref="CachePairState.Empty" /> when no file exists.</returns>
    private CachePairState ReadSingle(CurrencyPair pair) =>
        ReadFile(SingleFilePath(pair));

    /// <summary>
    /// Reads and merges every partition file for a pair under a partitioned layout.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>
    /// The merged state across all partitions, or <see cref="CachePairState.Empty" /> when none exists.
    /// </returns>
    private CachePairState ReadPartitioned(CurrencyPair pair)
    {
        string directory = ResolveDirectory(pair);
        if (!Directory.Exists(directory))
            return CachePairState.Empty;

        List<CachedRate> entries = new();
        List<CoverageWindow> coverage = new();

        foreach (string path in EnumeratePartitionFiles(directory))
        {
            CachePairState state = ReadFile(path);
            entries.AddRange(state.Entries);
            coverage.AddRange(state.Coverage);
        }

        if (entries.Count == 0 && coverage.Count == 0)
            return CachePairState.Empty;

        // Each partition holds disjoint dates and the shared rules re-merge coverage on use, so raw concatenation is
        // sufficient here; sorting and de-duplication are the caller's responsibility through RateCacheRules.
        return new CachePairState(entries, coverage);
    }

    /// <summary>
    /// Reads and deserializes one file, serving a memoized parse when the file's last-write instant is unchanged.
    /// </summary>
    /// <param name="path">The full path of the file to read.</param>
    /// <returns>The parsed state, or <see cref="CachePairState.Empty" /> when the file does not exist.</returns>
    private CachePairState ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            _parsed.TryRemove(path, out _);
            return CachePairState.Empty;
        }

        DateTime stamp = File.GetLastWriteTimeUtc(path);
        if (_parsed.TryGetValue(path, out (DateTime StampUtc, CachePairState State) memo) && memo.StampUtc == stamp)
            return memo.State;

        string text = File.ReadAllText(path);
        CachePairState state = Deserialize(text, path);

        // Memoize the parse against the file's last-write instant. A later write (here or in another process) moves the
        // timestamp, so the next read misses this entry and re-parses the fresh file.
        _parsed[path] = (stamp, state);
        return state;
    }

    /// <summary>
    /// Writes the single backing file for a pair under a non-partitioned layout, deleting it when the state is empty.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="state">The state to persist.</param>
    private void WriteSingle(CurrencyPair pair, CachePairState state)
    {
        string path = SingleFilePath(pair);

        // An entirely empty state has nothing worth persisting: delete the file so a stale, empty file is not left
        // behind, mirroring the in-memory cache that removes the pair entry.
        if (state.Entries.Count == 0 && state.Coverage.Count == 0)
        {
            DeleteFile(path);
            return;
        }

        WriteFile(path, Serialize(pair, state));
    }

    /// <summary>
    /// Writes a pair's state across one file per partition under a partitioned layout, reconciling the pair's folder so
    /// partition files that no longer carry any data are removed.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="state">The full state to distribute across partitions.</param>
    private void WritePartitioned(CurrencyPair pair, CachePairState state)
    {
        string directory = ResolveDirectory(pair);

        Dictionary<string, (List<CachedRate> Entries, List<CoverageWindow> Coverage)> buckets = BucketByPartition(state);

        // Map each partition key to the file it writes to, so a stale file can be detected by comparison below.
        Dictionary<string, string> desired = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, (List<CachedRate> Entries, List<CoverageWindow> Coverage)> bucket in buckets)
            desired[Path.Combine(directory, _layout.ResolveFileName(Provider, pair, bucket.Key, FileExtension))] = bucket.Key;

        // Delete partition files no longer represented in the new state — a partition pruned to empty must not leave a
        // stale file behind, the same way a single-file write deletes an emptied file.
        if (Directory.Exists(directory))
        {
            foreach (string existing in EnumeratePartitionFiles(directory))
            {
                if (!desired.ContainsKey(existing))
                    DeleteFile(existing);
            }
        }

        foreach (KeyValuePair<string, string> target in desired)
        {
            (List<CachedRate> entries, List<CoverageWindow> coverage) = buckets[target.Value];
            WriteFile(target.Key, Serialize(pair, new CachePairState(entries, coverage)));
        }

        // A cleared pair leaves an empty folder behind; remove it best-effort so the tree does not accumulate empties.
        if (desired.Count == 0)
            TryRemoveEmptyDirectory(directory);
    }

    /// <summary>
    /// Buckets a pair's rows and coverage windows by partition key, splitting each coverage window at partition
    /// boundaries so a window that crosses a period is stored as one sub-window per partition.
    /// </summary>
    /// <param name="state">The full pair state to distribute.</param>
    /// <returns>The per-partition rows and coverage windows, keyed by partition key.</returns>
    private Dictionary<string, (List<CachedRate> Entries, List<CoverageWindow> Coverage)> BucketByPartition(CachePairState state)
    {
        RateCachePartitionStrategy strategy = _layout.PartitionStrategy;
        Dictionary<string, (List<CachedRate> Entries, List<CoverageWindow> Coverage)> buckets = new(StringComparer.Ordinal);

        foreach (CachedRate entry in state.Entries)
            Bucket(buckets, strategy.GetPartitionKey(entry.Date)).Entries.Add(entry);

        foreach (CoverageWindow window in state.Coverage)
        {
            DateOnly cursor = window.Start;
            while (true)
            {
                (DateOnly _, DateOnly partitionEnd) = strategy.GetPartitionRange(cursor);
                DateOnly segmentEnd = partitionEnd < window.End ? partitionEnd : window.End;

                Bucket(buckets, strategy.GetPartitionKey(cursor)).Coverage.Add(new CoverageWindow(cursor, segmentEnd, window.FetchedAtUtc));

                if (segmentEnd >= window.End || segmentEnd == DateOnly.MaxValue)
                    break;

                cursor = segmentEnd.AddDays(1);
            }
        }

        return buckets;
    }

    /// <summary>
    /// Returns the bucket for a partition key, creating it on first use.
    /// </summary>
    /// <param name="buckets">The bucket map.</param>
    /// <param name="key">The partition key.</param>
    /// <returns>The bucket's row and coverage lists.</returns>
    private static (List<CachedRate> Entries, List<CoverageWindow> Coverage) Bucket(
        Dictionary<string, (List<CachedRate> Entries, List<CoverageWindow> Coverage)> buckets,
        string key)
    {
        if (!buckets.TryGetValue(key, out (List<CachedRate> Entries, List<CoverageWindow> Coverage) bucket))
        {
            bucket = (new List<CachedRate>(), new List<CoverageWindow>());
            buckets[key] = bucket;
        }

        return bucket;
    }

    /// <summary>
    /// Enumerates the cache files in a pair's directory, matching only files with this format's extension and skipping
    /// the temporary files left by an in-flight atomic write.
    /// </summary>
    /// <param name="directory">The directory to enumerate.</param>
    /// <returns>The full paths of the matching files.</returns>
    private IEnumerable<string> EnumeratePartitionFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*" + FileExtension)
            .Where(path => string.Equals(Path.GetExtension(path), FileExtension, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Writes <paramref name="text" /> to <paramref name="path" /> atomically, creating any missing parent directory
    /// first and invalidating the parse memo for the path.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="text">The text to write.</param>
    private void WriteFile(string path, string text)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        AtomicFileWriter.Write(path, text);

        // Invalidate the parse memo so the next read re-parses from the freshly written file and re-stamps it.
        _parsed.TryRemove(path, out _);
    }

    /// <summary>
    /// Deletes a cache file if it exists and invalidates its parse memo.
    /// </summary>
    /// <param name="path">The file path to delete.</param>
    private void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);

        _parsed.TryRemove(path, out _);
    }

    /// <summary>
    /// Reports a cache file that failed to deserialize and was treated as empty, so corruption surfaces to operators.
    /// Called by the format-specific <see cref="Deserialize(string, string)" /> implementations.
    /// </summary>
    /// <param name="path">The full path of the corrupt cache file.</param>
    /// <param name="exception">The swallowed deserialization exception.</param>
    private protected void OnCacheFileCorrupt(string path, Exception exception) =>
        Log.CacheFileCorrupt(_logger, Provider, path, exception);

    /// <summary>
    /// Reports a swallowed best-effort storage failure to the logger at <see cref="LogLevel.Warning" />, rate-limited
    /// so at most one warning is emitted per <see cref="RateLimitedWarningGate.DefaultCooldown" /> window.
    /// </summary>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    /// <param name="exception">The swallowed storage exception.</param>
    /// <remarks>
    /// The first failure after construction, and the first after each cooldown elapses, is logged immediately and
    /// carries the count of failures suppressed since the previous warning; failures inside the window only increment
    /// that count. A single warning slot is claimed with
    /// <see cref="Interlocked.CompareExchange(ref long, long, long)" /> so that under concurrent swallows exactly one
    /// caller logs per window. The cooldown is measured against the injected <see cref="TimeProvider" /> so the
    /// rate-limiting is deterministic under test.
    /// </remarks>
    private void OnStorageFailureSwallowed(string operation, Exception exception)
    {
        if (_warnGate.TryClaimWarning(out int suppressed))
            Log.FileStorageFailureSwallowed(_logger, Provider, operation, suppressed, exception);
    }

    /// <summary>
    /// Removes a directory if it exists and is empty, swallowing file-system failures so cleanup never masks a write.
    /// </summary>
    /// <param name="directory">The directory to remove.</param>
    private void TryRemoveEmptyDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
        catch (IOException ex)
        {
            // Best-effort cleanup of the emptied pair directory.
            Log.CleanupFailureSwallowed(_logger, Provider, directory, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Best-effort cleanup of the emptied pair directory.
            Log.CleanupFailureSwallowed(_logger, Provider, directory, ex);
        }
    }
}
