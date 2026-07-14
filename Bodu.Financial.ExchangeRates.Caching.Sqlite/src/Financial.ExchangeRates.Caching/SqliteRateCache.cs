// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using Bodu.Caching;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> that persists a single provider's rates and fetch-coverage windows in a SQLite
/// database, expiring them through the same freshness mechanism as the in-memory and TOML caches.
/// </summary>
/// <remarks>
/// <para>
/// Rates and coverage live in two tables. The <c>rates</c> table is keyed by
/// <c>(provider, from_code, to_code, obs_date)</c>, one row per dated observation, written through an UPSERT so a
/// re-stored date replaces the prior row; its nullable <c>observed_at</c> column carries the upstream fetch instant
/// when the source supplied one. The <c>coverage</c> table records
/// <c>(provider, from_code, to_code, start_date, end_date, fetched_at)</c>, allowing multiple windows per pair so a
/// sparse fetch history is preserved exactly. Decimal rates are stored as invariant strings and all dates and instants
/// as invariant ISO text (a <see cref="DateOnly" /> as <c>yyyy-MM-dd</c>, a <see cref="DateTimeOffset" /> in round-trip
/// <c>"O"</c> form) so the full precision and scale round-trips losslessly, mirroring the TOML cache's string-decimal
/// choice.
/// </para>
/// <para>
/// Expiry is by caching duration rather than by storage: stale and semantically invalid rows are filtered on read and
/// pruned on write, and stale coverage windows are pruned when coverage is recorded, so the database self-cleans over
/// time. The freshness, validity, merge, and coverage rules are delegated to the shared <see cref="RateCacheRules" />
/// so this backend stays behaviourally identical to the in-memory, file, and distributed caches; this class contributes
/// only its SQLite storage and locking. The two halves of a pair's state are written independently through
/// <see cref="Store" /> and <see cref="RecordCoverage" /> — storing rates never drops recorded coverage, and recording
/// coverage never drops cached rows — while <see cref="StoreFetchedRange" /> writes both halves in one transaction.
/// </para>
/// <para>
/// The cache is a single-process best-effort store. Writes for the same pair are serialized under a per-pair lock and
/// run in a transaction so concurrent same-pair writes cannot lose either half, matching the file cache's guarantee. As
/// required by <see cref="IRateCache" />, a storage failure surfaces as an empty read or a skipped write rather than an
/// exception: <see cref="SqliteException" /> and <see cref="IOException" /> degrade gracefully, while argument
/// validation still throws.
/// </para>
/// <para>
/// So that this graceful degradation is not silent, each swallowed storage failure is logged at
/// <see cref="LogLevel.Warning" /> through the optional logger supplied at construction. The first failure is logged
/// immediately and subsequent failures are rate-limited to at most one warning per minute, each carrying the count of
/// failures suppressed since the previous warning, so a sustained outage is visible to operators without flooding the
/// log. With no logger the degradation is unreported; set <see cref="RateCacheOptions.ThrowOnStorageFailure" /> instead
/// when a failure must surface as an exception.
/// </para>
/// <para>
/// A single keep-alive connection is held open for the instance lifetime so that a shared in-memory database (
/// <c>Mode=Memory;Cache=Shared</c>) survives between operations, which would otherwise be torn down when the last
/// connection closes. Per-operation connections are still opened and closed normally and are pooled by
/// Microsoft.Data.Sqlite. Dispose the cache to release the keep-alive connection.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // A SQLite caching provider persisting to a file under the system temp path.
/// var options = new SqliteRateCacheOptions { Provider = "RBA", DatabaseFilePath = "/var/cache/rba.db" };
/// using var cache = new SqliteRateCache(options);
/// IDatedRateProvider cached = new CachingRateProvider(rba, cache, new CachingRateOptions());
///]]>
/// </code>
/// </example>
/// <example>
/// Several providers can share one database file: the leading <c>provider</c> key column keeps each provider's series
/// partitioned, and one cache covers all of that provider's currency pairs, so there is never a cache per pair.
/// <code language="csharp">
///<![CDATA[
/// var options = new CachingRateOptions { DefaultExpiry = TimeSpan.FromHours(24) };
///
/// // One shared .db file, one cache per provider; each cache covers all of that provider's pairs.
/// using var rbaCache = new SqliteRateCache("RBA", "/var/cache/fx.db");
/// using var ofxCache = new SqliteRateCache("OFX", "/var/cache/fx.db");
///
/// IDatedRateProvider rba = new CachingRateProvider(rbaSource, rbaCache, options);
/// IDatedRateProvider ofx = new CachingRateProvider(ofxSource, ofxCache, options);
///]]>
/// </code>
/// </example>
public sealed class SqliteRateCache
    : IRateCache, IDisposable
{
    /// <summary>The validated options carrying the bound provider and the database location.</summary>
    private readonly SqliteRateCacheOptions _options;

    /// <summary>The resolved connection string every connection is opened with.</summary>
    private readonly string _connectionString;

    /// <summary>The keep-alive connection held open for the instance lifetime so a shared in-memory database is not torn down between operations. Closed on <see cref="Dispose" />.</summary>
    private readonly SqliteConnection _keepAlive;

    /// <summary>The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" />, <see cref="RecordCoverage" />, and <see cref="StoreFetchedRange" />.</summary>
    private readonly StripedLockSet<CurrencyPair> _pairLocks = new();

    /// <summary>Tracks whether the instance has been disposed, as <c>0</c> for live and <c>1</c> for disposed. Stored as an <see cref="int" /> so <see cref="Interlocked.Exchange(ref int, int)" /> can make <see cref="Dispose" /> idempotent: only the first caller observes the transition and releases the keep-alive connection.</summary>
    private int _disposed;

    /// <summary>The logger that receives the rate-limited degradation warnings, or <see cref="NullLogger.Instance" /> when none was supplied.</summary>
    private readonly ILogger _logger;

    /// <summary>Rate-limits the degradation warning to at most one emission per <see cref="RateLimitedWarningGate.DefaultCooldown" /> window.</summary>
    private readonly RateLimitedWarningGate _warnGate;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteRateCache" /> class.
    /// </summary>
    /// <param name="options">The options carrying the bound provider and the database location.</param>
    /// <param name="timeProvider">
    /// The time source the degradation-warning cooldown is measured against, or <see langword="null" /> to use
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that receives the rate-limited best-effort degradation warnings, or <see langword="null" /> to leave
    /// the degradation unreported.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    /// <remarks>
    /// The schema is created if it does not already exist, and a pre-existing <c>rates</c> table is migrated to add the
    /// <c>observed_at</c> column when it is absent, in one transaction, when the instance is constructed. A failure to
    /// create or migrate the schema is swallowed — and logged at <see cref="LogLevel.Warning" /> through
    /// <paramref name="logger" /> — so a transiently unwritable database surfaces later as empty reads and skipped
    /// writes rather than a construction-time exception, unless <see cref="RateCacheOptions.ValidateStorageOnStart" />
    /// or <see cref="RateCacheOptions.ThrowOnStorageFailure" /> is set, in which case the failure propagates from the
    /// constructor.
    /// </remarks>
    public SqliteRateCache(SqliteRateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _connectionString = options.ResolveConnectionString();
        _warnGate = new RateLimitedWarningGate(timeProvider, RateLimitedWarningGate.DefaultCooldown);
        _logger = logger ?? NullLogger.Instance;

        // Hold one connection open for the instance lifetime so a shared in-memory database is not destroyed between
        // operations. For a file database this is a harmless idle handle.
        _keepAlive = new SqliteConnection(_connectionString);

        try
        {
            _keepAlive.Open();
            ConfigureConnection(_keepAlive);
            if (_options.UseWriteAheadLogging)
                ApplyWriteAheadLogging(_keepAlive);
            EnsureSchema(_keepAlive);
        }
        catch (SqliteException ex) when (!_options.ValidateStorageOnStart && !_options.ThrowOnStorageFailure)
        {
            // Best-effort cache: a database that cannot be opened or initialized now degrades to empty reads and
            // skipped writes rather than failing construction. When either strict flag is set the failure propagates
            // from the constructor instead.
            OnStorageFailureSwallowed("schema initialization", ex);
        }
        catch (IOException ex) when (!_options.ValidateStorageOnStart && !_options.ThrowOnStorageFailure)
        {
            // Best-effort cache: see above.
            OnStorageFailureSwallowed("schema initialization", ex);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteRateCache" /> class bound to a provider and a database file.
    /// </summary>
    /// <param name="provider">The provider the cache stores rates for.</param>
    /// <param name="databaseFilePath">The path to the SQLite database file used by the cache.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> or <paramref name="databaseFilePath" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="provider" /> or <paramref name="databaseFilePath" /> is empty or white space.
    /// </exception>
    public SqliteRateCache(string provider, string databaseFilePath)
        : this(new SqliteRateCacheOptions { Provider = provider, DatabaseFilePath = databaseFilePath })
    {
    }

    /// <inheritdoc />
    public string Provider => _options.Provider;

    /// <summary>
    /// Gets a value indicating whether a caught storage failure should degrade to a best-effort fallback rather than
    /// propagate. Used as the exception filter on the read and write catch blocks so a strict cache fails fast.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="RateCacheOptions.ThrowOnStorageFailure" /> is not set; otherwise
    /// <see langword="false" />, so the failure propagates.
    /// </value>
    private bool ShouldSwallowStorageFailure => !_options.ThrowOnStorageFailure;

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
        // The counter increments on every swallow; only the log volume is rate-limited by the gate below.
        CachingSqliteMeter.StorageFailure(_options.Provider, operation);

        if (_warnGate.TryClaimWarning(out int suppressed))
            Log.StorageFailureSwallowed(_logger, _options.Provider, operation, suppressed, exception);
    }

    /// <inheritdoc />
    public IReadOnlyList<CachedRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedRate> entries = ReadEntries(pair);
        if (entries.Count == 0)
            return Array.Empty<CachedRate>();

        // The rows are materialized fresh per read, date-ordered by the query, so the all-fresh fast path serves them
        // without the extra filtered copy SelectFresh would build.
        return RateCacheRules.IsAllFreshOrdered(entries, duration, asOf)
            ? entries
            : RateCacheRules.SelectFresh(entries, duration, asOf);
    }

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        lock (LockFor(pair))
        {
            try
            {
                // One connection and one transaction span the read-merge-write, so the operation costs a single
                // pooled open instead of two and the read sees the state the write replaces.
                using SqliteConnection connection = OpenConnection();
                using SqliteTransaction transaction = connection.BeginTransaction();

                List<CachedRate> ordered = RateCacheRules.MergeRows(ReadEntries(connection, transaction, pair), rates, duration, asOf);

                // Replace only the entries half; the coverage half is left untouched so storing rows never drops coverage.
                ReplaceEntries(connection, transaction, pair, ordered);

                transaction.Commit();
            }
            catch (Exception ex) when (ex is SqliteException or IOException && ShouldSwallowStorageFailure)
            {
                // Best-effort cache: a failed write must not break rate retrieval.
                OnStorageFailureSwallowed("store", ex);
            }
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        RateCacheRules.BuildCoverage(ReadCoverage(pair), duration, asOf);

    /// <inheritdoc />
    public void RecordCoverage(CurrencyPair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            try
            {
                // One connection and one transaction span the read-merge-write; see Store for the rationale.
                using SqliteConnection connection = OpenConnection();
                using SqliteTransaction transaction = connection.BeginTransaction();

                List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                    RateCacheRules.MergeCoverage(ReadCoverage(connection, transaction, pair), start, end, duration, asOf);

                // Replace only the coverage half; the entries half is left untouched so recording coverage never drops rows.
                ReplaceCoverage(connection, transaction, pair, windows);

                transaction.Commit();
            }
            catch (Exception ex) when (ex is SqliteException or IOException && ShouldSwallowStorageFailure)
            {
                // Best-effort cache: a failed write must not break rate retrieval.
                OnStorageFailureSwallowed("coverage record", ex);
            }
        }
    }

    /// <inheritdoc />
    public RateCacheWriteStatus StoreFetchedRange(
        CurrencyPair pair,
        IReadOnlyList<CachedRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            try
            {
                // One connection and one transaction span both reads, both merges, and both replaces, so the whole
                // range store costs a single pooled open (previously three) and a reader never observes coverage
                // without its rows: success persists both halves, failure persists neither.
                using SqliteConnection connection = OpenConnection();
                using SqliteTransaction transaction = connection.BeginTransaction();

                List<CachedRate> ordered = RateCacheRules.MergeRows(ReadEntries(connection, transaction, pair), rows, duration, asOf);
                List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                    RateCacheRules.MergeCoverage(ReadCoverage(connection, transaction, pair), start, end, duration, asOf);

                ReplaceEntries(connection, transaction, pair, ordered);
                ReplaceCoverage(connection, transaction, pair, windows);

                transaction.Commit();
                return RateCacheWriteStatus.Stored;
            }
            catch (Exception ex) when (ex is SqliteException or IOException && ShouldSwallowStorageFailure)
            {
                // Best-effort cache: a failed write must not break rate retrieval. Report the failure so the caller
                // can refetch rather than trust coverage that was never persisted.
                OnStorageFailureSwallowed("range store", ex);
                return RateCacheWriteStatus.Failed;
            }
        }
    }

    /// <summary>
    /// Releases the keep-alive connection held for the instance lifetime.
    /// </summary>
    /// <remarks>
    /// Idempotent: a second or concurrent call is a safe no-op. The disposed flag is flipped with
    /// <see cref="Interlocked.Exchange(ref int, int)" /> so exactly one caller wins the transition and disposes the
    /// keep-alive connection, preventing a double dispose of the underlying handle.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _keepAlive.Dispose();
    }

    /// <summary>
    /// Creates the <c>rates</c> and <c>coverage</c> tables if they do not already exist, then brings a pre-existing
    /// <c>rates</c> table up to the current schema by adding the <c>observed_at</c> column when it is absent.
    /// </summary>
    /// <param name="connection">An open connection to run the schema statements on.</param>
    /// <remarks>
    /// The whole sequence runs in one transaction. A freshly created table already carries <c>observed_at</c> from the
    /// <c>CREATE</c>; a table created by a pre-C build lacks it, so <c>PRAGMA table_info(rates)</c> probes for the
    /// column and a single <c>ALTER TABLE ... ADD COLUMN</c> adds it as nullable, preserving every existing row with a
    /// <see langword="null" /> upstream fetch instant. The migration is best-effort like the rest of construction: a
    /// failure is swallowed by the caller so a transiently unwritable database degrades to empty reads and skipped
    /// writes rather than throwing.
    /// </remarks>
    private static void EnsureSchema(SqliteConnection connection)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS rates (
                    provider    TEXT NOT NULL,
                    from_code   TEXT NOT NULL,
                    to_code     TEXT NOT NULL,
                    obs_date    TEXT NOT NULL,
                    rate        TEXT NOT NULL,
                    cached_at   TEXT NOT NULL,
                    observed_at TEXT NULL,
                    PRIMARY KEY (provider, from_code, to_code, obs_date)
                );

                CREATE TABLE IF NOT EXISTS coverage (
                    provider    TEXT NOT NULL,
                    from_code   TEXT NOT NULL,
                    to_code     TEXT NOT NULL,
                    start_date  TEXT NOT NULL,
                    end_date    TEXT NOT NULL,
                    fetched_at  TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_coverage_pair ON coverage (provider, from_code, to_code);
                """;
            command.ExecuteNonQuery();
        }

        // A rates table created by a pre-C build lacks the observed_at column, so CREATE TABLE IF NOT EXISTS leaves it
        // untouched. Probe for the column and add it as nullable when it is absent, preserving every existing row with a
        // null upstream fetch instant. A freshly created table already carries observed_at from the CREATE above.
        if (!RatesTableHasObservedAt(connection, transaction))
        {
            using SqliteCommand alter = connection.CreateCommand();
            alter.Transaction = transaction;
            alter.CommandText = "ALTER TABLE rates ADD COLUMN observed_at TEXT NULL;";
            alter.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>
    /// Determines whether the <c>rates</c> table already carries the <c>observed_at</c> column.
    /// </summary>
    /// <param name="connection">An open connection to probe the schema on.</param>
    /// <param name="transaction">The transaction the probe participates in.</param>
    /// <returns>
    /// <see langword="true" /> when the <c>rates</c> table declares an <c>observed_at</c> column; otherwise
    /// <see langword="false" />, indicating a pre-C table that still needs the migrating <c>ALTER TABLE</c>.
    /// </returns>
    /// <remarks>
    /// Reads <c>PRAGMA table_info(rates)</c>, whose second projected column is the column name, and matches
    /// <c>observed_at</c> ordinally.
    /// </remarks>
    private static bool RatesTableHasObservedAt(SqliteConnection connection, SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA table_info(rates);";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            // The second column of a table_info row is the column name.
            if (string.Equals(reader.GetString(1), "observed_at", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Reads the persisted rate rows for a pair, returning an empty list when none exist or the read fails.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored rows, unfiltered, or an empty list on failure.</returns>
    /// <remarks>
    /// Returns the raw stored rows without freshness filtering; the freshness policy is applied by the public surface.
    /// A malformed row that cannot be parsed is skipped so a single corrupt value never fails the whole read.
    /// </remarks>
    private IReadOnlyList<CachedRate> ReadEntries(CurrencyPair pair)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            return ReadEntries(connection, transaction: null, pair);
        }
        catch (SqliteException ex) when (ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<CachedRate>();
        }
        catch (IOException ex) when (ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<CachedRate>();
        }
    }

    /// <summary>
    /// Reads the persisted rate rows for a pair on an already open connection, optionally inside a transaction, so a
    /// write operation's read-merge-write sequence runs on a single connection.
    /// </summary>
    /// <param name="connection">The open connection to read on.</param>
    /// <param name="transaction">The transaction the read participates in, or <see langword="null" /> for none.</param>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored rows, unfiltered.</returns>
    /// <remarks>
    /// Storage failures propagate to the caller, which owns the connection and the degradation policy; a malformed row
    /// is still skipped here so a single corrupt value never fails the whole read.
    /// </remarks>
    private IReadOnlyList<CachedRate> ReadEntries(SqliteConnection connection, SqliteTransaction? transaction, CurrencyPair pair)
    {
        List<CachedRate> rows = new();

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT obs_date, rate, cached_at, observed_at FROM rates WHERE provider = $provider AND from_code = $from AND to_code = $to;";
        BindPair(command, pair);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                // A pre-C row, or a row whose source never supplied a fetch instant, stores observed_at as NULL and
                // reads back as a null ObservedAtUtc.
                DateTimeOffset? observedAt = reader.IsDBNull(3) ? (DateTimeOffset?)null : InvariantCacheText.ParseInstant(reader.GetString(3));
                rows.Add(new CachedRate(
                    InvariantCacheText.ParseDate(reader.GetString(0)),
                    InvariantCacheText.ParseDecimal(reader.GetString(1)),
                    InvariantCacheText.ParseInstant(reader.GetString(2)),
                    observedAt));
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                // Skip a single malformed row rather than failing the whole read. An out-of-range decimal rate
                // parses as an OverflowException, which must be swallowed alongside FormatException so a poisoned
                // value cannot break the documented best-effort read contract.
            }
        }

        return rows;
    }

    /// <summary>
    /// Replaces the persisted rate rows for a pair within an open transaction, deleting the existing rows and inserting
    /// <paramref name="entries" />.
    /// </summary>
    /// <param name="connection">The open connection the commands run on.</param>
    /// <param name="transaction">The transaction the delete and insert participate in.</param>
    /// <param name="pair">The currency pair whose rows are replaced.</param>
    /// <param name="entries">The rows to persist.</param>
    /// <remarks>
    /// Shared by <see cref="Store" /> and <see cref="StoreFetchedRange" /> so the rate-table statements are
    /// single-sourced; the caller owns the transaction lifetime and commits or rolls back.
    /// </remarks>
    private void ReplaceEntries(SqliteConnection connection, SqliteTransaction transaction, CurrencyPair pair, List<CachedRate> entries)
    {
        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText =
                "DELETE FROM rates WHERE provider = $provider AND from_code = $from AND to_code = $to;";
            BindPair(delete, pair);
            delete.ExecuteNonQuery();
        }

        if (entries.Count == 0)
            return;

        using SqliteCommand insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO rates (provider, from_code, to_code, obs_date, rate, cached_at, observed_at)
            VALUES ($provider, $from, $to, $date, $rate, $cached, $observed)
            ON CONFLICT (provider, from_code, to_code, obs_date)
            DO UPDATE SET rate = excluded.rate, cached_at = excluded.cached_at, observed_at = excluded.observed_at;
            """;
        SqliteParameter date = insert.Parameters.Add("$date", SqliteType.Text);
        SqliteParameter rate = insert.Parameters.Add("$rate", SqliteType.Text);
        SqliteParameter cached = insert.Parameters.Add("$cached", SqliteType.Text);
        SqliteParameter observed = insert.Parameters.Add("$observed", SqliteType.Text);
        BindPair(insert, pair);

        foreach (CachedRate entry in entries)
        {
            date.Value = InvariantCacheText.FormatDate(entry.Date);
            rate.Value = InvariantCacheText.FormatDecimal(entry.Rate);
            cached.Value = InvariantCacheText.FormatInstant(entry.CachedAtUtc);
            observed.Value = entry.ObservedAtUtc is { } o ? InvariantCacheText.FormatInstant(o) : (object)DBNull.Value;
            insert.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Reads the persisted coverage windows for a pair, returning an empty list when none exist or the read fails.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored windows, unfiltered, or an empty list on failure.</returns>
    /// <remarks>
    /// Returns the raw stored windows without freshness filtering; the freshness policy is applied by the public
    /// surface. A malformed window that cannot be parsed is skipped so a single corrupt value never fails the whole
    /// read.
    /// </remarks>
    private IReadOnlyList<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> ReadCoverage(CurrencyPair pair)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            return ReadCoverage(connection, transaction: null, pair);
        }
        catch (SqliteException ex) when (ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<(DateOnly, DateOnly, DateTimeOffset)>();
        }
        catch (IOException ex) when (ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<(DateOnly, DateOnly, DateTimeOffset)>();
        }
    }

    /// <summary>
    /// Reads the persisted coverage windows for a pair on an already open connection, optionally inside a transaction,
    /// so a write operation's read-merge-write sequence runs on a single connection.
    /// </summary>
    /// <param name="connection">The open connection to read on.</param>
    /// <param name="transaction">The transaction the read participates in, or <see langword="null" /> for none.</param>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored windows, unfiltered.</returns>
    /// <remarks>
    /// Storage failures propagate to the caller, which owns the connection and the degradation policy; a malformed
    /// window is still skipped here so a single corrupt value never fails the whole read.
    /// </remarks>
    private IReadOnlyList<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> ReadCoverage(SqliteConnection connection, SqliteTransaction? transaction, CurrencyPair pair)
    {
        List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> windows = new();

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT start_date, end_date, fetched_at FROM coverage WHERE provider = $provider AND from_code = $from AND to_code = $to;";
        BindPair(command, pair);

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                windows.Add((
                    InvariantCacheText.ParseDate(reader.GetString(0)),
                    InvariantCacheText.ParseDate(reader.GetString(1)),
                    InvariantCacheText.ParseInstant(reader.GetString(2))));
            }
            catch (FormatException)
            {
                // Skip a single malformed window rather than failing the whole read.
            }
        }

        return windows;
    }

    /// <summary>
    /// Replaces the persisted coverage windows for a pair within an open transaction, deleting the existing windows and
    /// inserting <paramref name="windows" />.
    /// </summary>
    /// <param name="connection">The open connection the commands run on.</param>
    /// <param name="transaction">The transaction the delete and insert participate in.</param>
    /// <param name="pair">The currency pair whose windows are replaced.</param>
    /// <param name="windows">The windows to persist.</param>
    /// <remarks>
    /// Shared by <see cref="RecordCoverage" /> and <see cref="StoreFetchedRange" /> so the coverage-table statements
    /// are single-sourced; the caller owns the transaction lifetime and commits or rolls back.
    /// </remarks>
    private void ReplaceCoverage(SqliteConnection connection, SqliteTransaction transaction, CurrencyPair pair, List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows)
    {
        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText =
                "DELETE FROM coverage WHERE provider = $provider AND from_code = $from AND to_code = $to;";
            BindPair(delete, pair);
            delete.ExecuteNonQuery();
        }

        if (windows.Count == 0)
            return;

        using SqliteCommand insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO coverage (provider, from_code, to_code, start_date, end_date, fetched_at)
            VALUES ($provider, $from, $to, $start, $end, $fetched);
            """;
        SqliteParameter start = insert.Parameters.Add("$start", SqliteType.Text);
        SqliteParameter end = insert.Parameters.Add("$end", SqliteType.Text);
        SqliteParameter fetched = insert.Parameters.Add("$fetched", SqliteType.Text);
        BindPair(insert, pair);

        foreach ((DateOnly windowStart, DateOnly windowEnd, DateTimeOffset fetchedAt) in windows)
        {
            start.Value = InvariantCacheText.FormatDate(windowStart);
            end.Value = InvariantCacheText.FormatDate(windowEnd);
            fetched.Value = InvariantCacheText.FormatInstant(fetchedAt);
            insert.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Binds the provider and the pair's currency codes onto a command's parameters.
    /// </summary>
    /// <param name="command">The command to bind onto.</param>
    /// <param name="pair">The currency pair supplying the codes.</param>
    private void BindPair(SqliteCommand command, CurrencyPair pair)
    {
        command.Parameters.AddWithValue("$provider", _options.Provider);
        command.Parameters.AddWithValue("$from", pair.From.ToString());
        command.Parameters.AddWithValue("$to", pair.To.ToString());
    }

    /// <summary>
    /// Opens a connection to the cache database with the resolved connection string.
    /// </summary>
    /// <returns>An open connection the caller owns and disposes.</returns>
    private SqliteConnection OpenConnection()
    {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        ConfigureConnection(connection);
        return connection;
    }

    /// <summary>
    /// Applies the per-connection concurrency setting — the <c>busy_timeout</c> wait — to a freshly opened connection.
    /// </summary>
    /// <param name="connection">The open connection to configure.</param>
    /// <remarks>
    /// The <c>busy_timeout</c> pragma is per-connection, so it is set on every open. Write-ahead logging is
    /// deliberately not re-asserted here: <c>journal_mode = WAL</c> is a persistent database-level property, so it is
    /// applied once to the keep-alive connection at construction (see <see cref="ApplyWriteAheadLogging" />) and every
    /// later per-operation connection inherits it, saving a PRAGMA round-trip per open. The one behaviour change is
    /// deliberate: an external process that flips the database's journal mode between operations is no longer
    /// corrected until the next construction — acceptable for a best-effort cache that always treated WAL as advisory.
    /// </remarks>
    private void ConfigureConnection(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = string.Format(
            CultureInfo.InvariantCulture,
            "PRAGMA busy_timeout = {0};",
            (long)_options.BusyTimeout.TotalMilliseconds);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Enables write-ahead logging on the database through an open connection, once at construction.
    /// </summary>
    /// <param name="connection">The open keep-alive connection.</param>
    /// <remarks>
    /// Best-effort: a database that cannot honor the mode — notably an in-memory database, which reports back its
    /// native mode — is left unchanged rather than failing. Runs outside any transaction so the journal-mode change is
    /// permitted.
    /// </remarks>
    private static void ApplyWriteAheadLogging(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns the lock object guarding writes for the supplied pair, creating it on first use.
    /// </summary>
    /// <param name="pair">The currency pair whose write lock is required.</param>
    /// <returns>The per-pair lock object.</returns>
    private object LockFor(CurrencyPair pair) =>
        _pairLocks.For(pair);
}
