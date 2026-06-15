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
/// re-stored date replaces the prior row. The <c>coverage</c> table records
/// <c>(provider, from_code, to_code, start_date, end_date, fetched_at)</c>, allowing multiple windows per pair so a
/// sparse fetch history is preserved exactly. Decimal rates are stored as invariant strings and all dates and instants
/// as invariant ISO text (a <see cref="DateOnly" /> as <c>yyyy-MM-dd</c>, a <see cref="DateTimeOffset" /> in round-trip
/// <c>"O"</c> form) so the full precision and scale round-trips losslessly, mirroring the TOML cache's string-decimal
/// choice.
/// </para>
/// <para>
/// Expiry is by caching duration rather than by storage: stale and semantically invalid rows are filtered on read and
/// pruned on write, and stale coverage windows are pruned when coverage is recorded, so the database self-cleans over
/// time. The two halves of a pair's state are written independently — storing rates never drops recorded coverage, and
/// recording coverage never drops cached rows.
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
    /// <summary>
    /// The clock-skew tolerance applied when validating a row's caching instant: a row stamped more than this far in
    /// the future of the evaluation instant is treated as invalid rather than fresh.
    /// </summary>
    private static readonly TimeSpan s_clockSkewTolerance = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The validated options carrying the bound provider and the database location.
    /// </summary>
    private readonly SqliteExchangeRateCacheOptions _options;

    /// <summary>
    /// The resolved connection string every connection is opened with.
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// The keep-alive connection held open for the instance lifetime so a shared in-memory database is not torn down
    /// between operations. Closed on <see cref="Dispose" />.
    /// </summary>
    private readonly SqliteConnection _keepAlive;

    /// <summary>
    /// The striped per-pair locks guarding the read-modify-write sequences in <see cref="Store" /> and
    /// <see cref="RecordCoverage" />. One lock object is created per pair on first use and reused thereafter.
    /// </summary>
    private readonly ConcurrentDictionary<ExchangeRatePair, object> _pairLocks = new();

    /// <summary>
    /// Indicates whether the instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteExchangeRateCache" /> class.
    /// </summary>
    /// <param name="options">The options carrying the bound provider and the database location.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    /// <remarks>
    /// The schema is created if it does not already exist, in one transaction, when the instance is constructed. A
    /// failure to create the schema is swallowed so a transiently unwritable database surfaces later as empty reads and
    /// skipped writes rather than a construction-time exception.
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
        catch (SqliteException)
        {
            // Best-effort cache: a database that cannot be opened or initialized now degrades to empty reads and
            // skipped writes rather than failing construction.
        }
        catch (IOException)
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

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedExchangeRate> entries = ReadEntries(pair);
        if (entries.Count == 0)
            return Array.Empty<CachedExchangeRate>();

        List<CachedExchangeRate> fresh = new(entries.Count);
        foreach (CachedExchangeRate entry in entries)
        {
            if (IsValid(entry, asOf) && entry.IsFresh(asOf, duration))
                fresh.Add(entry);
        }

        fresh.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return fresh;
    }

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        lock (LockFor(pair))
        {
            IReadOnlyList<CachedExchangeRate> existing = ReadEntries(pair);

            // Merge with any existing entry so the most recently cached rate wins per date.
            Dictionary<DateOnly, CachedExchangeRate> merged = new();
            foreach (CachedExchangeRate row in existing)
                merged[row.Date] = row;

            foreach (CachedExchangeRate rate in rates)
            {
                if (!IsValid(rate, asOf))
                    continue;

                if (!merged.TryGetValue(rate.Date, out CachedExchangeRate current) || rate.CachedAtUtc >= current.CachedAtUtc)
                    merged[rate.Date] = rate;
            }

            // Prune rows that are no longer fresh or are semantically invalid, then order by date so the store is stable
            // and self-cleaning.
            List<CachedExchangeRate> ordered = new(merged.Count);
            foreach (CachedExchangeRate entry in merged.Values)
            {
                if (IsValid(entry, asOf) && entry.IsFresh(asOf, duration))
                    ordered.Add(entry);
            }

            ordered.Sort(static (left, right) => left.Date.CompareTo(right.Date));

            // Replace only the entries half; the coverage half is left untouched so storing rows never drops coverage.
            WriteEntries(pair, ordered);
        }
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        DateRangeCoverage coverage = new();
        foreach ((DateOnly start, DateOnly end, DateTimeOffset fetchedAt) in ReadCoverage(pair))
        {
            if (asOf - fetchedAt < duration)
                coverage.Add(start, end);
        }

        return coverage;
    }

    /// <inheritdoc />
    public void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        lock (LockFor(pair))
        {
            // Keep the still-fresh windows, drop the rest, then append the newly fetched window so the store self-cleans.
            List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> windows = new();
            foreach ((DateOnly windowStart, DateOnly windowEnd, DateTimeOffset fetchedAt) in ReadCoverage(pair))
            {
                if (asOf - fetchedAt < duration)
                    windows.Add((windowStart, windowEnd, fetchedAt));
            }

            windows.Add((start, end, asOf));

            // Replace only the coverage half; the entries half is left untouched so recording coverage never drops rows.
            WriteCoverage(pair, windows);
        }
    }

    /// <summary>
    /// Releases the keep-alive connection held for the instance lifetime.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _keepAlive.Dispose();
    }

    /// <summary>
    /// Reports whether a cached row is semantically valid against the evaluation instant.
    /// </summary>
    /// <param name="entry">The cached row to validate.</param>
    /// <param name="asOf">
    /// The instant against which the caching instant is checked for implausible future stamps.
    /// </param>
    /// <returns>
    /// <see langword="false" /> when the row carries a non-positive rate, a default (unset) date, or a caching instant
    /// implausibly far in the future of <paramref name="asOf" />; otherwise <see langword="true" />.
    /// </returns>
    /// <remarks>
    /// Invalid rows are silently skipped on both write (rejecting bad incoming data) and read (rejecting persisted or
    /// tampered rows) so a malformed cache never surfaces a nonsensical rate. A small clock-skew tolerance is allowed
    /// so a row stamped marginally ahead of the evaluating clock is not discarded.
    /// </remarks>
    private static bool IsValid(CachedExchangeRate entry, DateTimeOffset asOf) =>
        entry.Rate > 0m
            && entry.Date != default
            && entry.CachedAtUtc <= asOf + s_clockSkewTolerance;

    /// <summary>
    /// Creates the <c>rates</c> and <c>coverage</c> tables if they do not already exist.
    /// </summary>
    /// <param name="connection">An open connection to run the schema statements on.</param>
    private static void EnsureSchema(SqliteConnection connection)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS rates (
                    provider   TEXT NOT NULL,
                    from_code  TEXT NOT NULL,
                    to_code    TEXT NOT NULL,
                    obs_date   TEXT NOT NULL,
                    rate       TEXT NOT NULL,
                    cached_at  TEXT NOT NULL,
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

        transaction.Commit();
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
                "SELECT obs_date, rate, cached_at FROM rates WHERE provider = $provider AND from_code = $from AND to_code = $to;";
            BindPair(command, pair);

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    rows.Add(new CachedExchangeRate(
                        ParseDate(reader.GetString(0)),
                        ParseRate(reader.GetString(1)),
                        ParseInstant(reader.GetString(2))));
                }
                catch (FormatException)
                {
                    // Skip a single malformed row rather than failing the whole read.
                }
            }
        }
        catch (SqliteException)
        {
            return Array.Empty<CachedExchangeRate>();
        }
        catch (IOException)
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

            using (SqliteCommand delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText =
                    "DELETE FROM rates WHERE provider = $provider AND from_code = $from AND to_code = $to;";
                BindPair(delete, pair);
                delete.ExecuteNonQuery();
            }

            if (entries.Count > 0)
            {
                using SqliteCommand insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText =
                    """
                    INSERT INTO rates (provider, from_code, to_code, obs_date, rate, cached_at)
                    VALUES ($provider, $from, $to, $date, $rate, $cached)
                    ON CONFLICT (provider, from_code, to_code, obs_date)
                    DO UPDATE SET rate = excluded.rate, cached_at = excluded.cached_at;
                    """;
                SqliteParameter date = insert.Parameters.Add("$date", SqliteType.Text);
                SqliteParameter rate = insert.Parameters.Add("$rate", SqliteType.Text);
                SqliteParameter cached = insert.Parameters.Add("$cached", SqliteType.Text);
                BindPair(insert, pair);

                foreach (CachedExchangeRate entry in entries)
                {
                    date.Value = FormatDate(entry.Date);
                    rate.Value = FormatRate(entry.Rate);
                    cached.Value = FormatInstant(entry.CachedAtUtc);
                    insert.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
        catch (SqliteException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (IOException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
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
        catch (SqliteException)
        {
            return Array.Empty<(DateOnly, DateOnly, DateTimeOffset)>();
        }
        catch (IOException)
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
    private void WriteCoverage(ExchangeRatePair pair, List<(DateOnly Start, DateOnly End, DateTimeOffset FetchedAt)> windows)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText =
                    "DELETE FROM coverage WHERE provider = $provider AND from_code = $from AND to_code = $to;";
                BindPair(delete, pair);
                delete.ExecuteNonQuery();
            }

            if (windows.Count > 0)
            {
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

            transaction.Commit();
        }
        catch (SqliteException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
        }
        catch (IOException)
        {
            // Best-effort cache: a failed write must not break rate retrieval.
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
        command.Parameters.AddWithValue("$from", pair.FromIsoCode);
        command.Parameters.AddWithValue("$to", pair.ToIsoCode);
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
