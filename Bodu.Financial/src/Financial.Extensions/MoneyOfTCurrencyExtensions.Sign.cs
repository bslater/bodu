// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.Sign.cs" company="Bodu Pty. Ltd.">
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
        /// Gets the sign of this instance.
        /// </summary>
        /// <value>
        /// <c>-1</c> when the amount is negative, <c>0</c> when the amount is zero, and <c>1</c> when the amount is
        /// positive.
        /// </value>
        public int Sign =>
            Math.Sign(value.Amount);
    }

#else

    /// <summary>
    /// Returns the sign of the specified instance.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="value">The monetary value to test.</param>
    /// <returns>
    /// <c>-1</c> when the amount is negative, <c>0</c> when the amount is zero, and <c>1</c> when the amount is
    /// positive.
    /// </returns>
    public static int Sign<TCurrency>(this Money<TCurrency> value)
        where TCurrency : ICurrency =>
        Math.Sign(value.Amount);

#endif
}
