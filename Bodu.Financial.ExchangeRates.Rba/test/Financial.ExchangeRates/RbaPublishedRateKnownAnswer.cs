// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaPublishedRateKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A known-answer row pinning a rate the Reserve Bank of Australia published for the Australian dollar against a quote
/// currency on a specific date, typed by hand from the RBA's own file.
/// </summary>
/// <remarks>
/// <para>
/// Each row asserts that <see cref="RbaRateProvider.GetRate(string, string, DateOnly, RateLookupOptions?)" /> returns
/// <see cref="ExpectedRate" /> for <c>AUD</c> to <see cref="Currency" /> on <see cref="Date" /> once the era that
/// holds the date has been downloaded from the RBA. Unlike <see cref="RbaRateKnownAnswer" />, whose expectation is
/// computed from the synthetic workbook the test wrote, the expected rate here was read from the publication named by
/// <see cref="SourceFile" /> with a tool that is not the reader under test, so the row holds the reader to the RBA
/// rather than to itself.
/// </para>
/// <para>
/// <see cref="SourceFile" />, <see cref="SeriesTitle" /> and <see cref="RetrievedOn" /> cite the publication: the file
/// among the RBA's statistical tables, the title of the series in it (<c>A$1=USD</c>, for one), and the day the file
/// was retrieved and the constant typed from it.
/// </para>
/// <para>
/// The record is <see cref="SerializableAttribute">serializable</see> so that MSTest can write each row and run it as a
/// test of its own; without that, MSTest folds every row into one test. It holds its dates as day numbers because the
/// .NET 8 <c>DataContractJsonSerializer</c> writes a <see cref="DateOnly" /> as an empty object and reads it back as
/// <see cref="DateOnly.MinValue" />, which a test run discovered separately would then query.
/// </para>
/// </remarks>
[Serializable]
public sealed record RbaPublishedRateKnownAnswer
    : IKat
{
    /// <summary>
    /// The observation date's <see cref="DateOnly.DayNumber" />, which every serializer MSTest uses can write.
    /// </summary>
    private readonly int _dayNumber;

    /// <summary>
    /// The retrieval date's <see cref="DateOnly.DayNumber" />, held as <see cref="_dayNumber" /> is.
    /// </summary>
    private readonly int _retrievedOnDayNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="RbaPublishedRateKnownAnswer" /> class.
    /// </summary>
    /// <param name="date">The observation date.</param>
    /// <param name="currency">The ISO 4217 code of the quote currency.</param>
    /// <param name="expectedRate">The published rate of one Australian dollar in the quote currency.</param>
    /// <param name="sourceFile">
    /// The RBA file the rate was typed from, for example <c>2010-2013.xls</c> or <c>f11.1-data.csv</c>.
    /// </param>
    /// <param name="seriesTitle">The title of the series in that file, for example <c>A$1=USD</c>.</param>
    /// <param name="retrievedOn">The day the file was retrieved and the constant typed.</param>
    public RbaPublishedRateKnownAnswer(DateOnly date, string currency, decimal expectedRate, string sourceFile, string seriesTitle, DateOnly retrievedOn)
    {
        _dayNumber = date.DayNumber;
        Currency = currency;
        ExpectedRate = expectedRate;
        SourceFile = sourceFile;
        SeriesTitle = seriesTitle;
        _retrievedOnDayNumber = retrievedOn.DayNumber;
    }

    /// <summary>
    /// Gets the observation date.
    /// </summary>
    /// <value>The calendar date of the rate.</value>
    public DateOnly Date => DateOnly.FromDayNumber(_dayNumber);

    /// <summary>
    /// Gets the ISO 4217 code of the quote currency.
    /// </summary>
    /// <value>The code the provider serves the rate under.</value>
    public string Currency { get; }

    /// <summary>
    /// Gets the published rate of one Australian dollar in the quote currency.
    /// </summary>
    /// <value>The expected exchange rate, as the RBA printed it.</value>
    public decimal ExpectedRate { get; }

    /// <summary>
    /// Gets the RBA file the rate was typed from.
    /// </summary>
    /// <value>
    /// An era workbook's file name under <c>https://www.rba.gov.au/statistics/tables/xls-hist/</c>, or
    /// <c>f11.1-data.csv</c> under <c>https://www.rba.gov.au/statistics/tables/csv/</c>.
    /// </value>
    public string SourceFile { get; }

    /// <summary>
    /// Gets the title of the series in the source file.
    /// </summary>
    /// <value>The series title the RBA prints above the column, for example <c>A$1=USD</c>.</value>
    public string SeriesTitle { get; }

    /// <summary>
    /// Gets the day the source file was retrieved and the constant typed from it.
    /// </summary>
    /// <value>The retrieval date.</value>
    public DateOnly RetrievedOn => DateOnly.FromDayNumber(_retrievedOnDayNumber);

    /// <inheritdoc />
    public string Name =>
        $"{SourceFile} {SeriesTitle} {Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
}
