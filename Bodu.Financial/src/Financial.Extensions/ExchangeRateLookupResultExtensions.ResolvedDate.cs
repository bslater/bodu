// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupResultExtensions.ResolvedDate.cs" company="Bodu Pty. Ltd.">
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
        /// Gets the calendar date of the observation actually selected by the lookup, as carried by
        /// <see cref="ExchangeRateLookupResult.Rate" />.
        /// </summary>
        /// <value>The date the resolved rate is observed on.</value>
        public DateOnly ResolvedDate =>
            result.Rate.Date;
    }

#else

    /// <summary>
    /// Returns the calendar date of the observation actually selected by the lookup, as carried by
    /// <see cref="ExchangeRateLookupResult.Rate" />.
    /// </summary>
    /// <param name="result">The lookup outcome to inspect.</param>
    /// <returns>The date the resolved rate is observed on.</returns>
    public static DateOnly ResolvedDate(this ExchangeRateLookupResult result) =>
        result.Rate.Date;

#endif
}
