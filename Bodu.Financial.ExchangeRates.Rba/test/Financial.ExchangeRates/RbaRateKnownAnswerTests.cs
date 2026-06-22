// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that the provider returns the exact rate RBA published for a known currency and date, driven by an embedded
/// table of factual known-answer rows.
/// </summary>
/// <remarks>
/// <para>
/// The rows are loaded from the embedded <c>rba-known-answers.json</c> data set, which pins, for each RBA workbook and
/// currency, the first, last, minimum, maximum, and closest-to-median published rates. The point-lookup test runs the
/// subset whose workbook fixture is embedded in this assembly (currently <c>2023-current.xls</c>); committing another
/// era's workbook under <c>test/Fixtures/</c> automatically activates its rows.
/// </para>
/// </remarks>
[TestClass]
public partial class RbaRateKnownAnswerTests
{
    /// <summary>
    /// Shared options (no disk cache) used to build providers and to resolve currency aliases consistently.
    /// </summary>
    private static readonly RbaExchangeRateOptions s_options = new() { EnableDiskCache = false };

    /// <summary>
    /// The cache of providers built per source workbook, so each workbook is parsed once across all rows.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Task<RbaExchangeRateProvider>> s_providers = new();

    /// <summary>
    /// The options used to deserialize the embedded known-answer data set.
    /// </summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Every known-answer row from the embedded data set.
    /// </summary>
    private static readonly RbaRateKnownAnswer[] s_allRows = LoadRows();

    /// <summary>
    /// The workbook fixtures embedded in this test assembly, against which rows can actually be run.
    /// </summary>
    private static readonly IReadOnlySet<string> s_embeddedWorkbooks = RbaFixtures.EmbeddedWorkbookFileNames();

    /// <summary>
    /// Gets the known-answer rows whose workbook fixture is embedded, as dynamic-data rows.
    /// </summary>
    /// <returns>One object array per runnable known-answer row.</returns>
    public static IEnumerable<object[]> KnownAnswers =>
        s_allRows.Where(row => s_embeddedWorkbooks.Contains(row.SourceFileName)).Select(row => new object[] { row });

    /// <summary>
    /// Gets the distinct (workbook, currency) groups whose workbook fixture is embedded, as dynamic-data rows.
    /// </summary>
    /// <returns>One object array of (source file, currency) per runnable group.</returns>
    public static IEnumerable<object[]> ClassificationGroups =>
        s_allRows
            .Where(row => s_embeddedWorkbooks.Contains(row.SourceFileName))
            .Select(row => (row.SourceFileName, row.Currency))
            .Distinct()
            .Select(group => new object[] { group.SourceFileName, group.Currency });

    /// <summary>
    /// Deserializes the embedded known-answer data set.
    /// </summary>
    /// <returns>The known-answer rows.</returns>
    private static RbaRateKnownAnswer[] LoadRows()
    {
        string json = RbaFixtures.ReadText(RbaFixtures.KnownAnswerData);
        return JsonSerializer.Deserialize<RbaRateKnownAnswer[]>(json, s_jsonOptions) ?? [];
    }

    /// <summary>
    /// Resolves an RBA currency label to the ISO code the provider serves, applying the configured alias map.
    /// </summary>
    /// <param name="currency">The RBA currency label.</param>
    /// <returns>The ISO code to look up.</returns>
    private static string ResolveCurrency(string currency) =>
        s_options.CurrencyAliases.TryGetValue(currency, out string? iso) ? iso : currency;

    /// <summary>
    /// Returns the provider for a source workbook, building and caching it on first use.
    /// </summary>
    /// <param name="sourceFileName">The RBA workbook file name.</param>
    /// <returns>A task that yields the provider with the workbook's era loaded.</returns>
    private static Task<RbaExchangeRateProvider> GetProviderAsync(string sourceFileName) =>
        s_providers.GetOrAdd(sourceFileName, BuildProviderAsync);

    /// <summary>
    /// Builds a provider from an embedded workbook fixture, with the matching era loaded.
    /// </summary>
    /// <param name="sourceFileName">The RBA workbook file name.</param>
    /// <returns>A task that yields the loaded provider.</returns>
    private static async Task<RbaExchangeRateProvider> BuildProviderAsync(string sourceFileName)
    {
        RbaEra era = RbaEra.Default.Single(e => string.Equals(e.FileName, sourceFileName, StringComparison.Ordinal));
        FixtureRbaExchangeRateTableSource source = new(s_options, sourceFileName);

        RbaExchangeRateProvider provider = new(source, s_options);
        await provider.LoadEraAsync(era);
        return provider;
    }
}
