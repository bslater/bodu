// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryExchangeRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Contracts;

/// <summary>
/// Runs the shared <see cref="IExchangeRateCache" /> contract against the in-memory cache, which exercises the
/// <see cref="ExchangeRateCacheBase{TOptions}" /> mechanism without touching the file system.
/// </summary>
[TestClass]
public sealed class InMemoryExchangeRateCacheContractTests
    : ExchangeRateCacheContractTests<InMemoryExchangeRateCache>
{
    /// <inheritdoc />
    protected override InMemoryExchangeRateCache CreateCache() => new(Provider);
}
