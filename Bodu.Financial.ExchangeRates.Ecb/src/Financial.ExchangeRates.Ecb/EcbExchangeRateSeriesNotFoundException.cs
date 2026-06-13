// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateSeriesNotFoundException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// The exception thrown when a requested currency pair cannot be served from ECB data.
/// </summary>
/// <remarks>
/// The ECB publishes rates quoted against the euro, so only pairs where one side is <c>EUR</c> can be resolved
/// (directly or inverted). A cross-currency pair with neither side equal to <c>EUR</c> raises this exception.
/// </remarks>
public sealed class EcbExchangeRateSeriesNotFoundException
    : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateSeriesNotFoundException" /> class.
    /// </summary>
    public EcbExchangeRateSeriesNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateSeriesNotFoundException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    public EcbExchangeRateSeriesNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateSeriesNotFoundException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public EcbExchangeRateSeriesNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
