// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateSeriesNotFoundException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// The exception thrown when a requested currency pair cannot be served from RBA data.
/// </summary>
/// <remarks>
/// The RBA publishes rates quoted against the Australian dollar, so only pairs where one side is <c>AUD</c> can be
/// resolved (directly or inverted). A cross-currency pair with neither side equal to <c>AUD</c> raises this exception.
/// </remarks>
public sealed class RbaExchangeRateSeriesNotFoundException
    : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateSeriesNotFoundException" /> class.
    /// </summary>
    public RbaExchangeRateSeriesNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateSeriesNotFoundException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    public RbaExchangeRateSeriesNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateSeriesNotFoundException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public RbaExchangeRateSeriesNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
