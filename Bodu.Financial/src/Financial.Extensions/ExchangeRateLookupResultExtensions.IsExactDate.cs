// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupResultExtensions.IsExactDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Extensions;

public static partial class ExchangeRateLookupResultExtensions
{
#if BODU_EXTENSION_MEMBERS

    extension(ExchangeRateLookupResult result)
    {
        /// <summary>
        /// Gets a value indicating whether the resolved rate is observed on the requested date.
        /// </summary>
        /// <value>
        /// <see langword="true" /> when <see cref="ExchangeRateLookupResult.OffsetDays" /> is zero; otherwise
        /// <see langword="false" />.
        /// </value>
        public bool IsExactDate =>
            result.OffsetDays == 0;
    }

#else

    /// <summary>
    /// Returns a value indicating whether the resolved rate is observed on the requested date.
    /// </summary>
    /// <param name="result">The lookup outcome to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="ExchangeRateLookupResult.OffsetDays" /> is zero; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool IsExactDate(this ExchangeRateLookupResult result) =>
        result.OffsetDays == 0;

#endif
}
