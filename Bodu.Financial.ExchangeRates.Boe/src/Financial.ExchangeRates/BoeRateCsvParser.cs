// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateCsvParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Document;
using Bodu.Text.Delimited.Reader;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses the Bank of England Interactive Statistical Database (IADB) CSV column response into a normalized
/// <see cref="BoeRateTable" />.
/// </summary>
/// <remarks>
/// <para>
/// CSV tokenization is delegated to <see cref="DelimitedDocument" /> (the RFC 4180 reader in
/// <c>Bodu.Text.Delimited</c>); this type adds the IADB-specific interpretation. The column response is a small grid:
/// the first row is a header whose first cell is <c>DATE</c> and whose remaining cells are the requested series codes;
/// each subsequent row carries a <c>dd MMM yyyy</c> date followed by the rate for each series. Each rate gives the
/// number of units of the column's currency per one pound sterling.
/// </para>
/// <para>
/// Columns whose series code is not in the configured catalogue are ignored, and cells that are empty, non-numeric, or
/// not strictly positive are skipped, because the IADB leaves a cell blank when a currency was not quoted on a given
/// day. A response that lacks the <c>DATE</c> header — for example, an HTML error page returned in place of the CSV —
/// is reported as a format failure.
/// </para>
/// </remarks>
internal static class BoeRateCsvParser
{
    /// <summary>The delimited-reader options used to read the IADB CSV as a positional grid: RFC 4180 quoting, trimmed fields, and a tolerant reaction to ragged or malformed rows. The header row is interpreted by this parser, so the reader runs headerless.</summary>
    private static readonly DelimitedReaderOptions s_csvOptions = new()
    {
        NoHeader = true,
        TrimFields = true,
        FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
    };

    /// <summary>
    /// Parses the response content from a stream.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the CSV.</param>
    /// <param name="options">The provider options supplying the series catalogue.</param>
    /// <returns>The parsed, normalized table.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the content does not contain the expected IADB CSV header.
    /// </exception>
    public static BoeRateTable Parse(Stream stream, BoeRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);

        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd(), options);
    }

    /// <summary>
    /// Parses the response content from a string.
    /// </summary>
    /// <param name="content">The CSV content.</param>
    /// <param name="options">The provider options supplying the series catalogue.</param>
    /// <returns>The parsed, normalized table.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the content does not contain the expected IADB CSV header.
    /// </exception>
    public static BoeRateTable Parse(string content, BoeRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(content);
        ThrowHelper.ThrowIfNull(options);

        DelimitedDocument? document = null;
        try
        {
            try
            {
                document = DelimitedDocument.Parse(Encoding.UTF8.GetBytes(content), s_csvOptions);
            }
            catch (DelimitedFormatException ex)
            {
                throw new ExchangeRateFormatException(BoeResourceStrings.Format_Invalid_BoeHeaderMissing, ex);
            }

            DelimitedElement root = document.RootElement;
            int rowCount = root.GetArrayLength();
            if (rowCount == 0)
                throw new ExchangeRateFormatException(BoeResourceStrings.Format_Invalid_BoeHeaderMissing);

            List<string> headers = ReadFields(root[0]);
            if (headers.Count == 0 ||
                !string.Equals(headers[0], "DATE", StringComparison.OrdinalIgnoreCase))
            {
                throw new ExchangeRateFormatException(BoeResourceStrings.Format_Invalid_BoeHeaderMissing);
            }

            Dictionary<string, BoeSeries> byCode = BuildSeriesIndex(options);
            BoeSeries?[] columns = MapColumns(headers, byCode, out Dictionary<string, BoeSeries> present);

            var observations = new List<BoeRateObservation>();
            for (int index = 1; index < rowCount; index++)
            {
                DelimitedElement row = root[index];
                int fieldCount = row.GetArrayLength();
                if (fieldCount == 0 || !TryParseDate(row[0].GetString(), out DateOnly date))
                    continue;

                for (int column = 1; column < columns.Length && column < fieldCount; column++)
                {
                    BoeSeries? series = columns[column];
                    if (series is null)
                        continue;

                    if (TryParseRate(row[column].GetString(), out decimal rate))
                        observations.Add(new BoeRateObservation(date, series.QuoteIsoCode, rate));
                }
            }

            return new BoeRateTable(observations, present);
        }
        finally
        {
            document?.Dispose();
        }
    }

    /// <summary>
    /// Materializes the fields of a positional record.
    /// </summary>
    /// <param name="row">The record element.</param>
    /// <returns>The field values in column order.</returns>
    private static List<string> ReadFields(DelimitedElement row)
    {
        int count = row.GetArrayLength();
        var fields = new List<string>(count);
        for (int i = 0; i < count; i++)
            fields.Add(row[i].GetString());

        return fields;
    }

    /// <summary>
    /// Builds a lookup from IADB series code to its descriptor, comparing codes without case sensitivity.
    /// </summary>
    /// <param name="options">The provider options supplying the series catalogue.</param>
    /// <returns>The series index keyed by code.</returns>
    private static Dictionary<string, BoeSeries> BuildSeriesIndex(BoeRateProviderOptions options)
    {
        var index = new Dictionary<string, BoeSeries>(StringComparer.OrdinalIgnoreCase);
        foreach (BoeSeries series in options.Series)
            index[series.SeriesCode] = series;

        return index;
    }

    /// <summary>
    /// Maps each header column to its configured series, recording which currencies are present.
    /// </summary>
    /// <param name="headers">The parsed header row.</param>
    /// <param name="byCode">The series index keyed by code.</param>
    /// <param name="present">
    /// On return, the series descriptors for the columns present, keyed by quote ISO code.
    /// </param>
    /// <returns>A per-column array of series descriptors; <see langword="null" /> for unmapped columns.</returns>
    private static BoeSeries?[] MapColumns(IReadOnlyList<string> headers, Dictionary<string, BoeSeries> byCode, out Dictionary<string, BoeSeries> present)
    {
        var columns = new BoeSeries?[headers.Count];
        var found = new Dictionary<string, BoeSeries>(StringComparer.Ordinal);

        for (int column = 1; column < headers.Count; column++)
        {
            if (byCode.TryGetValue(headers[column], out BoeSeries? series))
            {
                columns[column] = series;
                found[series.QuoteIsoCode] = series;
            }
        }

        present = found;
        return columns;
    }

    /// <summary>
    /// Parses an IADB date in the <c>dd MMM yyyy</c> form used by the response.
    /// </summary>
    /// <param name="text">The date text.</param>
    /// <param name="date">
    /// When this method returns <see langword="true" />, the parsed date; otherwise <see langword="default" />.
    /// </param>
    /// <returns><see langword="true" /> when the text parsed; otherwise <see langword="false" />.</returns>
    private static bool TryParseDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>
    /// Parses a strictly positive rate from a cell.
    /// </summary>
    /// <param name="text">The cell text.</param>
    /// <param name="rate">
    /// When this method returns <see langword="true" />, the parsed rate; otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the cell holds a strictly positive number; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseRate(string text, out decimal rate) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out rate) && rate > 0m;
}
