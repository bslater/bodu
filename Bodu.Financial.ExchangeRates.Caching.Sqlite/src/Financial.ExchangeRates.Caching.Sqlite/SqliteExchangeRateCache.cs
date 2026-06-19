// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// An <see cref="IExchangeRateCache" /> that persists a single provider's rates and fetch-coverage windows in a SQLite
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
/// time. The freshness, validity, merge, and coverage rules are delegated to the shared
/// <see cref="ExchangeRateCacheRules" /> so this backend stays behaviourally identical to the in-memory, file, and
/// distributed caches; this class contributes only its SQLite storage and locking. The two halves of a pair's state are
/// written independently through <see cref="Store" /> and <see cref="RecordCoverage" /> — storing rates never drops
/// recorded coverage, and recording coverage never drops cached rows — while <see cref="StoreFetchedRange" /> writes
/// both halves in one transaction.
/// </para>
/// <para>
/// The cache is a single-process best-effort store. Writes for the same pair are serialized under a per-pair lock and
/// run in a transaction so concurrent same-pair writes cannot lose either half, matching the file cache's guarantee. As
/// required by <see cref="IExchangeRateCache" />, a storage failure surfaces as an empty read or a skipped write rather
/// than an exception: <see cref="SqliteException" /> and <see cref="IOException" /> degrade gracefully, while argument
/// validation still throws.
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
/// var options = new SqliteExchangeRateCacheOptions { Provider = "RBA", DatabaseFilePath = "/var/cache/rba.db" };
/// using var cache = new SqliteExchangeRateCache(options);
/// IDatedExchangeRateProvider cached = new CachingExchangeRateProvider(rba, cache, new CachingExchangeRateOptions());
///]]>
/// </code>
/// </example>
public sealed class SqliteExchangeRateCache
    : IExchangeRateCache, IDisposable
{
    /// <summary>The validated options carrying the bound provider and the database location.</summary>
    private readonly SqliteExchangeRateCacheOptions _options;

    /// <summary>The resolved connection string every connection is opened with.</summary>
    private readonly string _connectionString;

    /// <summary>The keep-alive connection held open for the instance lifetime so a shared in-memory database is not torn down between operations. Closed on <see cref="Dispose" />.</summary>
    private readonly SqliteConnection _keepAlive;

    /// <summary>The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" />, <see cref="RecordCoverage" />, and <see cref="StoreFetchedRange" />. One lock object is created per pair on first use and reused thereafter.</summary>
    private readonly ConcurrentDictionary<ExchangeRatePair, object> _pairLocks = new();

    /// <summary>Tracks whether the instance has been disposed, as <c>0</c> for live and <c>1</c> for disposed. Stored as an <see cref="int" /> so <see cref="Interlocked.Exchange(ref int, int)" /> can make <see cref="Dispose" /> idempotent: only the first caller observes the transition and releases the keep-alive connection.</summary>
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteExchangeRateCache" /> class.
    /// </summary>
    /// <param name="options">The options carrying the bound provider and the database location.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    /// <remarks>
    /// The schema is created if it does not already exist, and a pre-existing <c>rates</c> table is migrated to add the
    /// <c>observed_at</c> column when it is absent, in one transaction, when the instance is constructed. A failure to
    /// create or migrate the schema is swallowed so a transiently unwritable database surfaces later as empty reads and
    /// skipped writes rather than a construction-time exception, unless
    /// <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> or
    /// <see cref="ExchangeRateCacheOptions.ThrowOnStorageFailure" /> is set, in which case the failure propagates from
    /// the constructor.
    /// </remarks>
    public SqliteExchangeRateCache(SqliteExchangeRateCacheOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _connectionString = options.ResolveConnectionString();

        // Hold one connection open for the instance lifetime so a shared in-memory database is not destroyed between
        // operations. For a file database this is a harmless idle handle.
        _keepAlive = new SqliteConnection(_connectionString);

        try
        {
            _keepAlive.Open();
            EnsureSchema(_keepAlive);
        }
        catch (SqliteException) when (!_options.ValidateStorageOnStart && !_options.ThrowOnStorageFailure)
        {
            // Best-effort cache: a database that cannot be opened or initialized now degrades to empty reads and
            // skipped writes rather than failing construction. When either strict flag is set the failure propagates
            // from the constructor instead.
        }
        catch (IOException) when (!_options.ValidateStorageOnStart && !_options.ThrowOnStorageFailure)
        {
            // Best-effort cache: see above.
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteExchangeRateCache" /> class bound to a provider and a
    /// database file.
    /// </summary>
    /// <param name="provider">The provider the cache stores rates for.</param>
    /// <param name="databaseFilePath">The path to the SQLite database file used by the cache.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> or <paramref name="databaseFilePath" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="provider" /> or <paramref name="databaseFilePath" /> is empty or white space.
    /// </exception>
    public SqliteExchangeRateCache(string provider, string databaseFilePath)
        : this(new SqliteExchangeRateCacheOptions { Provider = provider, DatabaseFilePath = databaseFilePath })
    {
    }

    /// <inheritdoc />
    public string Provider => _options.Provider;

    /// <summary>
    /// Gets a value indicating whether a caught storage failure should degrade to a best-effort fallback rather than
    /// propagate. Used as the exception filter on the read and write catch blocks so a strict cache fails fast.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when <see cref="ExchangeRateCacheOptions.ThrowOnStorageFailure" /> is not set; otherwise
    /// <see langword="false" />, so the failure propagates.
    /// </returns>
    private bool ShouldSwallowStorageFailure => !_options.ThrowOnStorageFailure;

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedExchangeRate> entries = ReadEntries(pair);
        if (entries.Count == 0)
            return Array.Empty<CachedExchangeRate>();

        return ExchangeRateCacheRules.SelectFresh(entries, duration, asOf);
    }

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        lock (LockFor(pair))
        {
            List<CachedExchangeRate> ordered = ExchangeRateCacheRules.MergeRows(ReadEntries(pair), rates, duration, asOf);

            // Replace only the entries half; the coverage half is left untouched so storing rows never drops coverage.
            WriteEntries(pair, ordered);
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf) =>
        ExchangeRateCacheRules.BuildCoverage(ReadCoverage(pair), duration, asOf);

    /// <inheritdoc />
    public void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                ExchangeRateCacheRules.MergeCoverage(ReadCoverage(pair), start, end, duration, asOf);

            // Replace only the coverage half; the entries half is left untouched so recording coverage never drops rows.
            WriteCoverage(pair, windows);
        }
    }

    /// <inheritdoc />
    public ExchangeRateCacheWriteStatus StoreFetchedRange(
        ExchangeRatePair pair,
        IReadOnlyList<CachedExchangeRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            // Merge both halves first, then replace them in one transaction so a reader never observes coverage without
            // its rows. Both tables are rewritten together: success persists both halves, failure persists neither.
            List<CachedExchangeRate> ordered = ExchangeRateCacheRules.MergeRows(ReadEntries(pair), rows, duration, asOf);
            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows =
                ExchangeRateCacheRules.MergeCoverage(ReadCoverage(pair), start, end, duration, asOf);

            return WritePairState(pair, ordered, windows)
                ? ExchangeRateCacheWriteStatus.Stored
                : ExchangeRateCacheWriteStatus.Failed;
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

        // Bring a pre-C rates table up to the current schema: a table created before observed_at was added is otherwise
        // missing the column the INSERT and SELECT reference. Probe with PRAGMA table_info and add it once if absent.
        if (!HasObservedColumn(connection, transaction))
        {
            using SqliteCommand alter = connection.CreateCommand();
            alter.Transaction = transaction;
            alter.CommandText = "ALTER TABLE rates ADD COLUMN observed_at TEXT NULL;";
            alter.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>
    /// Reports whether the <c>rates</c> table already declares the <c>observed_at</c> column.
    /// </summary>
    /// <param name="connection">The open connection the probe runs on.</param>
    /// <param name="transaction">The transaction the probe participates in.</param>
    /// <returns>
    /// <see langword="true" /> when <c>rates</c> carries an <c>observed_at</c> column; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The <c>PRAGMA table_info(rates)</c> reader is fully read and closed before the caller issues the conditional
    /// <c>ALTER</c>, so the schema change does not run while a reader is open over the same table.
    /// </remarks>
    private static bool HasObservedColumn(SqliteConnection connection, SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA table_info(rates);";

        using SqliteDataReader reader = command.ExecuteReader();
        int nameOrdinal = reader.GetOrdinal("name");
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(nameOrdinal), "observed_at", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Formats a <see cref="DateOnly" /> as invariant <c>yyyy-MM-dd</c> text for storage.
    /// </summary>
    /// <param name="value">The date to format.</param>
    /// <returns>The invariant ISO date text.</returns>
    private static string FormatDate(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant <c>yyyy-MM-dd</c> text back into a <see cref="DateOnly" />.
    /// </summary>
    /// <param name="text">The stored date text.</param>
    /// <returns>The parsed date.</returns>
    private static DateOnly ParseDate(string text) =>
        DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a <see cref="DateTimeOffset" /> as invariant round-trip (<c>"O"</c>) text for storage.
    /// </summary>
    /// <param name="value">The instant to format.</param>
    /// <returns>The invariant round-trip text.</returns>
    private static string FormatInstant(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant round-trip (<c>"O"</c>) text back into a <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="text">The stored instant text.</param>
    /// <returns>The parsed instant.</returns>
    private static DateTimeOffset ParseInstant(string text) =>
        DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    /// <summary>
    /// Formats a decimal rate as invariant text for storage so its scale and precision round-trips losslessly.
    /// </summary>
    /// <param name="value">The rate to format.</param>
    /// <returns>The invariant decimal text.</returns>
    private static string FormatRate(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses invariant decimal text back into a rate.
    /// </summary>
    /// <param name="text">The stored decimal text.</param>
    /// <returns>The parsed rate.</returns>
    private static decimal ParseRate(string text) =>
        decimal.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads the persisted rate rows for a pair, returning an empty list when none exist or the read fails.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored rows, unfiltered, or an empty list on failure.</returns>
    /// <remarks>
    /// Returns the raw stored rows without freshness filtering; the freshness policy is applied by the public surface.
    /// A malformed row that cannot be parsed is skipped so a single corrupt value never fails the whole read.
    /// </remarks>
    private IReadOnlyList<CachedExchangeRate> ReadEntries(ExchangeRatePair pair)
    {
        List<CachedExchangeRate> rows = new();

        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
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
                    DateTimeOffset? observedAt = reader.IsDBNull(3) ? (DateTimeOffset?)null : ParseInstant(reader.GetString(3));
                    rows.Add(new CachedExchangeRate(
                        ParseDate(reader.GetString(0)),
                        ParseRate(reader.GetString(1)),
                        ParseInstant(reader.GetString(2)),
                        observedAt));
                }
                catch (FormatException)
                {
                    // Skip a single malformed row rather than failing the whole read.
                }
            }
        }
        catch (SqliteException) when (ShouldSwallowStorageFailure)
        {
            return Array.Empty<CachedExchangeRate>();
        }
        catch (IOException) when (ShouldSwallowStorageFailure)
        {
            return Array.Empty<CachedExchangeRate>();
        }

        return rows;
    }

    /// <summary>
    /// Replaces the persisted rate rows for a pair with <paramref name="entries" />, leaving the coverage half
    /// untouched.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="entries">The rows to persist.</param>
    /// <remarks>
    /// Runs as a single transaction so a reader never observes a half-replaced set. A storage failure is swallowed so a
    /// failed write does not break rate retrieval.
    /// </remarks>
    private void WriteEntries(ExchangeRatePair pair, List<CachedExchangeRate> entries)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            ReplaceEntries(connection, transaction, pair, entries);

            transaction.Commit();
        }
        catch (SqliteException) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (IOException) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
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
    /// Shared by <see cref="WriteEntries" /> and <see cref="WritePairState" /> so the rate-table statements are
    /// single-sourced; the caller owns the transaction lifetime and commits or rolls back.
    /// </remarks>
    private void ReplaceEntries(SqliteConnection connection, SqliteTransaction transaction, ExchangeRatePair pair, List<CachedExchangeRate> entries)
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

        foreach (CachedExchangeRate entry in entries)
        {
            date.Value = FormatDate(entry.Date);
            rate.Value = FormatRate(entry.Rate);
            cached.Value = FormatInstant(entry.CachedAtUtc);
            observed.Value = entry.ObservedAtUtc is { } o ? FormatInstant(o) : (object)DBNull.Value;
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
    private IReadOnlyList<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> ReadCoverage(ExchangeRatePair pair)
    {
        List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> windows = new();

        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT start_date, end_date, fetched_at FROM coverage WHERE provider = $provider AND from_code = $from AND to_code = $to;";
            BindPair(command, pair);

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    windows.Add((
                        ParseDate(reader.GetString(0)),
                        ParseDate(reader.GetString(1)),
                        ParseInstant(reader.GetString(2))));
                }
                catch (FormatException)
                {
                    // Skip a single malformed window rather than failing the whole read.
                }
            }
        }
        catch (SqliteException) when (ShouldSwallowStorageFailure)
        {
            return Array.Empty<(DateOnly, DateOnly, DateTimeOffset)>();
        }
        catch (IOException) when (ShouldSwallowStorageFailure)
        {
            return Array.Empty<(DateOnly, DateOnly, DateTimeOffset)>();
        }

        return windows;
    }

    /// <summary>
    /// Replaces the persisted coverage windows for a pair with <paramref name="windows" />, leaving the entries half
    /// untouched.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="windows">The windows to persist.</param>
    /// <remarks>
    /// Runs as a single transaction so a reader never observes a half-replaced set. A storage failure is swallowed so a
    /// failed write does not break rate retrieval.
    /// </remarks>
    private void WriteCoverage(ExchangeRatePair pair, List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            ReplaceCoverage(connection, transaction, pair, windows);

            transaction.Commit();
        }
        catch (SqliteException) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (IOException) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
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
    /// Shared by <see cref="WriteCoverage" /> and <see cref="WritePairState" /> so the coverage-table statements are
    /// single-sourced; the caller owns the transaction lifetime and commits or rolls back.
    /// </remarks>
    private void ReplaceCoverage(SqliteConnection connection, SqliteTransaction transaction, ExchangeRatePair pair, List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows)
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
            start.Value = FormatDate(windowStart);
            end.Value = FormatDate(windowEnd);
            fetched.Value = FormatInstant(fetchedAt);
            insert.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Replaces both the rate rows and the coverage windows for a pair in one transaction, so the two halves are
    /// persisted together or, on failure, neither is.
    /// </summary>
    /// <param name="pair">The currency pair whose state is replaced.</param>
    /// <param name="entries">The rows to persist.</param>
    /// <param name="windows">The coverage windows to persist.</param>
    /// <returns>
    /// <see langword="true" /> when the transaction committed; <see langword="false" /> when a storage error was
    /// swallowed and nothing was persisted.
    /// </returns>
    /// <remarks>
    /// Backs <see cref="StoreFetchedRange" />: rewriting both tables in a single transaction is what makes the
    /// rows-plus-coverage write atomic, closing the window in which coverage could be recorded without its rows.
    /// </remarks>
    private bool WritePairState(ExchangeRatePair pair, List<CachedExchangeRate> entries, List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)> windows)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            ReplaceEntries(connection, transaction, pair, entries);
            ReplaceCoverage(connection, transaction, pair, windows);

            transaction.Commit();
            return true;
        }
        catch (SqliteException) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: a failed write must not break rate retrieval. Report the failure so the caller can
            // refetch rather than trust coverage that was never persisted.
            return false;
        }
        catch (IOException) when (ShouldSwallowStorageFailure)
        {
            // Best-effort cache: see above.
            return false;
        }
    }

    /// <summary>
    /// Binds the provider and the pair's currency codes onto a command's parameters.
    /// </summary>
    /// <param name="command">The command to bind onto.</param>
    /// <param name="pair">The currency pair supplying the codes.</param>
    private void BindPair(SqliteCommand command, ExchangeRatePair pair)
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
        return connection;
    }

    /// <summary>
    /// Returns the lock object guarding writes for the supplied pair, creating it on first use.
    /// </summary>
    /// <param name="pair">The currency pair whose write lock is required.</param>
    /// <returns>The per-pair lock object.</returns>
    private object LockFor(ExchangeRatePair pair) =>
        _pairLocks.GetOrAdd(pair, static _ => new object());
}
