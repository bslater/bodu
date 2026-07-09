// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures a SQLite-backed <see cref="IRateCache" />: the provider inherited from
/// <see cref="RateCacheOptions" /> together with the location of the SQLite database its rates and coverage
/// windows are persisted in, and the connection-level concurrency settings applied on open.
/// </summary>
/// <remarks>
/// <para>
/// The database location is supplied either as a <see cref="DatabaseFilePath" /> — the simplest form, a path to a
/// SQLite file that is created on first use — or as a fully specified <see cref="ConnectionString" /> for advanced
/// scenarios such as a shared in-memory database or custom connection flags. At least one must be set;
/// <see cref="ConnectionString" /> takes precedence when both are supplied.
/// </para>
/// <para>
/// A single database file may be shared by several caches: each cache stores exactly one provider, and the provider is
/// the leading column of the rate and coverage keys, so multiple single-provider caches pointed at the same file keep
/// their series partitioned with no collisions. <see cref="UseWriteAheadLogging" /> and <see cref="BusyTimeout" />
/// govern how concurrent writers — multiple cache instances, or separate processes — sharing one file behave.
/// </para>
/// <para>
/// Expiry is not a storage concern — it is supplied per call by the caching provider — so this type carries only the
/// storage location and connection settings in addition to the bound provider.
/// </para>
/// </remarks>
public class SqliteRateCacheOptions
    : RateCacheOptions
{
    /// <summary>
    /// Gets or sets the path to the SQLite database file used by the cache.
    /// </summary>
    /// <value>
    /// The database file path, or <see langword="null" /> when a full <see cref="ConnectionString" /> is supplied
    /// instead.
    /// </value>
    /// <remarks>
    /// When set without a <see cref="ConnectionString" />, the cache opens the file with default connection settings,
    /// creating the file and any missing schema on first use.
    /// </remarks>
    public string? DatabaseFilePath { get; set; }

    /// <summary>
    /// Gets or sets the full SQLite connection string used by the cache.
    /// </summary>
    /// <value>
    /// The connection string, or <see langword="null" /> to derive one from <see cref="DatabaseFilePath" />.
    /// </value>
    /// <remarks>
    /// <para>
    /// Takes precedence over <see cref="DatabaseFilePath" /> when both are supplied, allowing scenarios such as a
    /// shared in-memory database (<c>Data Source=name;Mode=Memory;Cache=Shared</c>) or custom connection flags.
    /// </para>
    /// <para>
    /// A supplied value is not parsed or validated when the options are constructed — only its presence is checked. A
    /// malformed or unusable connection string is therefore not rejected up front; it surfaces later at connect time,
    /// where the cache's best-effort behaviour degrades to an empty read or a skipped write rather than throwing.
    /// </para>
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cache enables SQLite write-ahead logging (WAL) on the database.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to switch a file database to WAL journal mode on open; otherwise
    /// <see langword="false" /> to leave the journal mode unchanged. The default is <see langword="true" />.
    /// </value>
    /// <remarks>
    /// <para>
    /// WAL lets readers run concurrently with a writer and lifts write throughput when several caches or processes
    /// share one file, which is the recommended mode for a database holding more than one provider's series. The
    /// setting is applied best-effort: a database that does not support WAL — notably an in-memory database — is left
    /// in its native journal mode rather than failing.
    /// </para>
    /// <para>
    /// Disable it for storage where WAL is unsupported or undesirable, such as some network file systems.
    /// </para>
    /// </remarks>
    public bool UseWriteAheadLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the time a connection waits for a held database lock to clear before reporting a busy error.
    /// </summary>
    /// <value>
    /// The busy-wait duration applied as the SQLite <c>busy_timeout</c> pragma on every connection. The default is five
    /// seconds; <see cref="TimeSpan.Zero" /> disables waiting, so a contended write fails immediately.
    /// </value>
    /// <remarks>
    /// When multiple cache instances or processes write to one shared file, a non-zero timeout lets a writer wait for a
    /// peer's transaction to commit instead of failing the write outright, which the best-effort cache would otherwise
    /// swallow as a silently dropped write.
    /// </remarks>
    public TimeSpan BusyTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <see cref="RateCacheOptions.Provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="RateCacheOptions.Provider" /> is empty or white space, or when neither
    /// <see cref="DatabaseFilePath" /> nor <see cref="ConnectionString" /> is supplied.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="BusyTimeout" /> is negative.</exception>
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString) && string.IsNullOrWhiteSpace(DatabaseFilePath))
        {
            throw new ArgumentException(CachingSqliteResourceStrings.Arg_Invalid_DatabaseLocationMissing, nameof(DatabaseFilePath));
        }

        if (BusyTimeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(BusyTimeout), BusyTimeout, CachingSqliteResourceStrings.Arg_OutOfRange_BusyTimeoutNegative);
        }
    }

    /// <inheritdoc />
    public override bool TryValidate(out string? error)
    {
        if (!base.TryValidate(out error))
            return false;

        if (string.IsNullOrWhiteSpace(ConnectionString) && string.IsNullOrWhiteSpace(DatabaseFilePath))
        {
            error = CachingSqliteResourceStrings.Arg_Invalid_DatabaseLocationMissing;
            return false;
        }

        if (BusyTimeout < TimeSpan.Zero)
        {
            error = CachingSqliteResourceStrings.Arg_OutOfRange_BusyTimeoutNegative;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Resolves the SQLite connection string from the configured location, preferring an explicit
    /// <see cref="ConnectionString" /> over one built from <see cref="DatabaseFilePath" />.
    /// </summary>
    /// <returns>The connection string to open the cache database with.</returns>
    /// <remarks>
    /// Callers must validate the options before resolving; this method assumes at least one location is set.
    /// </remarks>
    internal string ResolveConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
            return ConnectionString!;

        return new SqliteConnectionStringBuilder { DataSource = DatabaseFilePath }.ToString();
    }
}
