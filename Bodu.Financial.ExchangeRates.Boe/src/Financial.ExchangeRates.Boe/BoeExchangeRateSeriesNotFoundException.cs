// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateSeriesNotFoundException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// The exception thrown when a requested currency pair cannot be served from Bank of England data.
/// </summary>
/// <remarks>
/// The Bank of England publishes daily spot rates quoted against the pound sterling, so only pairs where one side is
/// <c>GBP</c> can be resolved (directly or inverted). A cross-currency pair with neither side equal to <c>GBP</c>
/// raises this exception.
/// </remarks>
public sealed class BoeExchangeRateSeriesNotFoundException
    : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateSeriesNotFoundException" /> class.
    /// </summary>
    public BoeExchangeRateSeriesNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateSeriesNotFoundException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    public BoeExchangeRateSeriesNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateSeriesNotFoundException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the unavailable pair.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public BoeExchangeRateSeriesNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
