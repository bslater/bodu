// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryExchangeRateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Contracts;

/// <summary>
/// Runs the shared <see cref="IExchangeRateCache" /> contract against the in-memory test double, which exercises the
/// <see cref="ExchangeRateCacheBase" /> mechanism directly.
/// </summary>
[TestClass]
public sealed class InMemoryExchangeRateCacheContractTests
    : ExchangeRateCacheContractTests<InMemoryExchangeRateCache>
{
    /// <inheritdoc />
    protected override InMemoryExchangeRateCache CreateCache() => new();
}
