// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Renders <see cref="Money" /> and <see cref="Money{TCurrency}" /> values according to a fixed
/// <see cref="MoneyFormatOptions" />, translating the options into the format-specifier vocabulary of the monetary
/// types.
/// </summary>
public sealed class MoneyFormatter
{
    /// <summary>
    /// The options governing this formatter's output.
    /// </summary>
    private readonly MoneyFormatOptions _options;

    /// <summary>
    /// The pre-computed format specifier derived from <see cref="_options" />.
    /// </summary>
    private readonly string _format;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyFormatter" /> class.
    /// </summary>
    /// <param name="options">The formatting options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="options" />.<see cref="MoneyFormatOptions.CurrencyDisplay" /> is not a defined value.
    /// </exception>
    public MoneyFormatter(MoneyFormatOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        _options = options;
        _format = BuildFormat(options);
    }

    /// <summary>
    /// Formats a runtime-tagged <see cref="Money" /> value.
    /// </summary>
    /// <param name="money">The value to format.</param>
    /// <returns>The formatted string.</returns>
    public string Format(Money money) =>
        money.ToString(_format, _options.FormatProvider ?? CultureInfo.CurrentCulture);

    /// <summary>
    /// Formats a strongly-typed <see cref="Money{TCurrency}" /> value.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <param name="money">The value to format.</param>
    /// <returns>The formatted string.</returns>
    public string Format<TCurrency>(Money<TCurrency> money)
        where TCurrency : ICurrency =>
        money.ToString(_format, _options.FormatProvider ?? CultureInfo.CurrentCulture);

    /// <summary>
    /// Builds the format specifier string corresponding to <paramref name="options" />.
    /// </summary>
    /// <param name="options">The formatting options.</param>
    /// <returns>The format specifier understood by the monetary types.</returns>
    private static string BuildFormat(MoneyFormatOptions options)
    {
        string specifier = options.CurrencyDisplay switch
        {
            CurrencyDisplay.IsoCode => "G",
            CurrencyDisplay.Symbol => "C",
            CurrencyDisplay.EnglishName => "L",
            CurrencyDisplay.None => options.IncludeGrouping ? "N" : "F",
            _ => throw new ArgumentOutOfRangeException(
                nameof(options),
                options.CurrencyDisplay,
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CurrencyDisplayUndefined, options.CurrencyDisplay)),
        };

        // The "~" elide prefix and the numeric precision suffix apply only to designator-bearing specifiers.
        string elide = options.ElideWhenCultureMatches && options.CurrencyDisplay is CurrencyDisplay.IsoCode or CurrencyDisplay.Symbol or CurrencyDisplay.EnglishName
            ? "~"
            : string.Empty;

        string precision = options.MinorUnitsOverride is int p
            ? p.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        return string.Concat(elide, specifier, precision);
    }
}
