// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.Clamp.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Extensions;

public static partial class MoneyOfTCurrencyExtensions
{
    /// <summary>
    /// Clamps <paramref name="value" /> to the inclusive range <c>[min, max]</c>.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The lower bound (inclusive).</param>
    /// <param name="max">The upper bound (inclusive).</param>
    /// <returns><paramref name="value" /> when it lies within the range, otherwise the nearer bound.</returns>
    /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
    public static Money<TCurrency> Clamp<TCurrency>(Money<TCurrency> value, Money<TCurrency> min, Money<TCurrency> max)
        where TCurrency : ICurrency
    {
        return min.Amount > max.Amount
            ? throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Invalid_ClampMinGreaterThanMax, min, max),
                nameof(min))
            : value.Amount < min.Amount
            ? min
            : value.Amount > max.Amount
                ? max
                : value;
    }
}
