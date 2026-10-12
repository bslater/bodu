// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that the provider returns the exact rate an era's workbook holds for a currency and date, driven by known
/// answers computed from the content of the synthetic workbook of every RBA era.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RbaTestWorkbook" /> generates a workbook for each era of <see cref="RbaEraWorkbook.Default" />, and
/// <see cref="RbaFixtures.GetSheet" /> returns the content it was written from. For each era and each currency the
/// provider serves, the known answers pin the first, last, minimum, maximum and median rate the workbook holds: a
/// positive number cell, which placeholders, blanks and zeros are not. The point-lookup test runs every row against a
/// provider that loads the era's workbook, so every era is read end to end.
/// </para>
/// </remarks>
[TestClass]
public partial class RbaRateKnownAnswerTests
{
    /// <summary>
    /// Shared options (no disk cache) used to build providers.
    /// </summary>
    private static readonly RbaRateProviderOptions s_options = new() { EnableDiskCache = false };

    /// <summary>
    /// The cache of providers built per source workbook, so each workbook is parsed once across all rows.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Task<RbaRateProvider>> s_providers = new();

    /// <summary>
    /// Every known-answer row, computed from the content of every era's workbook.
    /// </summary>
    private static readonly RbaRateKnownAnswer[] s_allRows = BuildRows();

    /// <summary>
    /// Gets every known-answer row, as dynamic-data rows.
    /// </summary>
    /// <value>One object array per known-answer row.</value>
    public static IEnumerable<object[]> KnownAnswers =>
        s_allRows.Select(row => new object[] { row });

    /// <summary>
    /// Gets the distinct (workbook, currency) groups of the known-answer rows, as dynamic-data rows.
    /// </summary>
    /// <value>One object array of (source file, currency) per group.</value>
    public static IEnumerable<object[]> ClassificationGroups =>
        s_allRows
            .Select(row => (row.SourceFileName, row.Currency))
            .Distinct()
            .Select(group => new object[] { group.SourceFileName, group.Currency });

    /// <summary>
    /// Returns the rates an era's workbook holds for a currency, as the provider should serve them.
    /// </summary>
    /// <param name="sourceFileName">The era workbook file name.</param>
    /// <param name="currency">The ISO 4217 code of the quote currency.</param>
    /// <returns>Each date the currency's column holds a positive number, with that number, in date order.</returns>
    private static List<(DateOnly Date, decimal Rate)> GetPublishedRates(string sourceFileName, string currency)
    {
        RbaTestSheet sheet = RbaFixtures.GetSheet(sourceFileName);
        RbaTestColumn column = sheet.Columns.Single(candidate => string.Equals(candidate.CurrencyCode, currency, StringComparison.Ordinal));

        return [.. sheet.Dates
            .Select((date, row) => (Date: date, Cell: column.Cells[row]))
            .Where(entry => entry.Cell.Kind == RbaTestCellKind.Number && entry.Cell.Value > 0m)
            .Select(entry => (entry.Date, entry.Cell.Value))];
    }

    /// <summary>
    /// Computes the known-answer rows: for every era and every currency the provider serves from it, the first, last,
    /// minimum, maximum and median rate its workbook holds, the minimum, maximum and median taken at their earliest
    /// date.
    /// </summary>
    /// <returns>The known-answer rows.</returns>
    private static RbaRateKnownAnswer[] BuildRows()
    {
        List<RbaRateKnownAnswer> rows = [];
        foreach (RbaEraWorkbook era in RbaEraWorkbook.Default)
        {
            foreach (RbaTestColumn column in RbaFixtures.GetSheet(era.FileName).Columns)
            {
                // The provider serves a column only under a currency code the exchange-rate types model, so the SDR
                // column (XDR) and the trade-weighted index have no known answers.
                if (column.CurrencyCode is not { } currency || !CurrencyInfo.TryGetCurrencyCode(currency, out _))
                    continue;

                List<(DateOnly Date, decimal Rate)> rates = GetPublishedRates(era.FileName, currency);
                if (rates.Count == 0)
                    continue;

                decimal[] sorted = [.. rates.Select(static rate => rate.Rate).Order()];
                decimal min = sorted[0];
                decimal max = sorted[^1];
                decimal median = sorted[(sorted.Length - 1) / 2];

                rows.Add(ToRow(era.FileName, "First", currency, rates[0]));
                rows.Add(ToRow(era.FileName, "Last", currency, rates[^1]));
                rows.Add(ToRow(era.FileName, "Min", currency, rates.First(rate => rate.Rate == min)));
                rows.Add(ToRow(era.FileName, "Max", currency, rates.First(rate => rate.Rate == max)));
                rows.Add(ToRow(era.FileName, "Median", currency, rates.First(rate => rate.Rate == median)));
            }
        }

        return [.. rows];
    }

    /// <summary>
    /// Creates a known-answer row for a dated rate.
    /// </summary>
    /// <param name="sourceFileName">The era workbook file name.</param>
    /// <param name="type">Why the row was selected.</param>
    /// <param name="currency">The ISO 4217 code of the quote currency.</param>
    /// <param name="rate">The dated rate.</param>
    /// <returns>The known-answer row.</returns>
    private static RbaRateKnownAnswer ToRow(string sourceFileName, string type, string currency, (DateOnly Date, decimal Rate) rate) =>
        new(sourceFileName, type, rate.Date, currency, rate.Rate);

    /// <summary>
    /// Returns the provider for a source workbook, building and caching it on first use.
    /// </summary>
    /// <param name="sourceFileName">The era workbook file name.</param>
    /// <returns>A task that yields the provider with the workbook's era loaded.</returns>
    private static Task<RbaRateProvider> GetProviderAsync(string sourceFileName) =>
        s_providers.GetOrAdd(sourceFileName, BuildProviderAsync);

    /// <summary>
    /// Builds a provider over the synthetic workbooks, with the era of a workbook loaded.
    /// </summary>
    /// <param name="sourceFileName">The era workbook file name.</param>
    /// <returns>A task that yields the loaded provider.</returns>
    private static async Task<RbaRateProvider> BuildProviderAsync(string sourceFileName)
    {
        RbaEraWorkbook era = RbaEraWorkbook.Default.Single(e => string.Equals(e.FileName, sourceFileName, StringComparison.Ordinal));
        FixtureRbaRateTableSource source = new(s_options);

        RbaRateProvider provider = new(source, s_options);
        await provider.LoadEraAsync(era);
        return provider;
    }
}
