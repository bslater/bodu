// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateSeries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Describes one currency column of a parsed RBA workbook: its position and the RBA metadata read from the header rows.
/// </summary>
internal sealed class RbaExchangeRateSeries
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaExchangeRateSeries" /> class.
    /// </summary>
    /// <param name="columnIndex">The zero-based worksheet column index of the series.</param>
    /// <param name="seriesId">The RBA series identifier.</param>
    /// <param name="currencyCode">The resolved quote-currency ISO code.</param>
    /// <param name="units">The RBA units label.</param>
    /// <param name="title">The RBA title.</param>
    /// <param name="description">The RBA description.</param>
    internal RbaExchangeRateSeries(int columnIndex, string seriesId, string currencyCode, string units, string title, string description)
    {
        ColumnIndex = columnIndex;
        SeriesId = seriesId;
        CurrencyCode = currencyCode;
        Units = units;
        Title = title;
        Description = description;
    }

    /// <summary>
    /// Gets the zero-based worksheet column index of the series.
    /// </summary>
    /// <returns>The column index.</returns>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets the RBA series identifier.
    /// </summary>
    /// <returns>The RBA series mnemonic, or an empty string when absent.</returns>
    public string SeriesId { get; }

    /// <summary>
    /// Gets the resolved quote-currency ISO code (after alias mapping).
    /// </summary>
    /// <returns>The three-letter quote-currency ISO code.</returns>
    public string CurrencyCode { get; }

    /// <summary>
    /// Gets the RBA units label.
    /// </summary>
    /// <returns>The units text, or an empty string when absent.</returns>
    public string Units { get; }

    /// <summary>
    /// Gets the RBA title.
    /// </summary>
    /// <returns>The title text, or an empty string when absent.</returns>
    public string Title { get; }

    /// <summary>
    /// Gets the RBA description.
    /// </summary>
    /// <returns>The description text, or an empty string when absent.</returns>
    public string Description { get; }
}
