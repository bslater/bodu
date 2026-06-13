// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.IsPositive.cs" company="Bodu Pty. Ltd.">
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
        /// Gets a value indicating whether this instance is strictly greater than zero.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when <see cref="Money{TCurrency}.Amount" /> is positive; <see langword="false" />
        /// when it is zero or negative.
        /// </returns>
        public bool IsPositive =>
            value.Amount > 0m;
    }

#else

    /// <summary>
    /// Returns a value indicating whether the specified instance is strictly greater than zero.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="value">The monetary value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="Money{TCurrency}.Amount" /> is positive; <see langword="false" /> when
    /// it is zero or negative.
    /// </returns>
    public static bool IsPositive<TCurrency>(this Money<TCurrency> value)
        where TCurrency : ICurrency =>
        value.Amount > 0m;

#endif
}
