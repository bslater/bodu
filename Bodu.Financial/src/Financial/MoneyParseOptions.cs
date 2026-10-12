// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Configures how <see cref="Money.Parse(string, MoneyParseOptions)" /> and its <c>TryParse</c> counterpart interpret
/// monetary text.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
/// using Bodu.Financial.Currencies;
///
/// // RoundTripOnly pairs with the "R" format specifier for storage and wire formats.
/// var wire = Money.From(1234.56m, CurrencyCode.USD).ToString("R");   // "USD 1234.56"
/// Money restored = Money.Parse(wire, new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly });
///
/// // LenientImport trims white space and upcases the ISO code - for spreadsheets and external feeds.
/// Money imported = Money.Parse("  1234.56 usd ", new MoneyParseOptions { Mode = MoneyParseMode.LenientImport });
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed record MoneyParseOptions
{
    /// <summary>
    /// Gets the shared default options: strict ISO parsing, current-culture numeric interpretation, and rejection of
    /// unregistered currencies.
    /// </summary>
    /// <value>The default <see cref="MoneyParseOptions" />.</value>
    public static MoneyParseOptions Default { get; } = new();

    /// <summary>
    /// Gets the parse mode applied to the input.
    /// </summary>
    /// <value>The configured <see cref="MoneyParseMode" />.</value>
    public MoneyParseMode Mode { get; init; } = MoneyParseMode.StrictIso;

    /// <summary>
    /// Gets the culture used to interpret the numeric component, or <see langword="null" /> to use the current culture.
    /// <see cref="MoneyParseMode.RoundTripOnly" /> ignores it and always reads the invariant round-trip notation.
    /// </summary>
    /// <value>The configured format provider, or <see langword="null" />.</value>
    public IFormatProvider? FormatProvider { get; init; }

    /// <summary>
    /// Gets the currency lookup used to resolve symbols under <see cref="MoneyParseMode.CultureAware" />, or
    /// <see langword="null" /> to use a default <see cref="CurrencyLookupService" />.
    /// </summary>
    /// <value>The configured lookup, or <see langword="null" />.</value>
    /// <remarks>
    /// The default <see cref="CurrencyLookupService" /> resolves no symbols, because the shipped catalogue carries
    /// none; supply a lookup whose <see cref="CurrencyInfo" /> records carry <see cref="CurrencyInfo.Symbol" /> or
    /// <see cref="CurrencyInfo.AlternativeSymbols" /> data to parse symbol text.
    /// </remarks>
    public ICurrencyLookup? CurrencyLookup { get; init; }
}
