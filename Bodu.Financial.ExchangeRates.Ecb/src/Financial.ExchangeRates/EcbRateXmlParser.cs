// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateXmlParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Parses the European Central Bank's <c>eurofxref</c> XML feeds into a normalized <see cref="EcbRateTable" />.
/// </summary>
/// <remarks>
/// <para>
/// The <c>eurofxref</c> format nests three levels of <c>Cube</c> element in the
/// <c>http://www.ecb.int/vocabulary/2002-08-01/eurofxref</c> namespace: an outer container, a middle element carrying a
/// <c>time</c> attribute for each published day, and inner elements carrying <c>currency</c> and <c>rate</c>
/// attributes. Each rate gives the number of units of the quoted currency per one euro.
/// </para>
/// <para>
/// Cells whose rate is absent, non-numeric, or not strictly positive are skipped rather than treated as an error,
/// because the ECB occasionally omits a currency for a given day; a feed with no usable dated rows is, however,
/// reported as a layout failure.
/// </para>
/// </remarks>
internal static class EcbRateXmlParser
{
    /// <summary>The ECB <c>eurofxref</c> vocabulary namespace URI in which the <c>Cube</c> elements are declared.</summary>
    private const string EcbNamespaceUri = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";

    /// <summary>The ECB <c>eurofxref</c> vocabulary namespace in which the <c>Cube</c> elements are declared.</summary>
    private static readonly XNamespace s_ecbNamespace = EcbNamespaceUri;

    /// <summary>The reader settings <see cref="XDocument.Load(Stream)" /> uses, so a streamed parse accepts and rejects the same documents a loaded one does.</summary>
    private static readonly XmlReaderSettings s_readerSettings = new()
    {
        IgnoreWhitespace = true,
        DtdProcessing = DtdProcessing.Parse,
        MaxCharactersFromEntities = 10_000_000,
        XmlResolver = null,
    };

    /// <summary>
    /// Parses the feed content from a stream.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the feed XML.</param>
    /// <param name="options">The provider options supplying the currency alias map.</param>
    /// <returns>The parsed, normalized table.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the content is not well-formed XML or does not match the expected <c>eurofxref</c> layout.
    /// </exception>
    /// <remarks>
    /// A seekable stream is read forward once with an <see cref="XmlReader" /> rather than loaded into an
    /// <see cref="XDocument" />, so parsing the full history does not first build a tree of its hundreds of thousands
    /// of elements. A feed whose day elements nest inside one another, which the published layout never does, is read
    /// again through the document model, as is a stream that cannot seek, so every feed yields what it did before.
    /// </remarks>
    public static EcbRateTable Parse(Stream stream, EcbRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);

        if (stream.CanSeek)
        {
            long start = stream.Position;
            if (TryParseStreaming(stream, options, out EcbRateTable? table))
                return table;

            stream.Position = start;
        }

        XDocument document = Load(stream);
        return Parse(document, options);
    }

    /// <summary>
    /// Parses a loaded document into a normalized table.
    /// </summary>
    /// <param name="document">The loaded feed document.</param>
    /// <param name="options">The provider options supplying the currency alias map.</param>
    /// <returns>The parsed, normalized table.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the document does not match the expected <c>eurofxref</c> layout.
    /// </exception>
    public static EcbRateTable Parse(XDocument document, EcbRateProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(document);
        ThrowHelper.ThrowIfNull(options);

        if (document.Root is null)
            throw new ExchangeRateFormatException(EcbResourceStrings.Format_Invalid_EcbEnvelopeMissing);

        var dayCubes = document.Descendants(s_ecbNamespace + "Cube")
            .Where(static cube => cube.Attribute("time") is not null)
            .ToList();

        if (dayCubes.Count == 0)
            throw new ExchangeRateFormatException(EcbResourceStrings.Format_Invalid_EcbNoDataRows);

        var observations = new List<EcbRateObservation>();

        foreach (XElement dayCube in dayCubes)
        {
            XAttribute? time = dayCube.Attribute("time");
            if (time is null || !TryParseDate(time.Value, out DateOnly date))
                continue;

            foreach (XElement rateCube in dayCube.Elements(s_ecbNamespace + "Cube"))
            {
                if (TryParseCell(rateCube.Attribute("currency")?.Value, rateCube.Attribute("rate")?.Value, date, options, out EcbRateObservation observation))
                    observations.Add(observation);
            }
        }

        return new EcbRateTable(observations);
    }

    /// <summary>
    /// Loads a feed stream into an <see cref="XDocument" />, mapping malformed XML to a layout failure.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the feed XML.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the stream does not contain well-formed XML.
    /// </exception>
    private static XDocument Load(Stream stream)
    {
        try
        {
            return XDocument.Load(stream);
        }
        catch (XmlException ex)
        {
            throw new ExchangeRateFormatException(EcbResourceStrings.Format_Invalid_EcbMalformedXml, ex);
        }
    }

    /// <summary>
    /// Reads a feed forward with an <see cref="XmlReader" />, collecting the cells of each day element in document
    /// order.
    /// </summary>
    /// <param name="stream">The stream positioned at the start of the feed XML.</param>
    /// <param name="options">The provider options supplying the currency alias map.</param>
    /// <param name="table">
    /// When this method returns <see langword="true" />, the parsed table; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the feed was parsed; <see langword="false" /> when a day element nests inside
    /// another, a layout whose cells the document-model parse orders differently, so the caller parses it that way
    /// instead.
    /// </returns>
    /// <exception cref="ExchangeRateFormatException">
    /// Thrown when the stream does not contain well-formed XML, or holds no day element.
    /// </exception>
    private static bool TryParseStreaming(Stream stream, EcbRateProviderOptions options, [NotNullWhen(true)] out EcbRateTable? table)
    {
        table = null;
        var observations = new List<EcbRateObservation>();
        int dayCount = 0;
        int openDayDepth = -1;
        DateOnly? openDate = null;

        try
        {
            using XmlReader reader = XmlReader.Create(stream, s_readerSettings);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Depth == openDayDepth)
                        openDayDepth = -1;

                    continue;
                }

                if (reader.NodeType != XmlNodeType.Element
                    || !string.Equals(reader.LocalName, "Cube", StringComparison.Ordinal)
                    || !string.Equals(reader.NamespaceURI, EcbNamespaceUri, StringComparison.Ordinal))
                {
                    continue;
                }

                string? time = reader.GetAttribute("time");

                // A cube directly inside the open day element is one of its cells.
                if (openDayDepth >= 0 && reader.Depth == openDayDepth + 1)
                {
                    if (time is not null)
                        return false;

                    if (openDate is { } date
                        && TryParseCell(reader.GetAttribute("currency"), reader.GetAttribute("rate"), date, options, out EcbRateObservation observation))
                    {
                        observations.Add(observation);
                    }

                    continue;
                }

                if (time is null)
                    continue;

                if (openDayDepth >= 0)
                    return false;

                dayCount++;
                openDate = TryParseDate(time, out DateOnly parsed) ? parsed : null;
                openDayDepth = reader.IsEmptyElement ? -1 : reader.Depth;
            }
        }
        catch (XmlException ex)
        {
            throw new ExchangeRateFormatException(EcbResourceStrings.Format_Invalid_EcbMalformedXml, ex);
        }

        if (dayCount == 0)
            throw new ExchangeRateFormatException(EcbResourceStrings.Format_Invalid_EcbNoDataRows);

        table = new EcbRateTable(observations);
        return true;
    }

    /// <summary>
    /// Parses a single inner <c>Cube</c> cell into an observation, resolving and validating the quote currency.
    /// </summary>
    /// <param name="currency">The cell's <c>currency</c> attribute, or <see langword="null" /> when absent.</param>
    /// <param name="rateText">The cell's <c>rate</c> attribute, or <see langword="null" /> when absent.</param>
    /// <param name="date">The observation date inherited from the enclosing day element.</param>
    /// <param name="options">The provider options supplying the currency alias map.</param>
    /// <param name="observation">
    /// When this method returns <see langword="true" />, the parsed observation; otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the cell yielded a usable observation; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseCell(string? currency, string? rateText, DateOnly date, EcbRateProviderOptions options, out EcbRateObservation observation)
    {
        observation = default;

        if (string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(rateText))
            return false;

        if (options.CurrencyAliases.TryGetValue(currency, out string? alias))
            currency = alias;

        if (!IsValidIsoCode(currency))
            return false;

        if (!decimal.TryParse(rateText, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal rate) || rate <= 0m)
            return false;

        observation = new EcbRateObservation(date, currency, rate);
        return true;
    }

    /// <summary>
    /// Parses an ISO 8601 calendar date in the <c>yyyy-MM-dd</c> form used by the feed.
    /// </summary>
    /// <param name="text">The date text.</param>
    /// <param name="date">
    /// When this method returns <see langword="true" />, the parsed date; otherwise <see langword="default" />.
    /// </param>
    /// <returns><see langword="true" /> when the text parsed; otherwise <see langword="false" />.</returns>
    private static bool TryParseDate(string text, out DateOnly date) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>
    /// Determines whether a candidate code is exactly three uppercase ASCII letters, the shape the
    /// <see cref="ExchangeRate" /> contract requires.
    /// </summary>
    /// <param name="code">The candidate ISO code.</param>
    /// <returns>
    /// <see langword="true" /> when the code is a valid ISO-style code; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsValidIsoCode(string code) =>
        code.Length == 3
        && char.IsAsciiLetterUpper(code[0])
        && char.IsAsciiLetterUpper(code[1])
        && char.IsAsciiLetterUpper(code[2]);
}
