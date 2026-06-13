// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// The exception thrown when a Bank of England response does not match the expected IADB CSV layout — for example, when
/// an HTML error page is returned in place of the CSV grid.
/// </summary>
/// <remarks>
/// Reports a layout-level failure, such as a response that lacks the <c>DATE</c> header row, distinct from a transport
/// failure, which surfaces through the HTTP layer.
/// </remarks>
public sealed class BoeExchangeRateFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateFormatException" /> class.
    /// </summary>
    public BoeExchangeRateFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateFormatException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the layout failure.</param>
    public BoeExchangeRateFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoeExchangeRateFormatException" /> class with the specified message
    /// and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the layout failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public BoeExchangeRateFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
