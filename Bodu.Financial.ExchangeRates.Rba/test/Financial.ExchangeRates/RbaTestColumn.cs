// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestColumn.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes one value column of a synthetic F11.1 data sheet: its header cells and its data cells.
/// </summary>
/// <remarks>
/// A header property that is <see langword="null" /> is written as a blank cell, as the RBA leaves the series
/// identifier of its legacy columns blank. The data cells are aligned with <see cref="RbaTestSheet.Dates" />.
/// </remarks>
internal sealed record RbaTestColumn
{
    /// <summary>
    /// Gets the text of the column's <c>Title</c> cell.
    /// </summary>
    /// <value>The title, for example <c>A$1=USD</c>, or <see langword="null" /> for a blank cell.</value>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the text of the column's <c>Description</c> cell.
    /// </summary>
    /// <value>The description, or <see langword="null" /> for a blank cell.</value>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the text of the column's <c>Frequency</c> cell.
    /// </summary>
    /// <value>The frequency; <c>Daily</c> unless set.</value>
    public string? Frequency { get; init; } = "Daily";

    /// <summary>
    /// Gets the text of the column's <c>Type</c> cell.
    /// </summary>
    /// <value>The series type; <c>Indicative</c> unless set.</value>
    public string? Type { get; init; } = "Indicative";

    /// <summary>
    /// Gets the text of the column's <c>Units</c> cell.
    /// </summary>
    /// <value>The units, for example <c>USD</c> or <c>Index</c>, or <see langword="null" /> for a blank cell.</value>
    public string? Units { get; init; }

    /// <summary>
    /// Gets the text of the column's <c>Source</c> cell.
    /// </summary>
    /// <value>The source, for example <c>RBA</c>, or <see langword="null" /> for a blank cell.</value>
    public string? Source { get; init; }

    /// <summary>
    /// Gets the date of the column's <c>Publication date</c> cell.
    /// </summary>
    /// <value>The publication date, or <see langword="null" /> for a blank cell.</value>
    public DateOnly? PublicationDate { get; init; }

    /// <summary>
    /// Gets the text of the column's <c>Series ID</c> cell.
    /// </summary>
    /// <value>The series identifier, for example <c>FXRUSD</c>, or <see langword="null" /> for a blank cell.</value>
    public string? SeriesId { get; init; }

    /// <summary>
    /// Gets the ISO 4217 code the parser should resolve the column to.
    /// </summary>
    /// <value>
    /// The code, for example <c>XDR</c> for the <c>SDR</c> column, or <see langword="null" /> when the column is not a
    /// currency, as the trade-weighted index is not.
    /// </value>
    public string? CurrencyCode { get; init; }

    /// <summary>
    /// Gets the number of decimal places the number format of the column's data cells shows.
    /// </summary>
    /// <value>From zero to four; four unless set.</value>
    public int Decimals { get; init; } = 4;

    /// <summary>
    /// Gets the column's data cells.
    /// </summary>
    /// <value>One cell per observation date, aligned with <see cref="RbaTestSheet.Dates" />.</value>
    public IReadOnlyList<RbaTestCell> Cells { get; init; } = [];
}
