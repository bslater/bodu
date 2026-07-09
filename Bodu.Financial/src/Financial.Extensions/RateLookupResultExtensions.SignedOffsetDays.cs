// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateLookupResultExtensions.SignedOffsetDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Extensions;

public static partial class RateLookupResultExtensions
{
#if BODU_EXTENSION_MEMBERS

    extension(RateLookupResult result)
    {
        /// <summary>
        /// Gets the signed difference, in days, from <see cref="RateLookupResult.RequestedDate" /> to the resolved
        /// observation date.
        /// </summary>
        /// <value>
        /// A negative value when the resolved observation predates the request (PreviousOnOrBefore fallback), a
        /// positive value when it post-dates the request (NextOnOrAfter fallback), or zero for an exact match.
        /// </value>
        /// <remarks>
        /// <para>
        /// Accounting and tax workflows typically distinguish between historical and forward-looking fallbacks; this
        /// member surfaces that distinction without forcing the caller to recompute it from
        /// <see cref="RateLookupResult.OffsetDays" />.
        /// </para>
        /// </remarks>
        public int SignedOffsetDays =>
            result.Rate.Date.DayNumber - result.RequestedDate.DayNumber;
    }

#else

    /// <summary>
    /// Returns the signed difference, in days, from <see cref="RateLookupResult.RequestedDate" /> to the
    /// resolved observation date.
    /// </summary>
    /// <param name="result">The lookup outcome to inspect.</param>
    /// <returns>
    /// A negative value when the resolved observation predates the request (PreviousOnOrBefore fallback), a positive
    /// value when it post-dates the request (NextOnOrAfter fallback), or zero for an exact match.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Accounting and tax workflows typically distinguish between historical and forward-looking fallbacks; this
    /// member surfaces that distinction without forcing the caller to recompute it from
    /// <see cref="RateLookupResult.OffsetDays" />.
    /// </para>
    /// </remarks>
    public static int SignedOffsetDays(this RateLookupResult result) =>
        result.Rate.Date.DayNumber - result.RequestedDate.DayNumber;

#endif
}
