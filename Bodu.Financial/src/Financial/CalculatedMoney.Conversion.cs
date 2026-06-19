// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.Conversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public readonly partial struct CalculatedMoney
{
    /// <summary>
    /// Materialises this high-precision amount as a settlement <see cref="Money" />, rounding according to
    /// <paramref name="context" />.
    /// </summary>
    /// <param name="context">
    /// The monetary context governing rounding and scale; <see langword="null" /> selects
    /// <see cref="MonetaryContext.Default" />.
    /// </param>
    /// <returns>The settled <see cref="Money" /> in this value's currency.</returns>
    /// <exception cref="InvalidOperationException">This value carries no ISO code (default-initialised).</exception>
    /// <exception cref="ArgumentException"><paramref name="context" /> is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="context" /> carries an out-of-range policy.
    /// </exception>
    /// <remarks>
    /// The scale is resolved from <paramref name="context" />: <see cref="ScalePolicy.CurrencyMinorUnits" /> rounds to
    /// the registered currency's minor units and <see cref="ScalePolicy.Unrounded" /> falls back to that same natural
    /// scale because a settlement value must carry a concrete precision. When the resolved scale differs from the
    /// registered minor units (or the currency is unregistered) the result carries the resolved scale explicitly.
    /// </remarks>
    public Money RoundToMoney(MonetaryContext? context = null)
    {
        if (string.IsNullOrEmpty(IsoCode))
            throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);

        MonetaryContext effective = context ?? MonetaryContext.Default;
        effective.Validate();

        bool registered = CurrencyResolution.TryGet(IsoCode, out CurrencyInfo? info) && info is not null;
        int currencyMinorUnits = registered ? info!.MinorUnits : 0;

        int scale = effective.ResolveScale(currencyMinorUnits);
        if (scale < 0)
            scale = currencyMinorUnits;

        decimal rounded = effective.Rounding.Round(_amount, scale);

        return registered && scale == currencyMinorUnits
            ? new Money(rounded, Code)
            : Money.FromExplicitScale(rounded, Code, scale);
    }

    /// <summary>
    /// Materialises this high-precision amount as a settlement <see cref="Money" />, rounding to the currency's minor
    /// units using the supplied midpoint rule.
    /// </summary>
    /// <param name="rounding">The midpoint-rounding rule applied at the currency's minor-unit precision.</param>
    /// <returns>The settled <see cref="Money" /> in this value's currency.</returns>
    /// <exception cref="InvalidOperationException">This value carries no ISO code (default-initialised).</exception>
    public Money RoundToMoney(MidpointRounding rounding) =>
        RoundToMoney(MonetaryContext.Default with { Rounding = new MidpointRoundingStrategy(rounding) });
}
