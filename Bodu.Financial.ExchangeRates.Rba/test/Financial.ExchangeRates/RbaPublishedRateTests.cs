// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaPublishedRateTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that the provider, downloading the RBA's own era workbooks, returns rates the RBA published, typed by hand
/// from the RBA's publications so that the reader is held to its source rather than to the synthetic workbooks every
/// row of <see cref="RbaRateKnownAnswerTests" /> is computed from.
/// </summary>
/// <remarks>
/// <para>
/// The oracle is <see cref="s_rows" />: thirteen rates of the Australian dollar against the US dollar, the yen, the
/// rand and the euro across six of the RBA's eras, from the first row of its earliest file to the most recent row of
/// its current one. The rows take in the first and the last business day of an era, three leap days, the rand rate
/// the 2010-2013 workbook stores as the double <c>6.6754999999999995</c>, and a date from each side of the boundary
/// between the 2018-2022 era and the current one. Each constant was typed from the RBA file the row cites, read with
/// <c>xlrd</c> (an era workbook) or as text (the F11.1 CSV), never through the reader under test, on the retrieval
/// date the row carries. The F11.1 CSV, <c>https://www.rba.gov.au/statistics/tables/csv/f11.1-data.csv</c>, restates
/// the current era's workbook, and the three rows typed from it agree with <c>2023-current.xls</c> as retrieved the
/// same day. A row that fails may mean the reader is wrong, or that the RBA has revised a figure; the citation names
/// the file to re-read before the constant is changed.
/// </para>
/// <para>
/// The live test is environment-gated, as the Redis integration tests are: it runs only when
/// <c>BODU_TEST_RBA_LIVE</c> is set to a non-empty value, and reports inconclusive otherwise, so the default build
/// reaches no network. It is in the Stress tier, which every default run excludes, so it is run on demand with
/// <c>stress.runsettings</c>. The provider is built from default options against the RBA's live base address, with no
/// disk cache and a one-minute HTTP timeout, and the eras the rows need are downloaded once per test run and shared by
/// every row. The guard over the row set runs offline in every tier.
/// </para>
/// </remarks>
[TestClass]
public partial class RbaPublishedRateTests
{
    /// <summary>
    /// The environment variable that, set to any non-empty value, enables the live RBA test.
    /// </summary>
    private const string LiveEnvVar = "BODU_TEST_RBA_LIVE";

    /// <summary>
    /// The RBA's F11.1 CSV, which restates the current era's workbook.
    /// </summary>
    private const string CsvFile = "f11.1-data.csv";

    /// <summary>
    /// The day every source file was retrieved and its constants typed.
    /// </summary>
    private static readonly DateOnly s_retrievedOn = new(2026, 10, 31);

    /// <summary>
    /// The oracle: rates the RBA published, typed from the file each row cites.
    /// </summary>
    private static readonly RbaPublishedRateKnownAnswer[] s_rows =
    [
        // The first row of the RBA's earliest era file: 12 December 1983, the day the dollar floated.
        new(new DateOnly(1983, 12, 12), "USD", 0.9175m, "1983-1986.xls", "A$1=USD", s_retrievedOn),

        // The last business day of the 1983-1986 era.
        new(new DateOnly(1986, 12, 31), "JPY", 106.37m, "1983-1986.xls", "A$1=JPY", s_retrievedOn),

        // The first business day of the 1999-2002 era, and the first day the RBA quoted the euro.
        new(new DateOnly(1999, 1, 4), "EUR", 0.5175m, "1999-2002.xls", "A$1=EUR", s_retrievedOn),

        // The first business day of the 2010-2013 era. The workbook stores the figure as the double 6.6754999999999995,
        // which the reader has to round back to the four places the RBA published.
        new(new DateOnly(2010, 1, 4), "ZAR", 6.6755m, "2010-2013.xls", "A$1=ZAR", s_retrievedOn),

        // A leap day.
        new(new DateOnly(2012, 2, 29), "JPY", 86.92m, "2010-2013.xls", "A$1=JPY", s_retrievedOn),

        // The last business day of the 2010-2013 era.
        new(new DateOnly(2013, 12, 31), "EUR", 0.6485m, "2010-2013.xls", "A$1=EUR", s_retrievedOn),

        // The first business day of the 2014-2017 era.
        new(new DateOnly(2014, 1, 2), "ZAR", 9.3487m, "2014-2017.xls", "A$1=ZAR", s_retrievedOn),

        // A leap day.
        new(new DateOnly(2016, 2, 29), "USD", 0.7140m, "2014-2017.xls", "A$1=USD", s_retrievedOn),

        // The first business day of the 2018-2022 era.
        new(new DateOnly(2018, 1, 2), "USD", 0.7837m, "2018-2022.xls", "A$1=USD", s_retrievedOn),

        // The last business day of the 2018-2022 era, the business day before the current era opens.
        new(new DateOnly(2022, 12, 30), "JPY", 89.88m, "2018-2022.xls", "A$1=JPY", s_retrievedOn),

        // The first business day of the current era, from the F11.1 CSV.
        new(new DateOnly(2023, 1, 3), "USD", 0.6828m, CsvFile, "A$1=USD", s_retrievedOn),

        // A leap day, from the F11.1 CSV.
        new(new DateOnly(2024, 2, 29), "EUR", 0.6017m, CsvFile, "A$1=EUR", s_retrievedOn),

        // The most recent row of the F11.1 CSV as retrieved, the rate the RBA published on 30 October 2026.
        new(new DateOnly(2026, 10, 30), "USD", 0.7040m, CsvFile, "A$1=USD", s_retrievedOn),
    ];

    /// <summary>
    /// The provider over the RBA's live workbooks, built once by the first row that needs it and shared by the rest,
    /// or <see langword="null" /> until then.
    /// </summary>
    private static Task<RbaRateProvider>? s_provider;

    /// <summary>
    /// Guards <see cref="s_provider" />, so that rows run at once build one provider between them.
    /// </summary>
    private static readonly object s_providerLock = new();

    /// <summary>
    /// Gets every published-rate row, as dynamic-data rows.
    /// </summary>
    /// <value>One object array per row of <see cref="s_rows" />.</value>
    public static IEnumerable<object[]> PublishedRates =>
        s_rows.Select(row => new object[] { row });

    /// <summary>
    /// Reports whether the live test is enabled, which <see cref="LiveEnvVar" /> set to a non-empty value does.
    /// </summary>
    /// <returns><see langword="true" /> when the live test should run; otherwise <see langword="false" />.</returns>
    private static bool IsLiveTestEnabled() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LiveEnvVar));

    /// <summary>
    /// Returns the provider over the RBA's live workbooks, building it on first use.
    /// </summary>
    /// <returns>A task that yields the provider with every era the rows need loaded.</returns>
    private static Task<RbaRateProvider> GetProviderAsync()
    {
        lock (s_providerLock)
        {
            return s_provider ??= BuildProviderAsync();
        }
    }

    /// <summary>
    /// Builds a provider from default options against the RBA's live base address, with no disk cache, and downloads
    /// every era of the default catalog that covers a row's date.
    /// </summary>
    /// <returns>A task that yields the loaded provider.</returns>
    private static async Task<RbaRateProvider> BuildProviderAsync()
    {
        RbaRateProviderOptions options = new() { EnableDiskCache = false, HttpTimeout = TimeSpan.FromMinutes(1) };
        RbaRateProvider provider = new(options);

        foreach (RbaEraWorkbook era in RbaEraWorkbook.Default)
        {
            if (s_rows.Any(row => era.Covers(row.Date)))
                await provider.LoadEraAsync(era);
        }

        return provider;
    }
}
