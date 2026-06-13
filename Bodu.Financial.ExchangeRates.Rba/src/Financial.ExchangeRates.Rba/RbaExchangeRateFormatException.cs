// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// The exception thrown when an RBA workbook is a valid spreadsheet but does not match the expected RBA exchange-rate
/// layout.
/// </summary>
/// <remarks>
/// Reports a layout-level failure — a missing <c>Data</c> sheet, absent header rows, or no data rows — distinct from a
/// malformed compound file or BIFF stream, which surface through the lower reader layers.
/// </remarks>
public sealed class RbaExchangeRateFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateFormatException" /> class.
    /// </summary>
    public RbaExchangeRateFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateFormatException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the layout failure.</param>
    public RbaExchangeRateFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateFormatException" /> class with the specified message
    /// and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the layout failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public RbaExchangeRateFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
