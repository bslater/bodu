// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic options shared by every <see cref="IExchangeRateCache" />: the single provider a cache
/// instance is bound to. Storage-specific option types derive from this base to add their own location settings.
/// </summary>
/// <remarks>
/// A cache instance serves exactly one provider, so the provider name is fixed here at construction rather than
/// supplied on each call. This lets the provider identity participate in the storage layout (for example a per-provider
/// subdirectory) without ambiguity, and keeps the <see cref="IExchangeRateCache" /> surface free of a provider
/// argument.
/// </remarks>
public class ExchangeRateCacheOptions
{
    /// <summary>
    /// Gets or sets the name of the provider whose rates this cache stores.
    /// </summary>
    /// <value>The provider identifier the cache is bound to.</value>
    /// <returns>The configured provider name.</returns>
    public string Provider { get; set; } = default!;

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <see cref="Provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="Provider" /> is empty or white space.</exception>
    public virtual void Validate() =>
        ThrowHelper.ThrowIfNullOrWhiteSpace(Provider);
}
