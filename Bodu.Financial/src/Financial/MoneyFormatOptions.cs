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
    /// <value>The default <see cref="MoneyFormatOptions" />.</value>
    public static MoneyFormatOptions Default { get; } = new();

    /// <summary>
    /// Gets how the currency designator is rendered.
    /// </summary>
    /// <value>The configured <see cref="CurrencyDisplay" />.</value>
    public CurrencyDisplay CurrencyDisplay { get; init; } = CurrencyDisplay.IsoCode;

    /// <summary>
    /// Gets the culture used to render the numeric component, or <see langword="null" /> for the current culture.
    /// </summary>
    /// <value>The configured format provider, or <see langword="null" />.</value>
    public IFormatProvider? FormatProvider { get; init; }

    /// <summary>
    /// Gets a value indicating whether digit-group separators are emitted. Applies to the
    /// <see cref="CurrencyDisplay.None" /> presentation; currency presentations group according to the culture.
    /// </summary>
    /// <value><see langword="true" /> to include grouping.</value>
    public bool IncludeGrouping { get; init; } = true;

    /// <summary>
    /// Gets an explicit fractional-digit count overriding the currency's natural precision, or <see langword="null" />
    /// to use the currency's minor units.
    /// </summary>
    /// <value>The override precision, or <see langword="null" />.</value>
    public int? MinorUnitsOverride { get; init; }

    /// <summary>
    /// Gets a value indicating whether the currency designator is elided when the formatting culture's region currency
    /// matches the amount's currency.
    /// </summary>
    /// <value><see langword="true" /> to elide the designator on a culture match.</value>
    public bool ElideWhenCultureMatches { get; init; }
}
