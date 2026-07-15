// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedCacheStorageStartupValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Validates, at application startup, that the backing store of a notable-date cache configured with
/// <see cref="NotableDateCacheOptions.ValidateStorageOnStart" /> can be reached, so an unreachable or misconfigured
/// distributed cache fails the host start rather than the first lookup.
/// </summary>
/// <remarks>
/// Registered as an <see cref="IValidateOptions{TOptions}" /> so the existing <c>ValidateOnStart</c> wiring runs it at
/// host start. It is a no-op unless <see cref="NotableDateCacheOptions.ValidateStorageOnStart" /> is set.
/// </remarks>
internal sealed class DistributedCacheStorageStartupValidator
    : IValidateOptions<DistributedNotableDateCacheOptions>
{
    /// <summary>The sentinel territory whose key the probe reads.</summary>
    private const string ProbeTerritory = "__PROBE__";

    /// <summary>The distributed cache the notable-date cache is backed by, probed at startup.</summary>
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheStorageStartupValidator" /> class.
    /// </summary>
    /// <param name="distributedCache">The distributed cache backing the notable-date cache.</param>
    public DistributedCacheStorageStartupValidator(IDistributedCache distributedCache) =>
        _distributedCache = distributedCache;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DistributedNotableDateCacheOptions options)
    {
        if (!options.ValidateStorageOnStart)
            return ValidateOptionsResult.Skip;

        try
        {
            // A sentinel read exercises the connection to the backing store without writing anything.
            _ = _distributedCache.Get(options.BuildKey(ProbeTerritory));
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ValidateOptionsResult.Fail(string.Format(
                CultureInfo.CurrentCulture,
                CalendarCachingDistributedResourceStrings.Op_Invalid_StorageProbeFailed,
                ex.Message));
        }
    }
}
