// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImfReportParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Financial.Currencies;
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Document;
using Bodu.Text.Delimited.Reader;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses the IMF Representative Exchange Rates monthly report — a tab-separated grid of every reported currency across
/// each business day of the month — into a normalized <see cref="ImfRateTable" />.
/// </summary>
/// <remarks>
/// <para>
/// Tokenization is delegated to <see cref="DelimitedDocument" /> (the RFC 4180 reader in <c>Bodu.Text.Delimited</c>);
/// this type adds the report-specific interpretation. The report is a sequence of blocks: a title line, a
/// <c>Currency</c> header row whose remaining cells are business-day dates in <c>MMMM dd, yyyy</c> form, and one data
/// row per currency. When a month has more business days than fit in one block the report continues in a second block
/// with its own <c>Currency</c> header redefining the current date columns. A trailing <c>Notes:</c> section closes the
/// report.
/// </para>
/// <para>
/// A currency label ending in <c>(1)</c> is quoted in US dollars per currency unit; every other label is quoted in
/// currency units per US dollar. Both are normalized to units of the quote currency per one US dollar, so the stored
/// rate is always USD-anchored. The <c>U.S. dollar</c> anchor row (all ones) and any currency the options do not
/// resolve to an ISO code are skipped, as are missing cells (the literal <c>NA</c>) and non-positive values.
/// </para>
/// </remarks>
internal static class ImfReportParser
{
    /// <summary>The anchor row label whose values are all one and is skipped.</summary>
    private const string UsDollarLabel = "U.S. dollar";

    /// <summary>The token marking a missing observation.</summary>
    private const string NotAvailable = "NA";

    /// <summary>The delimited-reader options used to read the report: a tab-separated positional grid with no header row, trimmed fields, and a tolerant reaction to ragged or malformed rows.</summary>
    private static readonly DelimitedReaderOptions s_tsvOptions = new()
    {
        Delimiter = '\t',
        NoHeader = true,
        TrimFields = true,
        FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
    };

    /// <summary>
    /// Parses the report content from a stream.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the report.</param>
    /// <param name="options">The provider options supplying the currency-name map.</param>
    /// <returns>The parsed, normalized table.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the content contains no recognizable header row or yields no usable observations.
    /// </exception>
    public static ImfRateTable Parse(Stream stream, ImfRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);

        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd(), options);
    }

    /// <summary>
    /// Parses the report content from a string.
    /// </summary>
    /// <param name="content">The tab-separated report content.</param>
    /// <param name="options">The provider options supplying the currency-name map.</param>
    /// <returns>The parsed, normalized table.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the content contains no recognizable header row or yields no usable observations.
    /// </exception>
    public static ImfRateTable Parse(string content, ImfRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(content);
        ThrowHelper.ThrowIfNull(options);

        DelimitedDocument? document = null;
        try
        {
            try
            {
                document = DelimitedDocument.Parse(Encoding.UTF8.GetBytes(content), s_tsvOptions);
            }
            catch (DelimitedFormatException ex)
            {
                throw new ExchangeRateFormatException(ImfResourceStrings.Format_Invalid_ImfReportHeader, ex);
            }

            var observations = new List<ImfRateObservation>();
            DateOnly?[]? currentDates = null;
            bool sawHeader = false;

            DelimitedElement root = document.RootElement;
            int rowCount = root.GetArrayLength();
            for (int index = 0; index < rowCount; index++)
            {
                DelimitedElement row = root[index];
                if (row.GetArrayLength() == 0)
                    continue;

                string first = row[0].GetString();
                if (first.Length == 0)
                    continue;

                if (string.Equals(first, "Currency", StringComparison.Ordinal))
                {
                    currentDates = ParseHeaderDates(row);
                    sawHeader = true;
                    continue;
                }

                if (first.StartsWith("Representative Exchange Rates", StringComparison.Ordinal))
                    continue;

                if (first.StartsWith("Notes", StringComparison.Ordinal) ||
                    first.StartsWith('(') ||
                    first.StartsWith("These ", StringComparison.Ordinal))
                {
                    currentDates = null;
                    continue;
                }

                if (currentDates is null)
                    continue;

                AppendDataRow(row, currentDates, options, observations);
            }

            if (!sawHeader)
                throw new ExchangeRateFormatException(ImfResourceStrings.Format_Invalid_ImfReportHeader);

            if (observations.Count == 0)
            {
                throw new ExchangeRateFormatException(
                    string.Format(CultureInfo.CurrentCulture, ImfResourceStrings.Format_Invalid_ImfReportNoRows, string.Empty));
            }

            return new ImfRateTable(observations);
        }
        finally
        {
            document?.Dispose();
        }
    }

    /// <summary>
    /// Parses the business-day dates from a <c>Currency</c> header row into a per-column lookup.
    /// </summary>
    /// <param name="row">The header row whose first cell is <c>Currency</c>.</param>
    /// <returns>
    /// A per-column array of dates; index 0 (the <c>Currency</c> cell) and any unparsable column is
    /// <see langword="null" />.
    /// </returns>
    private static DateOnly?[] ParseHeaderDates(DelimitedElement row)
    {
        var dates = new DateOnly?[row.GetArrayLength()];
        for (int i = 1; i < dates.Length; i++)
        {
            dates[i] = DateOnly.TryParseExact(row[i].GetString(), "MMMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                ? date
                : null;
        }

        return dates;
    }

    /// <summary>
    /// Parses one currency data row, appending a normalized observation for every dated cell that carries a positive
    /// rate.
    /// </summary>
    /// <param name="row">The data row to parse.</param>
    /// <param name="currentDates">The per-column dates from the active header row.</param>
    /// <param name="options">The provider options supplying the currency-name map.</param>
    /// <param name="observations">The accumulator to append parsed observations to.</param>
    private static void AppendDataRow(
        DelimitedElement row,
        DateOnly?[] currentDates,
        ImfRateProviderOptions options,
        List<ImfRateObservation> observations)
    {
        string name = NormalizeLabel(row[0].GetString(), out bool usdPerUnit);
        if (string.Equals(name, UsDollarLabel, StringComparison.Ordinal))
            return;

        if (!options.TryResolveCurrency(name, out CurrencyCode code))
            return;

        string isoCode = code.ToString();
        int limit = Math.Min(row.GetArrayLength(), currentDates.Length);
        for (int i = 1; i < limit; i++)
        {
            if (currentDates[i] is not { } date)
                continue;

            string cell = row[i].GetString();
            if (cell.Length == 0 || string.Equals(cell, NotAvailable, StringComparison.Ordinal))
                continue;

            if (!decimal.TryParse(cell, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out decimal value) || value <= 0m)
                continue;

            decimal rate = usdPerUnit ? 1m / value : value;
            observations.Add(new ImfRateObservation(date, isoCode, rate));
        }
    }

    /// <summary>
    /// Strips the trailing footnote markers from a currency label, reporting whether the label carried the <c>(1)</c>
    /// direction marker.
    /// </summary>
    /// <param name="label">The raw currency label, for example <c>Euro(1)</c>.</param>
    /// <param name="usdPerUnit">
    /// On return, <see langword="true" /> when the label carried the <c>(1)</c> marker (US dollars per currency unit);
    /// otherwise <see langword="false" />.
    /// </param>
    /// <returns>The footnote-stripped, trimmed label.</returns>
    private static string NormalizeLabel(string label, out bool usdPerUnit)
    {
        usdPerUnit = false;

        string current = label.Trim();
        while (current.EndsWith(')'))
        {
            int open = current.LastIndexOf('(');
            if (open < 0)
                break;

            string footnote = current.Substring(open);
            if (string.Equals(footnote, "(1)", StringComparison.Ordinal))
                usdPerUnit = true;

            current = current.Substring(0, open).Trim();
        }

        return current;
    }
}
