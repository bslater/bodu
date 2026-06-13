// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupResultExtensions.IsFutureDate.cs" company="Bodu Pty. Ltd.">
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
        /// Gets a value indicating whether the resolved observation post-dates the requested date.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> when the resolved observation falls strictly after
        /// <see cref="ExchangeRateLookupResult.RequestedDate" />; otherwise <see langword="false" />.
        /// </returns>
        public bool IsFutureDate =>
            result.Rate.Date.DayNumber - result.RequestedDate.DayNumber > 0;
    }

#else

    /// <summary>
    /// Returns a value indicating whether the resolved observation post-dates the requested date.
    /// </summary>
    /// <param name="result">The lookup outcome to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the resolved observation falls strictly after
    /// <see cref="ExchangeRateLookupResult.RequestedDate" />; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsFutureDate(this ExchangeRateLookupResult result) =>
        result.Rate.Date.DayNumber - result.RequestedDate.DayNumber > 0;

#endif
}
