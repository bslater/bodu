// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// The exception thrown when an upstream exchange-rate feed returns data that cannot be interpreted as exchange-rate
/// information — because it is malformed or omits the expected values.
/// </summary>
/// <remarks>
/// This is the shared format-failure type for the exchange-rate provider packages. Each provider raises it for the
/// feed-specific reason its parser detected (malformed JSON, XML, or CSV, or missing expected rows), carrying a message
/// that describes the failure.
/// </remarks>
public sealed class ExchangeRateFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateFormatException" /> class.
    /// </summary>
    public ExchangeRateFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateFormatException" /> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    public ExchangeRateFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateFormatException" /> class with the specified message
    /// and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public ExchangeRateFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
