// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.Abs.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.Extensions;

public static partial class MoneyOfTCurrencyExtensions
{
#if BODU_EXTENSION_MEMBERS

    extension<TCurrency>(Money<TCurrency> value)
        where TCurrency : ICurrency
    {
        /// <summary>
        /// Gets the absolute value of this instance.
        /// </summary>
        /// <value>A <see cref="Money{TCurrency}" /> whose amount is <c>|Amount|</c>.</value>
        public Money<TCurrency> Abs =>
            value.Amount < 0m ? -value : value;
    }

#else

    /// <summary>
    /// Returns the absolute value of the specified instance.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="value">The monetary value whose magnitude is taken.</param>
    /// <returns>A <see cref="Money{TCurrency}" /> whose amount is <c>|Amount|</c>.</returns>
    public static Money<TCurrency> Abs<TCurrency>(this Money<TCurrency> value)
        where TCurrency : ICurrency =>
        value.Amount < 0m ? -value : value;

#endif
}
