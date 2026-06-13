// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooExchangeRateDateRangeException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// The exception thrown when a date range supplied to the provider is invalid — for example, when the end date precedes
/// the start date.
/// </summary>
public sealed class YahooExchangeRateDateRangeException
    : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateDateRangeException" /> class.
    /// </summary>
    public YahooExchangeRateDateRangeException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateDateRangeException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the invalid range.</param>
    public YahooExchangeRateDateRangeException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateDateRangeException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the invalid range.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public YahooExchangeRateDateRangeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateDateRangeException" /> class with the specified
    /// message and the name of the parameter that caused the exception.
    /// </summary>
    /// <param name="message">A message that describes the invalid range.</param>
    /// <param name="paramName">The name of the parameter that caused the exception.</param>
    public YahooExchangeRateDateRangeException(string? message, string? paramName)
        : base(message, paramName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YahooExchangeRateDateRangeException" /> class with the specified
    /// message, parameter name, and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the invalid range.</param>
    /// <param name="paramName">The name of the parameter that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public YahooExchangeRateDateRangeException(string? message, string? paramName, Exception? innerException)
        : base(message, paramName, innerException)
    {
    }
}
