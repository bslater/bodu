// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Caching;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> that persists computed years in a SQLite database, expiring them through the
/// same freshness and version mechanism as the in-memory and file caches.
/// </summary>
/// <remarks>
/// <para>
/// Computed years live in one table keyed by <c>(territory, year, version)</c>, one row per cached year, with the
/// year's occurrences stored as a JSON blob and the computed instant as invariant round-trip text. The read-merge-write
/// mechanism, per-territory locking, and the freshness, validity, version-matching, and merge rules are all inherited
/// from <see cref="NotableDateCacheBase{TOptions}" />; this class contributes only its SQLite storage, plus a
/// single-row <see cref="GetYear" /> override so a lookup reads one row rather than parsing every year's occurrence
/// blob.
/// </para>
/// <para>
/// The cache is a single-process best-effort store. As required by <see cref="INotableDateCache" />, a storage failure
/// surfaces as an empty read or a skipped write rather than an exception, and each swallowed failure is logged at
/// <see cref="LogLevel.Warning" /> rate-limited to at most one warning per minute. A single keep-alive connection is
/// held open for the instance lifetime so a shared in-memory database survives between operations.
/// </para>
/// </remarks>
public sealed class SqliteNotableDateCache
    : NotableDateCacheBase<SqliteNotableDateCacheOptions>, IDisposable
{
    /// <summary>The resolved connection string every connection is opened with.</summary>
    private readonly string _connectionString;

    /// <summary>The keep-alive connection held open so a shared in-memory database is not torn down between operations.</summary>
    private readonly SqliteConnection _keepAlive;

    /// <summary>The logger that receives the rate-limited degradation warnings.</summary>
    private readonly ILogger _logger;

    /// <summary>Rate-limits the degradation warning to at most one emission per cooldown window.</summary>
    private readonly RateLimitedWarningGate _warnGate;

    /// <summary>Tracks whether the instance has been disposed, as <c>0</c> for live and <c>1</c> for disposed.</summary>
    private int _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteNotableDateCache" /> class.
    /// </summary>
    /// <param name="options">The options carrying the database location.</param>
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
    public SqliteNotableDateCache(SqliteNotableDateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options)
    {
        _connectionString = options.ResolveConnectionString();
        _warnGate = new RateLimitedWarningGate(timeProvider, RateLimitedWarningGate.DefaultCooldown);
        _logger = logger ?? NullLogger.Instance;

        _keepAlive = new SqliteConnection(_connectionString);

        try
        {
            _keepAlive.Open();
            ConfigureConnection(_keepAlive);
            EnsureSchema(_keepAlive);
        }
        catch (Exception ex) when (ex is SqliteException or IOException && !options.ValidateStorageOnStart && !options.ThrowOnStorageFailure)
        {
            // Best-effort cache: a database that cannot be opened or initialized now degrades to empty reads and skipped
            // writes rather than failing construction. When either strict flag is set the failure propagates instead.
            OnStorageFailureSwallowed("schema initialization", ex);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteNotableDateCache" /> class bound to a database file.
    /// </summary>
    /// <param name="databaseFilePath">The path to the SQLite database file used by the cache.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="databaseFilePath" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="databaseFilePath" /> is empty or white space.
    /// </exception>
    public SqliteNotableDateCache(string databaseFilePath)
        : this(new SqliteNotableDateCacheOptions { DatabaseFilePath = databaseFilePath })
    {
    }

    /// <summary>
    /// Gets a value indicating whether a caught storage failure should degrade to a best-effort fallback rather than
    /// propagate.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="NotableDateCacheOptions.ThrowOnStorageFailure" /> is not set.
    /// </value>
    private bool ShouldSwallowStorageFailure => !Options.ThrowOnStorageFailure;

    /// <inheritdoc />
    /// <remarks>
    /// Overridden so a single-year lookup reads exactly one keyed row rather than loading and parsing every cached
    /// year's occurrence blob for the territory; the same freshness, validity, and version policy is applied through
    /// the shared <see cref="NotableDateCacheRules" />.
    /// </remarks>
    public override NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(resourceVersion);

        string key = NotableDateCacheRules.NormalizeTerritory(territory);

        NotableDateCacheEntry? entry = ReadYear(key, year, resourceVersion);
        if (entry is null)
            return null;

        return NotableDateCacheRules.IsValid(entry, asOf) && entry.IsFresh(asOf, ttl) ? entry : null;
    }

    /// <inheritdoc />
    public override void Clear()
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM notable_dates;";
            command.ExecuteNonQuery();
        }
        catch (Exception ex) when (ex is SqliteException or IOException && ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("clear", ex);
        }
    }

    /// <summary>
    /// Releases the keep-alive connection held for the instance lifetime.
    /// </summary>
    /// <remarks>
    /// Idempotent: a second or concurrent call is a safe no-op. The disposed flag is flipped with
    /// <see cref="Interlocked.Exchange(ref int, int)" /> so exactly one caller wins the transition and disposes the
    /// keep-alive connection.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _keepAlive.Dispose();
    }

    /// <inheritdoc />
    protected internal override IReadOnlyList<NotableDateCacheEntry> ReadEntries(string territory)
    {
        List<NotableDateCacheEntry> entries = new();

        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT year, version, computed_at, occurrences FROM notable_dates WHERE territory = $territory;";
            command.Parameters.AddWithValue("$territory", territory);

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                NotableDateCacheEntry? entry = BuildEntry(territory, reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
                if (entry is not null)
                    entries.Add(entry);
            }
        }
        catch (Exception ex) when (ex is SqliteException or IOException && ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return Array.Empty<NotableDateCacheEntry>();
        }

        return entries;
    }

    /// <inheritdoc />
    protected internal override bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM notable_dates WHERE territory = $territory;";
                delete.Parameters.AddWithValue("$territory", territory);
                delete.ExecuteNonQuery();
            }

            if (entries.Count > 0)
            {
                using SqliteCommand insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText =
                    """
                    INSERT INTO notable_dates (territory, year, version, computed_at, occurrences)
                    VALUES ($territory, $year, $version, $computed, $occurrences);
                    """;
                SqliteParameter year = insert.Parameters.Add("$year", SqliteType.Integer);
                SqliteParameter version = insert.Parameters.Add("$version", SqliteType.Text);
                SqliteParameter computed = insert.Parameters.Add("$computed", SqliteType.Text);
                SqliteParameter occurrences = insert.Parameters.Add("$occurrences", SqliteType.Text);
                insert.Parameters.AddWithValue("$territory", territory);

                foreach (NotableDateCacheEntry entry in entries)
                {
                    year.Value = entry.Year;
                    version.Value = entry.ResourceVersion;
                    computed.Value = InvariantCacheText.FormatInstant(entry.ComputedAtUtc);
                    occurrences.Value = NotableDateCacheFileConverter.SerializeOccurrences(entry.Occurrences);
                    insert.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            return true;
        }
        catch (Exception ex) when (ex is SqliteException or IOException && ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("store", ex);
            return false;
        }
    }

    /// <summary>
    /// Creates the <c>notable_dates</c> table if it does not already exist.
    /// </summary>
    /// <param name="connection">An open connection to run the schema statement on.</param>
    private static void EnsureSchema(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS notable_dates (
                territory     TEXT NOT NULL,
                year          INTEGER NOT NULL,
                version       TEXT NOT NULL,
                computed_at   TEXT NOT NULL,
                occurrences   TEXT NOT NULL,
                PRIMARY KEY (territory, year, version)
            );
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Builds a cache entry from stored columns, skipping a row whose stored instant cannot be parsed.
    /// </summary>
    /// <param name="territory">The normalized territory.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="version">The resource version.</param>
    /// <param name="computedAt">The stored computed instant text.</param>
    /// <param name="occurrences">The stored occurrences JSON blob.</param>
    /// <returns>The reconstructed entry, or <see langword="null" /> when the row is malformed.</returns>
    private static NotableDateCacheEntry? BuildEntry(string territory, int year, string version, string computedAt, string occurrences)
    {
        try
        {
            return new NotableDateCacheEntry(
                territory,
                year,
                version,
                NotableDateCacheFileConverter.DeserializeOccurrences(occurrences),
                InvariantCacheText.ParseInstant(computedAt));
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            // Skip a single malformed row rather than failing the whole read.
            return null;
        }
    }

    /// <summary>
    /// Reads a single cached year for a territory, year, and version, or <see langword="null" /> when absent or the
    /// read fails.
    /// </summary>
    /// <param name="territory">The normalized territory.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="version">The resource version.</param>
    /// <returns>The stored entry, or <see langword="null" />.</returns>
    private NotableDateCacheEntry? ReadYear(string territory, int year, string version)
    {
        try
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT computed_at, occurrences FROM notable_dates WHERE territory = $territory AND year = $year AND version = $version;";
            command.Parameters.AddWithValue("$territory", territory);
            command.Parameters.AddWithValue("$year", year);
            command.Parameters.AddWithValue("$version", version);

            using SqliteDataReader reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            return BuildEntry(territory, year, version, reader.GetString(0), reader.GetString(1));
        }
        catch (Exception ex) when (ex is SqliteException or IOException && ShouldSwallowStorageFailure)
        {
            OnStorageFailureSwallowed("read", ex);
            return null;
        }
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
    /// Applies the connection-level concurrency settings to a freshly opened connection.
    /// </summary>
    /// <param name="connection">The open connection to configure.</param>
    private void ConfigureConnection(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = string.Format(
            CultureInfo.InvariantCulture,
            "PRAGMA busy_timeout = {0};",
            (long)Options.BusyTimeout.TotalMilliseconds);
        command.ExecuteNonQuery();

        if (Options.UseWriteAheadLogging)
        {
            command.CommandText = "PRAGMA journal_mode = WAL;";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Reports a swallowed best-effort storage failure to the logger, rate-limited to at most one warning per cooldown
    /// window.
    /// </summary>
    /// <param name="operation">The storage operation that failed.</param>
    /// <param name="exception">The swallowed storage exception.</param>
    private void OnStorageFailureSwallowed(string operation, Exception exception)
    {
        if (_warnGate.TryClaimWarning(out int suppressed))
            Log.StorageFailureSwallowed(_logger, operation, suppressed, exception);
    }
}
