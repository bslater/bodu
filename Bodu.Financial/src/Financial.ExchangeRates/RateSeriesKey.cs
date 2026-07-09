// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Identifies a single rate series within an <see cref="RateTableBuilder" /> by its currency pair and provider.
/// </summary>
/// <remarks>
/// <para>
/// Provider equality is intentionally <see cref="StringComparer.Ordinal" /> — two keys whose providers differ only in
/// letter case are considered distinct. Callers that need case-insensitive grouping should normalise the provider
/// identifier before constructing the key.
/// </para>
/// </remarks>
public readonly record struct RateSeriesKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateSeriesKey" /> class.
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
    public RateSeriesKey(CurrencyPair pair, string provider)
    {
        FinancialThrowHelper.ThrowIfInvalidCurrencyPair(pair);
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        Pair = pair;
        Provider = provider;
    }

    /// <summary>
    /// Gets the currency pair the series describes.
    /// </summary>
    /// <value>The directional pair.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the non-empty identifier of the publishing source.
    /// </summary>
    /// <value>The provider identifier, compared ordinally.</value>
    public string Provider { get; }
}
