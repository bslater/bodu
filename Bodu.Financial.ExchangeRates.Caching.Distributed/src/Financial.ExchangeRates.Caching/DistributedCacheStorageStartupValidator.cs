// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedCacheStorageStartupValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Validates, at application startup, that the backing store of an exchange-rate cache configured with
/// <see cref="RateCacheOptions.ValidateStorageOnStart" /> can be reached, so an unreachable or misconfigured
/// distributed cache fails the host start rather than the first lookup.
/// </summary>
/// <remarks>
/// Registered as an <see cref="IValidateOptions{TOptions}" /> so the existing <c>ValidateOnStart</c> wiring runs it at
/// host start. It is a no-op unless <see cref="RateCacheOptions.ValidateStorageOnStart" /> is set.
/// </remarks>
internal sealed class DistributedCacheStorageStartupValidator
    : IValidateOptions<DistributedRateCacheOptions>
{
    /// <summary>The distributed cache the exchange-rate cache is backed by, probed at startup.</summary>
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheStorageStartupValidator" /> class.
    /// </summary>
    /// <param name="distributedCache">The distributed cache backing the exchange-rate cache.</param>
    public DistributedCacheStorageStartupValidator(IDistributedCache distributedCache) =>
        _distributedCache = distributedCache;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DistributedRateCacheOptions options)
    {
        if (!options.ValidateStorageOnStart)
            return ValidateOptionsResult.Skip;

        try
        {
            // Constructing the cache runs the same sentinel-read probe the runtime uses; with ValidateStorageOnStart set
            // the constructor throws rather than swallowing when the backing store is unreachable.
            _ = new DistributedRateCache(_distributedCache, options);
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ValidateOptionsResult.Fail(
                $"The distributed exchange-rate cache for provider '{options.Provider}' could not be reached at startup: {ex.Message}");
        }
    }
}
