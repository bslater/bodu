// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.Conversion.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Numerics;

namespace Bodu.Financial;

public readonly partial struct Money<TCurrency>
{
    /// <summary>
    /// Converts this amount to <typeparamref name="TTarget" /> at the supplied exchange rate, rounding the result to
    /// the minor-unit precision of <typeparamref name="TTarget" />.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency.</typeparam>
    /// <param name="exchangeRate">
    /// The exchange rate, expressed as units of <typeparamref name="TTarget" /> per single unit of
    /// <typeparamref name="TCurrency" />. Must be strictly positive.
    /// </param>
    /// <param name="rounding">The midpoint-rounding rule applied when narrowing to the target precision.</param>
    /// <returns>The converted monetary amount in <typeparamref name="TTarget" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="exchangeRate" /> is zero or negative. A zero rate is treated as a programmer error
    /// in real FX code; for the rare legitimate scalar-zero case, construct the destination amount directly.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when <c>amount × exchangeRate</c> falls outside the range of <see cref="decimal" />.
    /// </exception>
    public Money<TTarget> Convert<TTarget>(decimal exchangeRate, MidpointRounding rounding = MidpointRounding.ToEven)
        where TTarget : ICurrency
    {
        FinancialThrowHelper.ThrowIfExchangeRateNotPositive(exchangeRate);

        return new Money<TTarget>(_amount * exchangeRate, rounding);
    }

    /// <summary>
    /// Returns a new <see cref="Money{TCurrency}" /> with the amount rounded to <paramref name="decimals" /> fractional
    /// digits using the specified rule.
    /// </summary>
    /// <param name="decimals">
    /// The number of fractional digits to keep. Must be non-negative and not greater than the currency's
    /// <see cref="MinorUnits" />.
    /// </param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The rounded amount.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="decimals" /> is negative or greater than <see cref="MinorUnits" />.
    /// </exception>
    /// <remarks>
    /// Use this to coarsen below the currency's natural precision — for example, rounding USD to whole dollars before
    /// display. Rounding above <see cref="MinorUnits" /> has no effect because the stored amount is already limited to
    /// the minor-unit precision.
    /// </remarks>
    public Money<TCurrency> Round(int decimals, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        ThrowHelper.ThrowIfNegative(decimals);
        ThrowHelper.ThrowIfGreaterThan(decimals, CurrencyMetadata<TCurrency>.Value.MinorUnits);

        return Money<TCurrency>.FromNormalizedAmount(decimal.Round(_amount, decimals, rounding));
    }

    /// <summary>
    /// Converts this amount to an exact <see cref="Bodu.Numerics.Fraction{T}" /> over <see cref="BigInteger" /> for
    /// arithmetic that must not round at each step.
    /// </summary>
    /// <returns>The amount as an exact rational value.</returns>
    /// <remarks>
    /// Use <see cref="ToFraction" /> together with
    /// <see cref="FromFraction(Bodu.Numerics.Fraction{BigInteger}, MidpointRounding)" /> to defer rounding until the
    /// end of a calculation chain. For example, multiplying by an exchange-rate fraction and a fee-percentage fraction
    /// before converting back to <see cref="Money{TCurrency}" /> rounds only once on return.
    /// </remarks>
    public Fraction<BigInteger> ToFraction() =>
        Fraction<BigInteger>.FromDecimal(_amount);

    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from an exact rational value, rounding to the currency's minor-unit
    /// precision using the specified rule. The rounding is performed directly in <see cref="BigInteger" /> arithmetic
    /// without an intermediate <see cref="decimal" /> step, so values that exceed <see cref="decimal" />'s 28-digit
    /// precision retain their exact rounding direction.
    /// </summary>
    /// <param name="value">The exact amount.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The rounded monetary amount.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the rounded result cannot be represented as a <see cref="decimal" />.
    /// </exception>
    public static Money<TCurrency> FromFraction(Fraction<BigInteger> value, MidpointRounding rounding = MidpointRounding.ToEven)
    {
        CurrencyMetadataDescriptor metadata = CurrencyMetadata<TCurrency>.Value;
        BigInteger numerator = value.Numerator;
        BigInteger denominator = value.Denominator;

        BigInteger scaledNumerator = numerator * BigInteger.Pow(10, metadata.MinorUnits);
        BigInteger roundedMinorUnits = DivideRound(scaledNumerator, denominator, rounding);
        var amount = (decimal)roundedMinorUnits / metadata.MinorUnitFactor;
        return FromNormalizedAmount(amount);
    }

    /// <summary>
    /// Multiplies this amount by an exact rational factor and rounds the result to the currency's minor-unit precision.
    /// </summary>
    /// <param name="factor">The exact multiplier.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The product rounded to <c>TCurrency.MinorUnits</c>.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the result cannot be represented as a <see cref="decimal" />.
    /// </exception>
    /// <remarks>
    /// Equivalent to <c>FromFraction(ToFraction() * factor, rounding)</c> but expresses the common multiply-then-round
    /// pattern in a single call. Inherits the exact <see cref="BigInteger" /> rounding behaviour of
    /// <see cref="FromFraction(Bodu.Numerics.Fraction{BigInteger}, MidpointRounding)" />.
    /// </remarks>
    public Money<TCurrency> MultiplyExact(Fraction<BigInteger> factor, MidpointRounding rounding = MidpointRounding.ToEven) =>
        FromFraction(ToFraction() * factor, rounding);

    /// <summary>
    /// Divides <paramref name="numerator" /> by <paramref name="denominator" /> as <see cref="BigInteger" /> arithmetic
    /// and rounds the quotient to an integer using <paramref name="rounding" />.
    /// </summary>
    /// <param name="numerator">The dividend.</param>
    /// <param name="denominator">The divisor; must be non-zero.</param>
    /// <param name="rounding">The midpoint-rounding rule.</param>
    /// <returns>The rounded integer quotient.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rounding" /> is not a defined value.</exception>
    /// <remarks>
    /// Directed modes (<see cref="MidpointRounding.ToZero" />, <see cref="MidpointRounding.ToPositiveInfinity" />,
    /// <see cref="MidpointRounding.ToNegativeInfinity" />) apply to every non-zero remainder, not only at midpoints.
    /// Midpoint-only modes (<see cref="MidpointRounding.AwayFromZero" />, <see cref="MidpointRounding.ToEven" />) defer
    /// to the nearest-rounding helper.
    /// </remarks>
    private static BigInteger DivideRound(BigInteger numerator, BigInteger denominator, MidpointRounding rounding)
    {
        if (denominator.IsZero)
            throw new DivideByZeroException();

        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        var quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
        return remainder.IsZero
            ? quotient
            : rounding switch
        {
            MidpointRounding.ToZero => quotient,

            MidpointRounding.ToPositiveInfinity =>
                numerator.Sign > 0 ? quotient + BigInteger.One : quotient,

            MidpointRounding.ToNegativeInfinity =>
                numerator.Sign < 0 ? quotient - BigInteger.One : quotient,

            MidpointRounding.AwayFromZero =>
                RoundNearest(numerator, denominator, quotient, remainder, tiesAwayFromZero: true),

            MidpointRounding.ToEven =>
                RoundNearest(numerator, denominator, quotient, remainder, tiesAwayFromZero: false),

            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, FinancialResourceStrings.Arg_OutOfRange_UnsupportedMidpointRounding),
        };
    }

    /// <summary>
    /// Rounds the quotient to the nearest integer based on the magnitude of the remainder, with midpoint ties broken by
    /// <paramref name="tiesAwayFromZero" /> (true for <see cref="MidpointRounding.AwayFromZero" />, false for
    /// <see cref="MidpointRounding.ToEven" />).
    /// </summary>
    /// <param name="numerator">The original dividend (its sign determines round-away direction).</param>
    /// <param name="denominator">The positive divisor.</param>
    /// <param name="quotient">The truncated quotient from <see cref="BigInteger.DivRem" />.</param>
    /// <param name="remainder">The non-zero remainder.</param>
    /// <param name="tiesAwayFromZero">
    /// Whether to round midpoints away from zero (true) or to the nearest even (false).
    /// </param>
    /// <returns>The rounded integer.</returns>
    private static BigInteger RoundNearest(
        BigInteger numerator,
        BigInteger denominator,
        BigInteger quotient,
        BigInteger remainder,
        bool tiesAwayFromZero)
    {
        BigInteger absDoubleRem = BigInteger.Abs(remainder) * 2;
        var cmp = absDoubleRem.CompareTo(denominator);

        var roundAway = cmp > 0 || (cmp == 0 && (tiesAwayFromZero || !quotient.IsEven));

        return !roundAway
            ? quotient
            : numerator.Sign >= 0
            ? quotient + BigInteger.One
            : quotient - BigInteger.One;
    }
}
