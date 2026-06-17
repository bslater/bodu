// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteCacheStorageStartupValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates.Caching;
using Bodu.Financial.ExchangeRates.Caching.Sqlite;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite.DependencyInjection;

/// <summary>
/// Validates, at application startup, that the SQLite database backing an exchange-rate cache configured with
/// <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> can be opened and initialized, so a misconfigured or
/// unwritable database fails the host start rather than the first lookup.
/// </summary>
/// <remarks>
/// Registered as an <see cref="IValidateOptions{TOptions}" /> so the existing <c>ValidateOnStart</c> wiring runs it at
/// host start. It is a no-op unless <see cref="ExchangeRateCacheOptions.ValidateStorageOnStart" /> is set.
/// </remarks>
internal sealed class SqliteCacheStorageStartupValidator
    : IValidateOptions<SqliteExchangeRateCacheOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SqliteExchangeRateCacheOptions options)
    {
        if (!options.ValidateStorageOnStart)
            return ValidateOptionsResult.Skip;

        try
        {
            // Constructing the cache runs the same open-and-ensure-schema probe the runtime uses; with
            // ValidateStorageOnStart set the constructor throws rather than swallowing when the database is unusable.
            using var probe = new SqliteExchangeRateCache(options);
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ValidateOptionsResult.Fail(
                $"The SQLite exchange-rate cache database for provider '{options.Provider}' could not be opened at startup: {ex.Message}");
        }
    }
}
