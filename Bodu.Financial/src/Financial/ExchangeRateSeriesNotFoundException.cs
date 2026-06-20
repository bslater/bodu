// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesNotFoundException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// The exception thrown when a requested currency pair cannot be served because the upstream feed structurally does not
/// carry it.
/// </summary>
/// <remarks>
/// This is the shared "pair not carried" type for the exchange-rate provider packages. It is distinct from the
/// <see cref="KeyNotFoundException" /> a provider raises when a pair it does cover simply has no rate on a requested
/// date; this exception signals that the feed can never resolve the pair (for example, a cross-currency pair on a feed
/// that only quotes against a single base currency).
/// </remarks>
public sealed class ExchangeRateSeriesNotFoundException
    : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesNotFoundException" /> class.
    /// </summary>
    public ExchangeRateSeriesNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesNotFoundException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    public ExchangeRateSeriesNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateSeriesNotFoundException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public ExchangeRateSeriesNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
