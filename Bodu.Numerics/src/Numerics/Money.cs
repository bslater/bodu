// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Numerics;

/// <summary>
/// Represents a monetary amount expressed in the currency identified by the type parameter <typeparamref name="TCurrency" />.
/// </summary>
/// <typeparam name="TCurrency">
/// A marker type implementing <see cref="ICurrency" /> that binds the currency's ISO-style code and natural
/// decimal-place count to this monetary value.
/// </typeparam>
/// <param name="Amount">The signed monetary amount.</param>
/// <remarks>
/// <para>
/// Conversions between currencies are intentionally explicit: see <see cref="Convert{TTarget}(decimal, MidpointRounding)" />
/// for the in-place conversion that applies a caller-supplied rate, or
/// <see cref="MoneyExchangeRateExtensions.ConvertTo{TSource, TTarget}(Money{TSource}, IDatedExchangeRateProvider, DateOnly, ExchangeRateLookupOptions, MidpointRounding)" />
/// for the rate-resolving wrapper that consults an <see cref="IDatedExchangeRateProvider" />.
/// </para>
/// </remarks>
[DebuggerDisplay("{Amount} {IsoCode,nq}")]
public readonly record struct Money<TCurrency>(decimal Amount)
    where TCurrency : ICurrency
{
    /// <summary>
    /// Gets the ISO-style currency code of <typeparamref name="TCurrency" />.
    /// </summary>
    /// <returns>The three-character ISO-style code defined by <typeparamref name="TCurrency" />.</returns>
    public static string IsoCode => TCurrency.IsoCode;

    /// <summary>
    /// Gets the natural decimal-place count of <typeparamref name="TCurrency" /> used when rounding converted amounts.
    /// </summary>
    /// <returns>The decimal-place count defined by <typeparamref name="TCurrency" />.</returns>
    public static int DecimalPlaces => TCurrency.DecimalPlaces;

    /// <summary>
    /// Converts this amount to <typeparamref name="TTarget" /> by multiplying by <paramref name="rate" /> and rounding
    /// to the target currency's natural decimal-place count.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency.</typeparam>
    /// <param name="rate">The multiplier that converts an amount in <typeparamref name="TCurrency" /> to <typeparamref name="TTarget" />.</param>
    /// <param name="rounding">The rounding mode applied to the converted amount. Defaults to <see cref="MidpointRounding.ToEven" />.</param>
    /// <returns>A <see cref="Money{TTarget}" /> carrying the converted, rounded amount.</returns>
    /// <exception cref="OverflowException">Thrown if the unrounded conversion result exceeds the range of <see cref="decimal" />.</exception>
    public Money<TTarget> Convert<TTarget>(decimal rate, MidpointRounding rounding = MidpointRounding.ToEven)
        where TTarget : ICurrency
    {
        decimal raw = Amount * rate;
        decimal rounded = decimal.Round(raw, TTarget.DecimalPlaces, rounding);
        return new Money<TTarget>(rounded);
    }
}
