// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteNotableDateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Configures a SQLite-backed <see cref="INotableDateCache" />: the location of the database computed years are
/// persisted in and the connection-level concurrency settings applied on open.
/// </summary>
/// <remarks>
/// The database location is supplied either as a <see cref="DatabaseFilePath" /> — a path to a SQLite file created on
/// first use — or as a fully specified <see cref="ConnectionString" /> for advanced scenarios such as a shared in-memory
/// database. At least one must be set; <see cref="ConnectionString" /> takes precedence when both are supplied.
/// </remarks>
public class SqliteNotableDateCacheOptions
    : NotableDateCacheOptions
{
    /// <summary>
    /// Gets or sets the path to the SQLite database file used by the cache.
    /// </summary>
    /// <value>
    /// The database file path, or <see langword="null" /> when a full <see cref="ConnectionString" /> is supplied
    /// instead.
    /// </value>
    public string? DatabaseFilePath { get; set; }

    /// <summary>
    /// Gets or sets the full SQLite connection string used by the cache.
    /// </summary>
    /// <value>The connection string, or <see langword="null" /> to derive one from <see cref="DatabaseFilePath" />.</value>
    /// <remarks>
    /// Takes precedence over <see cref="DatabaseFilePath" /> when both are supplied, allowing scenarios such as a shared
    /// in-memory database (<c>Data Source=name;Mode=Memory;Cache=Shared</c>). A supplied value's presence is checked but
    /// its contents are not parsed until connect time.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cache enables SQLite write-ahead logging (WAL) on the database.
    /// </summary>
    /// <value><see langword="true" /> to switch a file database to WAL journal mode on open; defaults to <see langword="true" />.</value>
    /// <remarks>
    /// The setting is applied best-effort: a database that does not support WAL — notably an in-memory database — is left
    /// in its native journal mode rather than failing.
    /// </remarks>
    public bool UseWriteAheadLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the time a connection waits for a held database lock to clear before reporting a busy error.
    /// </summary>
    /// <value>The busy-wait duration applied as the SQLite <c>busy_timeout</c> pragma; defaults to five seconds.</value>
    public TimeSpan BusyTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when neither <see cref="DatabaseFilePath" /> nor <see cref="ConnectionString" /> is supplied.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="BusyTimeout" /> is negative.</exception>
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString) && string.IsNullOrWhiteSpace(DatabaseFilePath))
            throw new ArgumentException(CalendarCachingSqliteResourceStrings.Arg_Invalid_DatabaseLocationMissing, nameof(DatabaseFilePath));

        if (BusyTimeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(BusyTimeout), BusyTimeout, CalendarCachingSqliteResourceStrings.Arg_OutOfRange_BusyTimeoutNegative);
    }

    /// <inheritdoc />
    public override bool TryValidate(out string? error)
    {
        if (!base.TryValidate(out error))
            return false;

        if (string.IsNullOrWhiteSpace(ConnectionString) && string.IsNullOrWhiteSpace(DatabaseFilePath))
        {
            error = CalendarCachingSqliteResourceStrings.Arg_Invalid_DatabaseLocationMissing;
            return false;
        }

        if (BusyTimeout < TimeSpan.Zero)
        {
            error = CalendarCachingSqliteResourceStrings.Arg_OutOfRange_BusyTimeoutNegative;
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
    internal string ResolveConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
            return ConnectionString!;

        return new SqliteConnectionStringBuilder { DataSource = DatabaseFilePath }.ToString();
    }
}
