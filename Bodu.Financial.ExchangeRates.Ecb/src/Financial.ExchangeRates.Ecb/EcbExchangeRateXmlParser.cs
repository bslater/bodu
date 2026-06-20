// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateXmlParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Parses the European Central Bank's <c>eurofxref</c> XML feeds into a normalized <see cref="EcbExchangeRateTable" />.
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
internal static class EcbExchangeRateXmlParser
{
    /// <summary>The ECB <c>eurofxref</c> vocabulary namespace in which the <c>Cube</c> elements are declared.</summary>
    private static readonly XNamespace s_ecbNamespace = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";

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
    /// Thrown when the content is well-formed XML but does not match the expected <c>eurofxref</c> layout.
    /// </exception>
    public static EcbExchangeRateTable Parse(Stream stream, EcbExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);

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
    public static EcbExchangeRateTable Parse(XDocument document, EcbExchangeRateOptions options)
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

        var observations = new List<EcbExchangeRateObservation>();

        foreach (XElement dayCube in dayCubes)
        {
            XAttribute? time = dayCube.Attribute("time");
            if (time is null || !TryParseDate(time.Value, out DateOnly date))
                continue;

            foreach (XElement rateCube in dayCube.Elements(s_ecbNamespace + "Cube"))
            {
                if (TryParseCell(rateCube, date, options, out EcbExchangeRateObservation observation))
                    observations.Add(observation);
            }
        }

        return new EcbExchangeRateTable(observations);
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
    /// Parses a single inner <c>Cube</c> cell into an observation, resolving and validating the quote currency.
    /// </summary>
    /// <param name="rateCube">
    /// The inner <c>Cube</c> element carrying <c>currency</c> and <c>rate</c> attributes.
    /// </param>
    /// <param name="date">The observation date inherited from the enclosing day element.</param>
    /// <param name="options">The provider options supplying the currency alias map.</param>
    /// <param name="observation">
    /// When this method returns <see langword="true" />, the parsed observation; otherwise <see langword="default" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the cell yielded a usable observation; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseCell(XElement rateCube, DateOnly date, EcbExchangeRateOptions options, out EcbExchangeRateObservation observation)
    {
        observation = default;

        string? currency = rateCube.Attribute("currency")?.Value;
        string? rateText = rateCube.Attribute("rate")?.Value;
        if (string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(rateText))
            return false;

        if (options.CurrencyAliases.TryGetValue(currency, out string? alias))
            currency = alias;

        if (!IsValidIsoCode(currency))
            return false;

        if (!decimal.TryParse(rateText, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal rate) || rate <= 0m)
            return false;

        observation = new EcbExchangeRateObservation(date, currency, rate);
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
