// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// The exception thrown when a Yahoo Finance chart response cannot be interpreted as exchange-rate data — because it is
/// malformed JSON, carries a chart error, or omits the expected chart members.
/// </summary>
public sealed class YahooExchangeRateFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateFormatException" /> class.
    /// </summary>
    public YahooExchangeRateFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateFormatException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    public YahooExchangeRateFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateFormatException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public YahooExchangeRateFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
