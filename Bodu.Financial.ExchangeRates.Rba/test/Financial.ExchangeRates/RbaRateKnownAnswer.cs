// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A known-answer row pinning the rate an era's synthetic workbook holds for the Australian dollar against a quote
/// currency on a specific date.
/// </summary>
/// <remarks>
/// <para>
/// Each row asserts that <see cref="RbaRateProvider.GetRate(string, string, DateOnly, RateLookupOptions?)" /> returns
/// <see cref="ExpectedRate" /> for <c>AUD</c> to <see cref="Currency" /> on <see cref="Date" />, when the workbook
/// named by <see cref="SourceFileName" /> is loaded. The whole read path - compound file, BIFF8 decode, and RBA
/// mapping - is exercised against a rate the test computed from the content <see cref="RbaTestWorkbook" /> wrote.
/// </para>
/// <para>
/// <see cref="Type" /> records why the row was chosen for its currency in the source workbook: the first, last,
/// minimum, maximum, or median rate.
/// </para>
/// </remarks>
public sealed record RbaRateKnownAnswer
    : IKat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaRateKnownAnswer" /> class.
    /// </summary>
    /// <param name="sourceFileName">
    /// The era workbook the rate is read from, for example <c>2023-current.xls</c>.
    /// </param>
    /// <param name="type">Why the row was selected (first, last, minimum, maximum, or median) for its workbook.</param>
    /// <param name="date">The observation date.</param>
    /// <param name="currency">The ISO 4217 code of the quote currency.</param>
    /// <param name="expectedRate">The workbook's rate of one Australian dollar in the quote currency.</param>
    public RbaRateKnownAnswer(string sourceFileName, string type, DateOnly date, string currency, decimal expectedRate)
    {
        SourceFileName = sourceFileName;
        Type = type;
        Date = date;
        Currency = currency;
        ExpectedRate = expectedRate;
    }

    /// <summary>
    /// Gets the era workbook the rate is read from.
    /// </summary>
    /// <value>The source workbook file name, for example <c>2023-current.xls</c>.</value>
    public string SourceFileName { get; }

    /// <summary>
    /// Gets the reason the row was selected for its workbook.
    /// </summary>
    /// <value>One of <c>First</c>, <c>Last</c>, <c>Min</c>, <c>Max</c>, or <c>Median</c>.</value>
    public string Type { get; }

    /// <summary>
    /// Gets the observation date.
    /// </summary>
    /// <value>The calendar date of the rate.</value>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the ISO 4217 code of the quote currency.
    /// </summary>
    /// <value>The code the provider serves the rate under.</value>
    public string Currency { get; }

    /// <summary>
    /// Gets the workbook's rate of one Australian dollar in the quote currency.
    /// </summary>
    /// <value>The expected exchange rate.</value>
    public decimal ExpectedRate { get; }

    /// <inheritdoc />
    public string Name =>
        $"{SourceFileName} {Type} AUD/{Currency} {Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
}
