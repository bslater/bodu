// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteExchangeRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Data.Sqlite;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures a SQLite-backed <see cref="IExchangeRateCache" />: the single provider inherited from
/// <see cref="ExchangeRateCacheOptions" /> together with the location of the SQLite database its rates and coverage
/// windows are persisted in.
/// </summary>
/// <remarks>
/// <para>
/// The database location is supplied either as a <see cref="DatabaseFilePath" /> — the simplest form, a path to a
/// SQLite file that is created on first use — or as a fully specified <see cref="ConnectionString" /> for advanced
/// scenarios such as a shared in-memory database or custom connection flags. At least one must be set;
/// <see cref="ConnectionString" /> takes precedence when both are supplied.
/// </para>
/// <para>
/// Expiry is not a storage concern — it is supplied per call by the caching provider — so this type carries only the
/// storage location in addition to the bound provider.
/// </para>
/// </remarks>
public class SqliteExchangeRateCacheOptions
    : ExchangeRateCacheOptions
{
    /// <summary>
    /// Gets or sets the path to the SQLite database file used by the cache.
    /// </summary>
    /// <value>
    /// The database file path, or <see langword="null" /> when a full <see cref="ConnectionString" /> is supplied
    /// instead.
    /// </value>
    /// <returns>The configured database file path, or <see langword="null" /> when none is set.</returns>
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
    /// <returns>The configured connection string, or <see langword="null" /> when none is set.</returns>
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
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <see cref="ExchangeRateCacheOptions.Provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="ExchangeRateCacheOptions.Provider" /> is empty or white space, or when neither
    /// <see cref="DatabaseFilePath" /> nor <see cref="ConnectionString" /> is supplied.
    /// </exception>
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(ConnectionString) && string.IsNullOrWhiteSpace(DatabaseFilePath))
        {
            throw new ArgumentException(CachingSqliteResourceStrings.Arg_Invalid_DatabaseLocationMissing, nameof(DatabaseFilePath));
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
