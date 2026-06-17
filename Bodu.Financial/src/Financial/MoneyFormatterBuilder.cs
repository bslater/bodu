// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatterBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Fluently composes a <see cref="MoneyFormatter" /> for complex formatting scenarios.
/// </summary>
public sealed class MoneyFormatterBuilder
{
    /// <summary>The options accumulated by the builder.</summary>
    private MoneyFormatOptions _options = MoneyFormatOptions.Default;

    /// <summary>
    /// Renders the currency designator as the ISO 4217 code.
    /// </summary>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder WithIsoCode()
    {
        _options = _options with { CurrencyDisplay = CurrencyDisplay.IsoCode };
        return this;
    }

    /// <summary>
    /// Renders the currency designator as the culture's native symbol.
    /// </summary>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder WithSymbol()
    {
        _options = _options with { CurrencyDisplay = CurrencyDisplay.Symbol };
        return this;
    }

    /// <summary>
    /// Renders the currency designator as the English-language currency name.
    /// </summary>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder WithEnglishName()
    {
        _options = _options with { CurrencyDisplay = CurrencyDisplay.EnglishName };
        return this;
    }

    /// <summary>
    /// Renders the bare numeric amount with no currency designator.
    /// </summary>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder WithNumericOnly()
    {
        _options = _options with { CurrencyDisplay = CurrencyDisplay.None };
        return this;
    }

    /// <summary>
    /// Sets the culture used to render the numeric component.
    /// </summary>
    /// <param name="formatProvider">The culture provider.</param>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder WithCulture(IFormatProvider formatProvider)
    {
        _options = _options with { FormatProvider = formatProvider };
        return this;
    }

    /// <summary>
    /// Sets whether digit-group separators are emitted for the numeric-only presentation.
    /// </summary>
    /// <param name="includeGrouping"><see langword="true" /> to include grouping.</param>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder WithGrouping(bool includeGrouping)
    {
        _options = _options with { IncludeGrouping = includeGrouping };
        return this;
    }

    /// <summary>
    /// Overrides the fractional-digit count.
    /// </summary>
    /// <param name="minorUnits">The explicit precision to render.</param>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder WithMinorUnits(int minorUnits)
    {
        _options = _options with { MinorUnitsOverride = minorUnits };
        return this;
    }

    /// <summary>
    /// Elides the currency designator when the formatting culture's region currency matches the amount's currency.
    /// </summary>
    /// <returns>This builder.</returns>
    public MoneyFormatterBuilder ElideWhenCultureMatches()
    {
        _options = _options with { ElideWhenCultureMatches = true };
        return this;
    }

    /// <summary>
    /// Builds the configured <see cref="MoneyFormatter" />.
    /// </summary>
    /// <returns>The formatter.</returns>
    public MoneyFormatter Build() =>
        new(_options);
}
