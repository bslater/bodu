// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.IsNegative.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class MoneyOfTCurrencyExtensions
{
#if BODU_EXTENSION_MEMBERS

    extension<TCurrency>(Money<TCurrency> value)
        where TCurrency : ICurrency
    {
        /// <summary>
        /// Gets a value indicating whether this instance is strictly less than zero.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when <see cref="Money{TCurrency}.Amount" /> is negative; <see langword="false" />
        /// when it is zero or positive.
        /// </returns>
        public bool IsNegative =>
            value.Amount < 0m;
    }

#else

    /// <summary>
    /// Returns a value indicating whether the specified instance is strictly less than zero.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="value">The monetary value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="Money{TCurrency}.Amount" /> is negative; <see langword="false" /> when
    /// it is zero or positive.
    /// </returns>
    public static bool IsNegative<TCurrency>(this Money<TCurrency> value)
        where TCurrency : ICurrency =>
        value.Amount < 0m;

#endif
}
