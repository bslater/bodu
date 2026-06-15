// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOrigin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Identifies where a served exchange rate was resolved from, distinguishing a live fetch through the wrapped inner
/// provider from a serve out of the cache.
/// </summary>
internal enum ExchangeRateOrigin
{
    /// <summary>
    /// The rate was resolved fresh from the wrapped inner provider on a cache miss.
    /// </summary>
    Live,

    /// <summary>
    /// The rate was served from the cache without consulting the wrapped inner provider.
    /// </summary>
    Cache,
}
