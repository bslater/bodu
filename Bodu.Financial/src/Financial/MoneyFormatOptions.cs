// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Configures how a <see cref="MoneyFormatter" /> renders a monetary value. Translates to the format-specifier
/// vocabulary understood by <see cref="Money" /> and <see cref="Money{TCurrency}" />.
/// </summary>
public sealed record MoneyFormatOptions
{
    /// <summary>
    /// Gets the shared default options: ISO-code designator, current-culture grouping, and the currency's natural
    /// minor-unit precision.
    /// </summary>
    /// <returns>The default <see cref="MoneyFormatOptions" />.</returns>
    public static MoneyFormatOptions Default { get; } = new();

    /// <summary>
    /// Gets how the currency designator is rendered.
    /// </summary>
    /// <returns>The configured <see cref="CurrencyDisplay" />.</returns>
    public CurrencyDisplay CurrencyDisplay { get; init; } = CurrencyDisplay.IsoCode;

    /// <summary>
    /// Gets the culture used to render the numeric component, or <see langword="null" /> for the current culture.
    /// </summary>
    /// <returns>The configured format provider, or <see langword="null" />.</returns>
    public IFormatProvider? FormatProvider { get; init; }

    /// <summary>
    /// Gets a value indicating whether digit-group separators are emitted. Applies to the
    /// <see cref="CurrencyDisplay.None" /> presentation; currency presentations group according to the culture.
    /// </summary>
    /// <returns><see langword="true" /> to include grouping.</returns>
    public bool IncludeGrouping { get; init; } = true;

    /// <summary>
    /// Gets an explicit fractional-digit count overriding the currency's natural precision, or <see langword="null" />
    /// to use the currency's minor units.
    /// </summary>
    /// <returns>The override precision, or <see langword="null" />.</returns>
    public int? MinorUnitsOverride { get; init; }

    /// <summary>
    /// Gets a value indicating whether the currency designator is elided when the formatting culture's region currency
    /// matches the amount's currency.
    /// </summary>
    /// <returns><see langword="true" /> to elide the designator on a culture match.</returns>
    public bool ElideWhenCultureMatches { get; init; }
}
