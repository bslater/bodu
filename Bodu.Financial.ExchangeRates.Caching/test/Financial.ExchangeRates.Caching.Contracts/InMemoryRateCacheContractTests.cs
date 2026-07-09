// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Contracts;

/// <summary>
/// Runs the shared <see cref="IRateCache" /> contract against the in-memory cache, which exercises the
/// <see cref="RateCacheBase{TOptions}" /> mechanism without touching the file system.
/// </summary>
[TestClass]
public sealed class InMemoryRateCacheContractTests
    : RateCacheContractTests<InMemoryRateCache>
{
    /// <inheritdoc />
    protected override InMemoryRateCache CreateCache() => new(Provider);
}
