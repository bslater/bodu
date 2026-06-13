// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// The exception thrown when an ECB feed is well-formed XML but does not match the expected <c>eurofxref</c> layout, or
/// when the downloaded content is not well-formed XML at all.
/// </summary>
/// <remarks>
/// Reports a layout-level failure — a missing envelope or no dated <c>Cube</c> rows — as well as malformed XML, both of
/// which indicate the feed could not be interpreted as ECB reference-rate data.
/// </remarks>
public sealed class EcbExchangeRateFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateFormatException" /> class.
    /// </summary>
    public EcbExchangeRateFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateFormatException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the layout failure.</param>
    public EcbExchangeRateFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EcbExchangeRateFormatException" /> class with the specified message
    /// and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the layout failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public EcbExchangeRateFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
