// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SqliteCacheStorageStartupValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Validates, at application startup, that the SQLite database backing an exchange-rate cache configured with
/// <see cref="RateCacheOptions.ValidateStorageOnStart" /> can be opened and initialized, so a misconfigured or
/// unwritable database fails the host start rather than the first lookup.
/// </summary>
/// <remarks>
/// Registered as an <see cref="IValidateOptions{TOptions}" /> so the existing <c>ValidateOnStart</c> wiring runs it at
/// host start. It is a no-op unless <see cref="RateCacheOptions.ValidateStorageOnStart" /> is set.
/// </remarks>
internal sealed class SqliteCacheStorageStartupValidator
    : IValidateOptions<SqliteRateCacheOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SqliteRateCacheOptions options)
    {
        if (!options.ValidateStorageOnStart)
            return ValidateOptionsResult.Skip;

        try
        {
            // Constructing the cache runs the same open-and-ensure-schema probe the runtime uses; with
            // ValidateStorageOnStart set the constructor throws rather than swallowing when the database is unusable.
            using var probe = new SqliteRateCache(options);
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ValidateOptionsResult.Fail(
                $"The SQLite exchange-rate cache database for provider '{options.Provider}' could not be opened at startup: {ex.Message}");
        }
    }
}
