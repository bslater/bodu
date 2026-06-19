// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxExchangeRateFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ofx;

/// <summary>
/// The exception thrown when an OFX spot-rate-history response cannot be interpreted as exchange-rate data — because it
/// is malformed JSON or omits the expected historical-points data.
/// </summary>
public sealed class OfxExchangeRateFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OfxExchangeRateFormatException" /> class.
    /// </summary>
    public OfxExchangeRateFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OfxExchangeRateFormatException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    public OfxExchangeRateFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OfxExchangeRateFormatException" /> class with the specified message
    /// and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public OfxExchangeRateFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
