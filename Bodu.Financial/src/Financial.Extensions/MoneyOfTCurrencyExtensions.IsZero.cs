// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyExtensions.IsZero.cs" company="Bodu Pty. Ltd.">
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
        /// Gets a value indicating whether this instance represents zero.
        /// </summary>
        /// <value>
        /// <see langword="true" /> when <see cref="Money{TCurrency}.Amount" /> is zero; otherwise
        /// <see langword="false" />.
        /// </value>
        public bool IsZero =>
            value.Amount == 0m;
    }

#else

    /// <summary>
    /// Returns a value indicating whether the specified instance represents zero.
    /// </summary>
    /// <typeparam name="TCurrency">The currency tag identifying the monetary type.</typeparam>
    /// <param name="value">The monetary value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="Money{TCurrency}.Amount" /> is zero; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsZero<TCurrency>(this Money<TCurrency> value)
        where TCurrency : ICurrency =>
        value.Amount == 0m;

#endif
}
