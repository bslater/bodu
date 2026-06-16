// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOrigin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Identifies where a served exchange rate was resolved from, distinguishing a value produced directly by a provider
/// from one served out of a cache.
/// </summary>
/// <remarks>
/// The value is surfaced through <see cref="ExchangeRateProvenance.Origin" /> on every
/// <see cref="ExchangeRateLookupResult" />, so a caller can tell a freshly resolved rate from a cached one without
/// inspecting the provider chain.
/// </remarks>
public enum ExchangeRateOrigin
{
    /// <summary>
    /// The rate was resolved directly by a provider rather than served from a cache. For a cache-fronted provider this
    /// corresponds to a cache miss that was satisfied by the wrapped inner provider.
    /// </summary>
    Live,

    /// <summary>
    /// The rate was served from a cache without consulting the underlying provider.
    /// </summary>
    Cache,
}
