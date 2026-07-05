// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.Conversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

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
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// using Bodu.Financial;
    ///
    /// var calc = new CalculatedMoney(10m, CurrencyCode.USD).Divide(3m);   // 3.3333... USD, unrounded
    ///
    /// Money defaultRule = calc.RoundToMoney();                            // 3.33 USD (banker's rounding)
    /// Money awayFromZero = calc.RoundToMoney(MidpointRounding.AwayFromZero);
    ///]]>
    /// </code>
    /// </example>
    public Money RoundToMoney(MonetaryContext? context = null)
    {
        if (_code == CurrencyCode.None)
            throw new InvalidOperationException(FinancialResourceStrings.Op_Invalid_MoneyRequiresCurrency);

        MonetaryContext effective = context ?? MonetaryContext.Default;
        effective.Validate();

        bool registered = CurrencyResolution.TryGet(IsoCodeOrEmpty, out CurrencyInfo? info) && info is not null;
        int currencyMinorUnits = registered ? info!.MinorUnits : 0;

        int scale = effective.ResolveScale(currencyMinorUnits);
        if (scale < 0)
            scale = currencyMinorUnits;

        decimal rounded = effective.Rounding.Round(_amount, scale);

        // Apply the context's cash-rounding policy at settlement: when the caller opts into cash rounding and the
        // currency declares a physical cash increment, snap the settled amount to the nearest multiple of that
        // increment using the context's rounding strategy. The default (CashRoundingPolicy.None) leaves the amount
        // untouched, preserving existing behaviour.
        if (registered
            && effective.CashRounding == CashRoundingPolicy.CurrencyCashIncrement
            && info!.CashRoundingIncrement > 0m)
        {
            decimal increment = info.CashRoundingIncrement;
            rounded = effective.Rounding.Round(rounded / increment, 0) * increment;
        }

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
