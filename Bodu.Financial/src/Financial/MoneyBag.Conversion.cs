// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.Conversion.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public sealed partial class MoneyBag
{
    /// <summary>
    /// Converts the entire bag to a single target currency by applying the supplied rate provider to every non-target
    /// balance and aggregating into the destination precision per
    /// <see cref="MoneyBagConversionRoundingPolicy.SumRawThenRound" />.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rates">The provider that returns the rate for each (from, to) pair.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rates" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">
    /// <paramref name="rates" /> cannot supply a rate for one of the bag's currencies.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A rate returned by <paramref name="rates" /> is zero or negative.
    /// </exception>
    public Money<TTarget> ConvertTo<TTarget>(IExchangeRateProvider rates)
        where TTarget : ICurrency =>
        ConvertTo<TTarget>(rates, MoneyBagConversionRoundingPolicy.SumRawThenRound);

    /// <summary>
    /// Converts the entire bag to a single target currency using the explicit rounding policy.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rates">The provider that returns the rate for each (from, to) pair.</param>
    /// <param name="policy">The rounding policy applied during aggregation.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rates" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy" /> is not a defined value.</exception>
    /// <exception cref="KeyNotFoundException">
    /// <paramref name="rates" /> cannot supply a rate for one of the bag's currencies.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A rate returned by <paramref name="rates" /> is zero or negative.
    /// </exception>
    public Money<TTarget> ConvertTo<TTarget>(IExchangeRateProvider rates, MoneyBagConversionRoundingPolicy policy)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(rates);
        return ConvertCore<TTarget>(rates.GetRate, policy);
    }

    /// <summary>
    /// Converts the entire bag to a single target currency using a delegate-based rate lookup.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rateLookup">A delegate that returns the rate for a (from, to) pair.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rateLookup" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// A rate returned by <paramref name="rateLookup" /> is zero or negative.
    /// </exception>
    public Money<TTarget> ConvertTo<TTarget>(Func<string, string, decimal> rateLookup)
        where TTarget : ICurrency =>
        ConvertTo<TTarget>(rateLookup, MoneyBagConversionRoundingPolicy.SumRawThenRound);

    /// <summary>
    /// Converts the entire bag to a single target currency using a delegate-based rate lookup and the supplied rounding
    /// policy.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rateLookup">A delegate that returns the rate for a (from, to) pair.</param>
    /// <param name="policy">The rounding policy applied during aggregation.</param>
    /// <returns>The aggregated <see cref="Money{TTarget}" /> total.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rateLookup" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy" /> is not a defined value.</exception>
    /// <exception cref="InvalidOperationException">
    /// A rate returned by <paramref name="rateLookup" /> is zero or negative.
    /// </exception>
    public Money<TTarget> ConvertTo<TTarget>(Func<string, string, decimal> rateLookup, MoneyBagConversionRoundingPolicy policy)
        where TTarget : ICurrency
    {
        ThrowHelper.ThrowIfNull(rateLookup);
        return ConvertCore<TTarget>(rateLookup, policy);
    }

    /// <summary>
    /// Shared core of the conversion paths. Validates each rate is strictly positive and applies the supplied rounding
    /// policy.
    /// </summary>
    /// <typeparam name="TTarget">The destination currency type.</typeparam>
    /// <param name="rateLookup">A delegate that returns the rate for a (from, to) pair.</param>
    /// <param name="policy">The rounding policy.</param>
    /// <returns>The aggregated total.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy" /> is not a defined value.</exception>
    /// <exception cref="InvalidOperationException">
    /// A rate returned by <paramref name="rateLookup" /> is zero or negative.
    /// </exception>
    private Money<TTarget> ConvertCore<TTarget>(Func<string, string, decimal> rateLookup, MoneyBagConversionRoundingPolicy policy)
        where TTarget : ICurrency
    {
        FinancialThrowHelper.ThrowIfMoneyBagRoundingPolicyUndefined(policy);

        var targetIso = CurrencyMetadata<TTarget>.Value.IsoCode;

        if (policy == MoneyBagConversionRoundingPolicy.SumRawThenRound)
        {
            var total = 0m;
            foreach (KeyValuePair<string, decimal> entry in _balances)
            {
                if (string.Equals(entry.Key, targetIso, StringComparison.Ordinal))
                {
                    total += entry.Value;
                }
                else
                {
                    var rate = ValidateRate(rateLookup, entry.Key, targetIso);
                    total += entry.Value * rate;
                }
            }

            return new Money<TTarget>(total);
        }

        // RoundEachCurrencyThenSum: round each converted balance to TTarget's precision before adding it in.
        Money<TTarget> running = Money<TTarget>.Zero;
        foreach (KeyValuePair<string, decimal> entry in _balances)
        {
            Money<TTarget> contribution;
            if (string.Equals(entry.Key, targetIso, StringComparison.Ordinal))
            {
                contribution = new Money<TTarget>(entry.Value);
            }
            else
            {
                var rate = ValidateRate(rateLookup, entry.Key, targetIso);
                contribution = new Money<TTarget>(entry.Value * rate);
            }

            running += contribution;
        }

        return running;
    }

    /// <summary>
    /// Looks up the rate and validates it is strictly positive.
    /// </summary>
    /// <param name="rateLookup">The lookup delegate.</param>
    /// <param name="from">The source ISO code.</param>
    /// <param name="to">The destination ISO code.</param>
    /// <returns>The validated rate.</returns>
    /// <exception cref="InvalidOperationException">The rate is zero or negative.</exception>
    private static decimal ValidateRate(Func<string, string, decimal> rateLookup, string from, string to)
    {
        var rate = rateLookup(from, to);
        if (rate <= 0m)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_RateNotStrictlyPositive,
                    from,
                    to,
                    rate));
        }

        return rate;
    }
}
