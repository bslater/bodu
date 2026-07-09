// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Represents one observation date of a parsed RBA workbook and the rate values recorded for each currency series on
/// that date.
/// </summary>
internal sealed class RbaRateRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateRow" /> class.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="values">
    /// The per-series rate values, positionally aligned to the table's series; <see langword="null" /> entries denote a
    /// blank cell.
    /// </param>
    internal RbaRateRow(DateOnly date, decimal?[] values)
    {
        Date = date;
        Values = values;
    }

    /// <summary>
    /// Gets the observation date.
    /// </summary>
    /// <value>The calendar date of the observations.</value>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the per-series rate values, positionally aligned to the owning table's series.
    /// </summary>
    /// <value>A read-only list of nullable decimals; a <see langword="null" /> entry denotes a blank cell.</value>
    public IReadOnlyList<decimal?> Values { get; }
}
