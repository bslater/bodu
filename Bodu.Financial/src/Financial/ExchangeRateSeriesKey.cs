// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Identifies a single rate series within an <see cref="ExchangeRateTableBuilder" /> by its currency pair and provider.
/// </summary>
/// <remarks>
/// <para>
/// Provider equality is intentionally <see cref="StringComparer.Ordinal" /> — two keys whose providers differ only in
/// letter case are considered distinct. Callers that need case-insensitive grouping should normalise the provider
/// identifier before constructing the key.
/// </para>
/// </remarks>
public readonly record struct ExchangeRateSeriesKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesKey" /> struct.
    /// </summary>
    /// <param name="pair">The currency pair the series describes.</param>
    /// <param name="provider">The non-empty identifier of the publishing source.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="pair" /> is the default-struct value (and therefore carries null ISO codes), or if
    /// <paramref name="provider" /> is empty or white-space.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public ExchangeRateSeriesKey(ExchangeRatePair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidExchangeRatePair(pair);
        FinancialThrowHelper.ThrowIfNullOrWhiteSpaceProvider(provider);

        Pair = pair;
        Provider = provider;
    }

    /// <summary>
    /// Gets the currency pair the series describes.
    /// </summary>
    /// <returns>The directional pair.</returns>
    public ExchangeRatePair Pair { get; }

    /// <summary>
    /// Gets the non-empty identifier of the publishing source.
    /// </summary>
    /// <returns>The provider identifier, compared ordinally.</returns>
    public string Provider { get; }
}
