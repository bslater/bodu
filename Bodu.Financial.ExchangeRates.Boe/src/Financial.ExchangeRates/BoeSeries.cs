// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeSeries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Maps a quote currency to the Bank of England Interactive Statistical Database (IADB) series code that publishes its
/// daily spot rate against the pound sterling.
/// </summary>
/// <remarks>
/// The Bank of England publishes each currency's daily spot rate as a separate IADB series — for example,
/// <c>XUDLUSS</c> is the US dollar into Sterling rate. Each series value is the number of units of the quote currency
/// per one pound, so the provider treats it as a <c>GBP</c>-based rate. The default catalogue is exposed through
/// <see cref="Default" /> and can be overridden or extended through the provider options.
/// </remarks>
public sealed class BoeSeries
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoeSeries" /> class.
    /// </summary>
    /// <param name="quoteIsoCode">The quote-currency ISO code.</param>
    /// <param name="seriesCode">The IADB series code that publishes the currency's daily spot rate.</param>
    /// <param name="description">A short human-readable description of the series.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="quoteIsoCode" />, <paramref name="seriesCode" />, or <paramref name="description" />
    /// is <see langword="null" />.
    /// </exception>
    public BoeSeries(string quoteIsoCode, string seriesCode, string description)
    {
        ThrowHelper.ThrowIfNull(quoteIsoCode);
        ThrowHelper.ThrowIfNull(seriesCode);
        ThrowHelper.ThrowIfNull(description);

        QuoteIsoCode = quoteIsoCode;
        SeriesCode = seriesCode;
        Description = description;
    }

    /// <summary>
    /// Gets the quote-currency ISO code.
    /// </summary>
    /// <returns>The three-letter ISO code of the quote currency.</returns>
    public string QuoteIsoCode { get; }

    /// <summary>
    /// Gets the IADB series code that publishes the currency's daily spot rate.
    /// </summary>
    /// <returns>The series code (for example, <c>XUDLUSS</c>).</returns>
    public string SeriesCode { get; }

    /// <summary>
    /// Gets a short human-readable description of the series.
    /// </summary>
    /// <returns>The series description.</returns>
    public string Description { get; }

    /// <summary>
    /// Gets the default catalogue of Bank of England daily spot series, each quoting a currency against the pound.
    /// </summary>
    /// <returns>The ordered, immutable default series catalogue.</returns>
    public static IReadOnlyList<BoeSeries> Default { get; } =
    [
        new("USD", "XUDLUSS", "US dollar into Sterling"),
        new("EUR", "XUDLERS", "Euro into Sterling"),
        new("JPY", "XUDLJYS", "Japanese yen into Sterling"),
        new("CHF", "XUDLSFS", "Swiss franc into Sterling"),
        new("AUD", "XUDLADS", "Australian dollar into Sterling"),
        new("CAD", "XUDLCDS", "Canadian dollar into Sterling"),
        new("DKK", "XUDLDKS", "Danish krone into Sterling"),
        new("NOK", "XUDLNKS", "Norwegian krone into Sterling"),
        new("SEK", "XUDLSKS", "Swedish krona into Sterling"),
        new("HKD", "XUDLHDS", "Hong Kong dollar into Sterling"),
        new("SGD", "XUDLSGS", "Singapore dollar into Sterling"),
    ];
}
